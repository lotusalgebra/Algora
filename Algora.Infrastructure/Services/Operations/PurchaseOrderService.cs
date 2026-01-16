using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Service for managing purchase orders and automated reordering.
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _db;
    private readonly ISupplierService _supplierService;
    private readonly IShopifyProductService _shopifyProductService;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        AppDbContext db,
        ISupplierService supplierService,
        IShopifyProductService shopifyProductService,
        ILogger<PurchaseOrderService> logger)
    {
        _db = db;
        _supplierService = supplierService;
        _shopifyProductService = shopifyProductService;
        _logger = logger;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        string shopDomain,
        PurchaseOrderFilterDto? filter = null)
    {
        var query = _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Location)
            .Include(po => po.Lines)
            .Where(po => po.ShopDomain == shopDomain);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(po => po.Status == filter.Status);
            if (filter.SupplierId.HasValue)
                query = query.Where(po => po.SupplierId == filter.SupplierId);
            if (filter.LocationId.HasValue)
                query = query.Where(po => po.LocationId == filter.LocationId);
            if (filter.FromDate.HasValue)
                query = query.Where(po => po.CreatedAt >= filter.FromDate);
            if (filter.ToDate.HasValue)
                query = query.Where(po => po.CreatedAt <= filter.ToDate);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(po => po.OrderNumber.Contains(filter.SearchTerm)
                    || po.Supplier.Name.Contains(filter.SearchTerm));
        }

        return await query
            .OrderByDescending(po => po.CreatedAt)
            .Select(po => MapToDto(po))
            .ToListAsync();
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderAsync(int id)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Location)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);

        return po == null ? null : MapToDto(po);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(dto.SupplierId)
            ?? throw new InvalidOperationException($"Supplier {dto.SupplierId} not found");

        var orderNumber = await GenerateOrderNumberAsync(dto.ShopDomain);

        var purchaseOrder = new PurchaseOrder
        {
            ShopDomain = dto.ShopDomain,
            SupplierId = dto.SupplierId,
            OrderNumber = orderNumber,
            Status = "draft",
            LocationId = dto.LocationId,
            Currency = dto.Currency,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.PurchaseOrders.Add(purchaseOrder);
        await _db.SaveChangesAsync();

        // Add line items if provided
        if (dto.Lines != null && dto.Lines.Any())
        {
            foreach (var lineDto in dto.Lines)
            {
                await AddLineItemInternalAsync(purchaseOrder.Id, lineDto);
            }
            await RecalculateTotalsAsync(purchaseOrder.Id);
        }

        _logger.LogInformation("Created purchase order {OrderNumber} for supplier {SupplierId}",
            orderNumber, dto.SupplierId);

        return (await GetPurchaseOrderAsync(purchaseOrder.Id))!;
    }

    public async Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderDto dto)
    {
        var po = await _db.PurchaseOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "draft")
            throw new InvalidOperationException("Can only edit draft purchase orders");

        if (dto.SupplierId.HasValue)
            po.SupplierId = dto.SupplierId.Value;
        if (dto.LocationId.HasValue)
            po.LocationId = dto.LocationId;
        if (dto.Notes != null)
            po.Notes = dto.Notes;
        if (dto.Tax.HasValue)
            po.Tax = dto.Tax.Value;
        if (dto.Shipping.HasValue)
            po.Shipping = dto.Shipping.Value;

        po.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(id);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task DeletePurchaseOrderAsync(int id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "draft")
            throw new InvalidOperationException("Can only delete draft purchase orders");

        _db.PurchaseOrders.Remove(po);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted purchase order {OrderId}", id);
    }

    public async Task<PurchaseOrderLineDto> AddLineItemAsync(int purchaseOrderId, AddPurchaseOrderLineDto dto)
    {
        var po = await _db.PurchaseOrders.FindAsync(purchaseOrderId)
            ?? throw new InvalidOperationException($"Purchase order {purchaseOrderId} not found");

        if (po.Status != "draft")
            throw new InvalidOperationException("Can only add items to draft purchase orders");

        var line = await AddLineItemInternalAsync(purchaseOrderId, dto);
        await RecalculateTotalsAsync(purchaseOrderId);

        return MapToDto(line);
    }

    private async Task<PurchaseOrderLine> AddLineItemInternalAsync(int purchaseOrderId, AddPurchaseOrderLineDto dto)
    {
        return await AddLineItemInternalAsync(purchaseOrderId, dto.ProductId, dto.ProductVariantId, dto.QuantityOrdered, dto.UnitCost ?? 0);
    }

    private async Task<PurchaseOrderLine> AddLineItemInternalAsync(int purchaseOrderId, CreatePurchaseOrderLineDto dto)
    {
        return await AddLineItemInternalAsync(purchaseOrderId, dto.ProductId, dto.ProductVariantId, dto.QuantityOrdered, dto.UnitCost);
    }

    private async Task<PurchaseOrderLine> AddLineItemInternalAsync(int purchaseOrderId, int productId, int? productVariantId, int quantityOrdered, decimal unitCost)
    {
        var product = await _db.Products.FindAsync(productId)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        ProductVariant? variant = null;
        if (productVariantId.HasValue)
        {
            variant = await _db.ProductVariants.FindAsync(productVariantId.Value);
        }

        // Get unit cost from supplier product or product cost
        var finalUnitCost = unitCost;
        if (unitCost <= 0)
        {
            var supplierProduct = await _db.SupplierProducts
                .FirstOrDefaultAsync(sp => sp.ProductId == productId
                    && (sp.ProductVariantId == productVariantId || sp.ProductVariantId == null));

            if (supplierProduct != null)
                finalUnitCost = supplierProduct.UnitCost;
            else
                finalUnitCost = variant?.CostOfGoodsSold ?? product.CostOfGoodsSold ?? 0;
        }

        var line = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            Sku = variant?.Sku ?? product.Sku,
            ProductTitle = product.Title,
            VariantTitle = variant?.Title,
            QuantityOrdered = quantityOrdered,
            QuantityReceived = 0,
            UnitCost = finalUnitCost,
            TotalCost = finalUnitCost * quantityOrdered
        };

        _db.PurchaseOrderLines.Add(line);
        await _db.SaveChangesAsync();

        return line;
    }

    public async Task<PurchaseOrderLineDto> UpdateLineItemAsync(int lineId, UpdatePurchaseOrderLineDto dto)
    {
        var line = await _db.PurchaseOrderLines
            .Include(l => l.PurchaseOrder)
            .FirstOrDefaultAsync(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Purchase order line {lineId} not found");

        if (line.PurchaseOrder.Status != "draft")
            throw new InvalidOperationException("Can only edit items in draft purchase orders");

        if (dto.QuantityOrdered.HasValue)
            line.QuantityOrdered = dto.QuantityOrdered.Value;
        if (dto.UnitCost.HasValue)
            line.UnitCost = dto.UnitCost.Value;

        line.TotalCost = line.UnitCost * line.QuantityOrdered;
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(line.PurchaseOrderId);

        return MapToDto(line);
    }

    public async Task RemoveLineItemAsync(int lineId)
    {
        var line = await _db.PurchaseOrderLines
            .Include(l => l.PurchaseOrder)
            .FirstOrDefaultAsync(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Purchase order line {lineId} not found");

        if (line.PurchaseOrder.Status != "draft")
            throw new InvalidOperationException("Can only remove items from draft purchase orders");

        var poId = line.PurchaseOrderId;
        _db.PurchaseOrderLines.Remove(line);
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(poId);
    }

    public async Task<PurchaseOrderDto> SendToSupplierAsync(int id, string? message = null)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "draft")
            throw new InvalidOperationException("Can only send draft purchase orders");

        var lineCount = await _db.PurchaseOrderLines.CountAsync(l => l.PurchaseOrderId == id);
        if (lineCount == 0)
            throw new InvalidOperationException("Cannot send empty purchase order");

        po.Status = "sent";
        po.OrderedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;

        // Update supplier product last ordered dates
        var lines = await _db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == id).ToListAsync();
        foreach (var line in lines)
        {
            var supplierProduct = await _db.SupplierProducts
                .FirstOrDefaultAsync(sp => sp.SupplierId == po.SupplierId
                    && sp.ProductId == line.ProductId
                    && sp.ProductVariantId == line.ProductVariantId);

            if (supplierProduct != null)
            {
                supplierProduct.LastOrderedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        // TODO: Send email to supplier if email is configured
        _logger.LogInformation("Sent purchase order {OrderNumber} to supplier {SupplierName}",
            po.OrderNumber, po.Supplier.Name);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task<PurchaseOrderDto> MarkAsConfirmedAsync(int id, DateTime? expectedDeliveryDate = null, string? supplierReference = null)
    {
        var po = await _db.PurchaseOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "sent")
            throw new InvalidOperationException("Can only confirm sent purchase orders");

        po.Status = "confirmed";
        po.ConfirmedAt = DateTime.UtcNow;
        po.ExpectedDeliveryDate = expectedDeliveryDate;
        po.SupplierReference = supplierReference;
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Confirmed purchase order {OrderNumber}", po.OrderNumber);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task<PurchaseOrderDto> MarkAsShippedAsync(int id, string? trackingNumber = null)
    {
        var po = await _db.PurchaseOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "confirmed")
            throw new InvalidOperationException("Can only mark confirmed purchase orders as shipped");

        po.Status = "shipped";
        po.ShippedAt = DateTime.UtcNow;
        po.TrackingNumber = trackingNumber;
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Marked purchase order {OrderNumber} as shipped", po.OrderNumber);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task<PurchaseOrderDto> ReceiveItemsAsync(int id, ReceiveItemsDto dto)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
                .ThenInclude(l => l.Product)
            .Include(p => p.Lines)
                .ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "shipped" && po.Status != "confirmed")
            throw new InvalidOperationException("Can only receive items for confirmed or shipped orders");

        // Get primary location for inventory updates
        string? primaryLocationId = null;
        try
        {
            primaryLocationId = await _shopifyProductService.GetPrimaryLocationIdAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get primary location for inventory sync");
        }

        foreach (var receiveItem in dto.Lines)
        {
            var line = po.Lines.FirstOrDefault(l => l.Id == receiveItem.LineId)
                ?? throw new InvalidOperationException($"Line {receiveItem.LineId} not found in this order");

            line.QuantityReceived += receiveItem.QuantityReceived;
            line.ReceivedAt = DateTime.UtcNow;

            _logger.LogInformation("Received {Qty} of {Product} for PO {OrderNumber}",
                receiveItem.QuantityReceived, line.ProductTitle, po.OrderNumber);

            // Sync inventory to Shopify
            if (primaryLocationId != null)
            {
                try
                {
                    // Get the variant's Shopify ID - prefer variant, fallback to product's first variant
                    long? platformVariantId = line.ProductVariant?.PlatformVariantId;

                    if (platformVariantId == null && line.Product != null)
                    {
                        // Try to get the first variant for this product
                        var firstVariant = await _db.ProductVariants
                            .FirstOrDefaultAsync(v => v.ProductId == line.ProductId);
                        platformVariantId = firstVariant?.PlatformVariantId;
                    }

                    if (platformVariantId.HasValue && platformVariantId.Value > 0)
                    {
                        var inventoryItemId = await _shopifyProductService.GetInventoryItemIdAsync(platformVariantId.Value);
                        if (!string.IsNullOrEmpty(inventoryItemId))
                        {
                            await _shopifyProductService.AdjustInventoryAsync(
                                inventoryItemId,
                                primaryLocationId,
                                receiveItem.QuantityReceived,
                                "received");

                            _logger.LogInformation(
                                "Synced {Qty} units to Shopify inventory for variant {VariantId}",
                                receiveItem.QuantityReceived, platformVariantId.Value);
                        }
                        else
                        {
                            _logger.LogWarning("No inventory item found for variant {VariantId}", platformVariantId.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No Shopify variant ID found for product {ProductTitle}", line.ProductTitle);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - inventory sync is non-critical
                    _logger.LogWarning(ex, "Failed to sync inventory to Shopify for {ProductTitle}", line.ProductTitle);
                }
            }
        }

        // Add notes to all received lines if provided
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            foreach (var receiveItem in dto.Lines)
            {
                var line = po.Lines.FirstOrDefault(l => l.Id == receiveItem.LineId);
                if (line != null)
                    line.ReceivingNotes = dto.Notes;
            }
        }

        po.UpdatedAt = DateTime.UtcNow;

        // Check if fully received
        if (po.Lines.All(l => l.QuantityReceived >= l.QuantityOrdered))
        {
            po.Status = "received";
            po.ReceivedAt = DateTime.UtcNow;
            await _supplierService.UpdateSupplierMetricsAsync(po.SupplierId);
        }

        await _db.SaveChangesAsync();

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task<PurchaseOrderDto> MarkAsReceivedAsync(int id)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status != "shipped" && po.Status != "confirmed")
            throw new InvalidOperationException("Can only complete confirmed or shipped orders");

        // Mark all lines as fully received
        foreach (var line in po.Lines)
        {
            line.QuantityReceived = line.QuantityOrdered;
            line.ReceivedAt = DateTime.UtcNow;
        }

        po.Status = "received";
        po.ReceivedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _supplierService.UpdateSupplierMetricsAsync(po.SupplierId);

        _logger.LogInformation("Completed purchase order {OrderNumber}", po.OrderNumber);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public async Task<PurchaseOrderDto> CancelOrderAsync(int id, string reason)
    {
        var po = await _db.PurchaseOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"Purchase order {id} not found");

        if (po.Status == "received")
            throw new InvalidOperationException("Cannot cancel completed purchase orders");

        po.Status = "cancelled";
        po.CancelledAt = DateTime.UtcNow;
        po.CancellationReason = reason;
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Cancelled purchase order {OrderNumber}: {Reason}", po.OrderNumber, reason);

        return (await GetPurchaseOrderAsync(id))!;
    }

    public Task<PurchaseOrderDto> CancelPurchaseOrderAsync(int id, string reason)
        => CancelOrderAsync(id, reason);

    public async Task<IEnumerable<SuggestedPurchaseOrderDto>> GenerateSuggestedOrdersAsync(string shopDomain)
    {
        var suggestions = new List<SuggestedPurchaseOrderDto>();

        // Get products that need reordering
        var predictions = await _db.InventoryPredictions
            .Where(p => p.ShopDomain == shopDomain
                && (p.Status == "low_stock" || p.Status == "critical" || p.Status == "out_of_stock"))
            .ToListAsync();

        // Get products with auto-reorder enabled
        var thresholds = await _db.ProductInventoryThresholds
            .Include(t => t.PreferredSupplier)
            .Where(t => t.ShopDomain == shopDomain && t.AutoReorderEnabled)
            .ToListAsync();

        // Group by supplier
        var supplierItems = new Dictionary<int, List<SuggestedLineItemDto>>();

        foreach (var prediction in predictions)
        {
            if (!prediction.ProductId.HasValue) continue;

            var threshold = thresholds.FirstOrDefault(t =>
                t.ProductId == prediction.ProductId &&
                t.ProductVariantId == prediction.ProductVariantId);

            // Get preferred supplier
            var supplierProduct = await _supplierService.GetPreferredSupplierForProductAsync(
                prediction.ProductId.Value,
                prediction.ProductVariantId);

            if (supplierProduct == null) continue;

            var supplierId = threshold?.PreferredSupplierId ?? supplierProduct.SupplierId;

            // Calculate suggested quantity
            var suggestedQty = threshold?.ReorderQuantity ?? prediction.SuggestedReorderQuantity;
            if (suggestedQty <= 0)
            {
                suggestedQty = (int)Math.Ceiling(prediction.AverageDailySales * 30);
            }

            var lineItem = new SuggestedLineItemDto(
                prediction.ProductId.Value,
                prediction.ProductVariantId,
                prediction.ProductTitle,
                prediction.VariantTitle,
                prediction.Sku,
                prediction.CurrentQuantity,
                suggestedQty,
                supplierProduct.UnitCost,
                prediction.DaysUntilStockout,
                prediction.AverageDailySales
            );

            if (!supplierItems.ContainsKey(supplierId))
                supplierItems[supplierId] = new List<SuggestedLineItemDto>();

            supplierItems[supplierId].Add(lineItem);
        }

        // Create suggestions per supplier
        foreach (var (supplierId, items) in supplierItems)
        {
            var supplier = await _db.Suppliers.FindAsync(supplierId);
            if (supplier == null) continue;

            var suggestion = new SuggestedPurchaseOrderDto(
                supplierId,
                supplier.Name,
                supplier.Email,
                null, // LocationId
                null, // LocationName
                items,
                items.Sum(i => i.UnitCost * i.SuggestedQuantity),
                "USD",
                $"{items.Count} products need restocking"
            );

            suggestions.Add(suggestion);
        }

        return suggestions;
    }

    public async Task<PurchaseOrderDto> CreateFromSuggestionAsync(SuggestedPurchaseOrderDto suggestion)
    {
        var shopDomain = (await _db.Suppliers.FindAsync(suggestion.SupplierId))?.ShopDomain
            ?? throw new InvalidOperationException("Supplier not found");

        var createDto = new CreatePurchaseOrderDto(
            shopDomain,
            suggestion.SupplierId,
            suggestion.LocationId,
            $"Auto-generated: {suggestion.Reason}",
            null, // ExpectedDeliveryDate
            suggestion.Lines.Select(l => new CreatePurchaseOrderLineDto(
                l.ProductId,
                l.ProductVariantId,
                l.SuggestedQuantity,
                l.UnitCost
            )).ToList(),
            suggestion.Currency
        );

        return await CreatePurchaseOrderAsync(createDto);
    }

    public async Task<int> ProcessAutoPurchaseOrdersAsync(string shopDomain, CancellationToken ct)
    {
        var suggestions = await GenerateSuggestedOrdersAsync(shopDomain);
        var count = 0;

        foreach (var suggestion in suggestions)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Check if there's already a pending PO for this supplier
                var hasPending = await _db.PurchaseOrders
                    .AnyAsync(po => po.SupplierId == suggestion.SupplierId
                        && (po.Status == "draft" || po.Status == "sent" || po.Status == "confirmed"),
                        ct);

                if (!hasPending)
                {
                    var po = await CreateFromSuggestionAsync(suggestion);
                    _logger.LogInformation("Auto-created purchase order {OrderNumber} for supplier {SupplierName}",
                        po.OrderNumber, suggestion.SupplierName);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create auto purchase order for supplier {SupplierId}",
                    suggestion.SupplierId);
            }
        }

        return count;
    }

    public async Task<string> GenerateOrderNumberAsync(string shopDomain)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PO-{today:yyyyMMdd}";

        var lastOrder = await _db.PurchaseOrders
            .Where(po => po.ShopDomain == shopDomain && po.OrderNumber.StartsWith(prefix))
            .OrderByDescending(po => po.OrderNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastOrder != null)
        {
            var lastSeq = lastOrder.OrderNumber.Split('-').LastOrDefault();
            if (int.TryParse(lastSeq, out var parsed))
                sequence = parsed + 1;
        }

        return $"{prefix}-{sequence:D4}";
    }

    public async Task RecalculateTotalsAsync(int purchaseOrderId)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

        if (po == null) return;

        po.Subtotal = po.Lines.Sum(l => l.TotalCost);
        po.Total = po.Subtotal + po.Tax + po.Shipping;
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po) => new(
        po.Id,
        po.ShopDomain,
        po.SupplierId,
        po.Supplier?.Name ?? "",
        po.OrderNumber,
        po.Status,
        po.LocationId,
        po.Location?.Name,
        po.Subtotal,
        po.Tax,
        po.Shipping,
        po.Total,
        po.Currency,
        po.Notes,
        po.SupplierReference,
        po.TrackingNumber,
        po.ExpectedDeliveryDate,
        po.OrderedAt,
        po.ConfirmedAt,
        po.ShippedAt,
        po.ReceivedAt,
        po.CancelledAt,
        po.CancellationReason,
        po.CreatedAt,
        po.UpdatedAt,
        po.Lines.Select(MapToDto).ToList()
    );

    private static PurchaseOrderLineDto MapToDto(PurchaseOrderLine l) => new(
        l.Id,
        l.PurchaseOrderId,
        l.ProductId,
        l.ProductVariantId,
        l.Sku,
        l.ProductTitle,
        l.VariantTitle,
        l.QuantityOrdered,
        l.QuantityReceived,
        l.UnitCost,
        l.TotalCost,
        l.ReceivedAt,
        l.ReceivingNotes
    );
}
