using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for managing product exchanges.
/// </summary>
public class ExchangeService : IExchangeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExchangeService> _logger;
    private const int DefaultExchangeWindowDays = 30;

    public ExchangeService(AppDbContext db, ILogger<ExchangeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ExchangeDto>> GetExchangesAsync(string shopDomain, ExchangeFilterDto? filter = null)
    {
        var query = _db.Exchanges
            .Include(e => e.Items)
            .Where(e => e.ShopDomain == shopDomain);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            if (filter.CustomerId.HasValue)
                query = query.Where(e => e.CustomerId == filter.CustomerId);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.CreatedAt >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.CreatedAt <= filter.ToDate);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(e =>
                    e.ExchangeNumber.ToLower().Contains(term) ||
                    e.OrderNumber.ToLower().Contains(term) ||
                    e.CustomerEmail.ToLower().Contains(term) ||
                    (e.CustomerName != null && e.CustomerName.ToLower().Contains(term)));
            }

            query = query.Skip(filter.Skip).Take(filter.Take);
        }
        else
        {
            query = query.Take(50);
        }

        var exchanges = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return exchanges.Select(MapToDto);
    }

    public async Task<ExchangeDto?> GetExchangeAsync(int id)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id);

        return exchange != null ? MapToDto(exchange) : null;
    }

    public async Task<ExchangeDto?> GetExchangeByNumberAsync(string shopDomain, string exchangeNumber)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.ShopDomain == shopDomain && e.ExchangeNumber == exchangeNumber);

        return exchange != null ? MapToDto(exchange) : null;
    }

    public async Task<ExchangeDto> CreateExchangeAsync(CreateExchangeDto dto)
    {
        var order = await _db.Orders.FindAsync(dto.OrderId)
            ?? throw new InvalidOperationException($"Order {dto.OrderId} not found");

        var exchangeNumber = await GenerateExchangeNumberAsync(dto.ShopDomain);

        var exchange = new Exchange
        {
            ShopDomain = dto.ShopDomain,
            ExchangeNumber = exchangeNumber,
            OrderId = dto.OrderId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerEmail = dto.CustomerEmail,
            CustomerName = dto.CustomerName,
            Status = "pending",
            Notes = dto.Notes,
            Currency = order.Currency ?? "USD",
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in dto.Items)
        {
            exchange.Items.Add(new ExchangeItem
            {
                OriginalOrderLineId = item.OrderLineId,
                OriginalProductId = item.ProductId,
                OriginalProductVariantId = item.ProductVariantId,
                OriginalProductTitle = item.ProductTitle,
                OriginalVariantTitle = item.VariantTitle,
                OriginalSku = item.Sku,
                OriginalPrice = item.Price,
                Quantity = item.Quantity,
                Reason = item.Reason,
                CustomerNote = item.CustomerNote
            });
        }

        _db.Exchanges.Add(exchange);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created exchange {ExchangeNumber} for order {OrderNumber}", exchangeNumber, order.OrderNumber);
        return MapToDto(exchange);
    }

    public async Task<ExchangeDto> UpdateExchangeItemsAsync(int id, UpdateExchangeItemsDto dto)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Exchange {id} not found");

        if (exchange.Status != "pending" && exchange.Status != "approved")
            throw new InvalidOperationException($"Cannot update items for exchange in status: {exchange.Status}");

        decimal totalPriceDifference = 0;

        foreach (var update in dto.Items)
        {
            var item = exchange.Items.FirstOrDefault(i => i.Id == update.ExchangeItemId)
                ?? throw new InvalidOperationException($"Exchange item {update.ExchangeItemId} not found");

            item.NewProductId = update.NewProductId;
            item.NewProductVariantId = update.NewProductVariantId;
            item.NewProductTitle = update.NewProductTitle;
            item.NewVariantTitle = update.NewVariantTitle;
            item.NewSku = update.NewSku;
            item.NewPrice = update.NewPrice;

            totalPriceDifference += (update.NewPrice - item.OriginalPrice) * item.Quantity;
        }

        exchange.PriceDifference = totalPriceDifference;
        exchange.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(exchange);
    }

    public async Task<ExchangeDto> ApproveExchangeAsync(int id, string? notes = null)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Exchange {id} not found");

        if (exchange.Status != "pending")
            throw new InvalidOperationException($"Cannot approve exchange in status: {exchange.Status}");

        exchange.Status = "approved";
        exchange.ApprovedAt = DateTime.UtcNow;
        exchange.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(notes))
            exchange.Notes = (exchange.Notes ?? "") + $"\nApproval: {notes}";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Approved exchange {ExchangeNumber}", exchange.ExchangeNumber);
        return MapToDto(exchange);
    }

    public async Task<ExchangeDto> MarkItemsReceivedAsync(int id)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Exchange {id} not found");

        if (exchange.Status != "approved" && exchange.Status != "shipped")
            throw new InvalidOperationException($"Cannot mark items received for exchange in status: {exchange.Status}");

        exchange.Status = "received";
        exchange.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Marked items received for exchange {ExchangeNumber}", exchange.ExchangeNumber);
        return MapToDto(exchange);
    }

    public async Task<ExchangeDto> CompleteExchangeAsync(int id)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Exchange {id} not found");

        if (exchange.Status != "received")
            throw new InvalidOperationException($"Cannot complete exchange in status: {exchange.Status}");

        exchange.Status = "completed";
        exchange.CompletedAt = DateTime.UtcNow;
        exchange.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Completed exchange {ExchangeNumber}", exchange.ExchangeNumber);
        return MapToDto(exchange);
    }

    public async Task<ExchangeDto> CancelExchangeAsync(int id, string reason)
    {
        var exchange = await _db.Exchanges
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Exchange {id} not found");

        if (exchange.Status == "completed")
            throw new InvalidOperationException("Cannot cancel a completed exchange");

        exchange.Status = "cancelled";
        exchange.Notes = (exchange.Notes ?? "") + $"\nCancelled: {reason}";
        exchange.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Cancelled exchange {ExchangeNumber}: {Reason}", exchange.ExchangeNumber, reason);
        return MapToDto(exchange);
    }

    public async Task<ExchangeEligibilityDto> CheckEligibilityAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return new ExchangeEligibilityDto(false, "Order not found", 0, DefaultExchangeWindowDays, Array.Empty<ExchangeEligibleItemDto>());

        var daysSinceOrder = (DateTime.UtcNow - order.CreatedAt).Days;
        if (daysSinceOrder > DefaultExchangeWindowDays)
            return new ExchangeEligibilityDto(false, $"Order is older than {DefaultExchangeWindowDays} days", daysSinceOrder, DefaultExchangeWindowDays, Array.Empty<ExchangeEligibleItemDto>());

        if (order.FulfillmentStatus != "fulfilled")
            return new ExchangeEligibilityDto(false, "Order has not been fulfilled", daysSinceOrder, DefaultExchangeWindowDays, Array.Empty<ExchangeEligibleItemDto>());

        // Get existing exchanges/returns for this order
        var existingExchanges = await _db.Exchanges
            .Include(e => e.Items)
            .Where(e => e.OrderId == orderId && e.Status != "cancelled")
            .ToListAsync();

        var existingReturns = await _db.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.OrderId == orderId && r.Status != "cancelled" && r.Status != "rejected")
            .ToListAsync();

        var eligibleItems = new List<ExchangeEligibleItemDto>();

        foreach (var line in order.Lines)
        {
            var exchangedQty = existingExchanges
                .SelectMany(e => e.Items)
                .Where(i => i.OriginalOrderLineId == line.Id)
                .Sum(i => i.Quantity);

            var returnedQty = existingReturns
                .SelectMany(r => r.Items)
                .Where(i => i.OrderLineId == line.Id)
                .Sum(i => i.QuantityReturned);

            var availableQty = line.Quantity - exchangedQty - returnedQty;

            if (availableQty > 0)
            {
                eligibleItems.Add(new ExchangeEligibleItemDto(
                    line.Id,
                    (int)(line.PlatformProductId ?? 0),
                    line.PlatformVariantId.HasValue ? (int?)line.PlatformVariantId : null,
                    line.ProductTitle,
                    line.VariantTitle,
                    line.Sku,
                    line.UnitPrice,
                    line.Quantity,
                    availableQty
                ));
            }
        }

        return new ExchangeEligibilityDto(
            eligibleItems.Count > 0,
            eligibleItems.Count > 0 ? null : "No items available for exchange",
            daysSinceOrder,
            DefaultExchangeWindowDays,
            eligibleItems
        );
    }

    public async Task<IEnumerable<ExchangeProductOptionDto>> GetExchangeOptionsAsync(string shopDomain, int? originalProductId = null)
    {
        var query = _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain && v.Product.IsActive);

        if (originalProductId.HasValue)
        {
            // Optionally filter to same product or similar products
            // For now, show all products
        }

        var variants = await query
            .OrderBy(v => v.Product!.Title)
            .Take(200)
            .ToListAsync();

        return variants.Select(v => new ExchangeProductOptionDto(
            v.ProductId,
            v.Id,
            v.Product!.Title,
            v.Title == "Default Title" ? null : v.Title,
            v.Sku,
            v.Price,
            v.InventoryQuantity,
            null, // ImageUrl not available on Product entity
            v.InventoryQuantity > 0
        ));
    }

    private async Task<string> GenerateExchangeNumberAsync(string shopDomain)
    {
        var prefix = "EXC";
        var count = await _db.Exchanges.CountAsync(e => e.ShopDomain == shopDomain);
        return $"{prefix}-{(count + 1):D6}";
    }

    private static ExchangeDto MapToDto(Exchange e) => new(
        e.Id,
        e.ShopDomain,
        e.ExchangeNumber,
        e.OrderId,
        e.OrderNumber,
        e.CustomerId,
        e.CustomerEmail,
        e.CustomerName,
        e.Status,
        e.ReturnRequestId,
        e.NewOrderId,
        e.PriceDifference,
        e.Currency,
        e.Notes,
        e.ApprovedAt,
        e.CompletedAt,
        e.CreatedAt,
        e.UpdatedAt,
        e.Items.Select(i => new ExchangeItemDto(
            i.Id,
            i.ExchangeId,
            i.OriginalOrderLineId,
            i.OriginalProductId,
            i.OriginalProductVariantId,
            i.OriginalProductTitle,
            i.OriginalVariantTitle,
            i.OriginalSku,
            i.OriginalPrice,
            i.Quantity,
            i.NewProductId,
            i.NewProductVariantId,
            i.NewProductTitle,
            i.NewVariantTitle,
            i.NewSku,
            i.NewPrice,
            i.Reason,
            i.CustomerNote
        ))
    );
}
