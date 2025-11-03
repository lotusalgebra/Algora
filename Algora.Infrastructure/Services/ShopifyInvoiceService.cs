using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using System.Linq;

namespace Algora.Infrastructure.Shopify;

public class ShopifyInvoiceService : IShopifyInvoiceService
{
    private readonly IShopContext _context;
    private readonly ILogger<ShopifyInvoiceService> _logger;

    public ShopifyInvoiceService(IShopContext context, ILogger<ShopifyInvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private DraftOrderService CreateDraftService() =>
        new(_context.ShopDomain, _context.AccessToken);

    private OrderService CreateOrderService() =>
        new(_context.ShopDomain, _context.AccessToken);

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync(int limit = 25)
    {
        var service = CreateDraftService();
        var drafts = await service.ListAsync(new ShopifySharp.Filters.DraftOrderListFilter
        {
            Limit = limit
        });

        var items = drafts?.Items ?? Enumerable.Empty<DraftOrder>();
        return items.Select(d => new InvoiceDto
        {
            Id = d.Id ?? 0,
            Name = d.Name ?? $"Draft #{d.Id}",
            CustomerEmail = d.Email ?? "",
            TotalPrice = d.TotalPrice ?? 0m,
            Status = d.Status ?? "open",
            Paid = d.CompletedAt.HasValue,
            CreatedAt = d.CreatedAt?.DateTime ?? DateTime.Now
        });
    }

    public async Task<InvoiceDto?> GetByIdAsync(long id)
    {
        var service = CreateDraftService();
        var draft = await service.GetAsync(id);

        if (draft == null) return null;

        return new InvoiceDto
        {
            Id = draft.Id ?? 0,
            Name = draft.Name ?? "",
            CustomerEmail = draft.Email ?? "",
            TotalPrice = draft.TotalPrice ?? 0m,
            Status = draft.Status ?? "open",
            Paid = draft.CompletedAt.HasValue,
            CreatedAt = draft.CreatedAt?.DateTime ?? DateTime.Now
        };
    }

    public async Task<InvoiceDto?> CreateInvoiceAsync(string email, string title, decimal price)
    {
        var service = CreateDraftService();

        var draftOrder = new DraftOrder
        {
            Email = email,
            LineItems =
            [
                new DraftLineItem
                {
                    Title = title,
                    Quantity = 1,
                    Price = price
                }
            ]
        };

        var created = await service.CreateAsync(draftOrder);

        _logger.LogInformation("Created draft invoice #{Id} for {Email}", created.Id, email);

        return new InvoiceDto
        {
            Id = created.Id ?? 0,
            Name = created.Name ?? "",
            CustomerEmail = created.Email ?? "",
            TotalPrice = created.TotalPrice ?? 0m,
            Status = created.Status ?? "open",
            Paid = false,
            CreatedAt = created.CreatedAt?.DateTime ?? DateTime.Now
        };
    }

    public async Task SendInvoiceAsync(long draftOrderId)
    {
        var service = CreateDraftService();
        await service.SendInvoiceAsync(draftOrderId);
        _logger.LogInformation("Invoice #{Id} email sent successfully.", draftOrderId);
    }

    public async Task CompleteInvoiceAsync(long draftOrderId)
    {
        var service = CreateDraftService();
        var completed = await service.CompleteAsync(draftOrderId, true);

        _logger.LogInformation("Invoice #{Id} marked as paid and converted to Order #{OrderId}.", draftOrderId, completed.OrderId);
    }

    public async Task CancelInvoiceAsync(long draftOrderId)
    {
        var service = CreateDraftService();
        await service.DeleteAsync(draftOrderId);
        _logger.LogInformation("Invoice #{Id} cancelled successfully.", draftOrderId);
    }
}
