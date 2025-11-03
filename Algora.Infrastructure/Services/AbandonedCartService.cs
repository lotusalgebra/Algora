using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Shopify;

public class AbandonedCartService : IAbandonedCartService
{
    private readonly IShopContext _context;
    private readonly ILogger<AbandonedCartService> _logger;
    private readonly AbandonedCheckoutService _checkoutService;
    private readonly IWhatsAppService _whatsApp; // optional if you added WhatsAppService

    public AbandonedCartService(IShopContext context, ILogger<AbandonedCartService> logger, IWhatsAppService whatsApp)
    {
        _context = context;
        _logger = logger;
        _whatsApp = whatsApp;
        // Use the local AbandonedCheckoutService placeholder (constructed from shop context)
        _checkoutService = new AbandonedCheckoutService(context.ShopDomain, context.AccessToken);
    }

    public async Task<IEnumerable<AbandonedCartDto>> GetAllAsync(DateTime? since = null)
    {
        // Use the local AbandonedCheckoutListFilter (defined in Algora.Infrastructure.Shopify)
        var filter = new AbandonedCheckoutListFilter
        {
            Limit = 50,
            CreatedAtMin = since ?? DateTime.UtcNow.AddDays(-7)
        };

        var checkouts = await _checkoutService.ListAsync(filter);

        return checkouts.Select(c => new AbandonedCartDto
        {
            Id = c.Id ?? 0,
            Email = c.Email,
            Phone = c.Phone,
            TotalPrice = decimal.TryParse(c.TotalPrice, out var p) ? p : 0,
            AbandonedAt = c.CreatedAt?.UtcDateTime,
            Items = (c.LineItems ?? new List<AbandonedCheckoutLineItem>()).Select(i => new CartItemDto
            {
                Title = i.Title,
                Quantity = i.Quantity ?? 0,
                Price = i.Price ?? 0m
            }).ToList()
        });
    }

    public async Task<bool> SendReminderAsync(long checkoutId)
    {
        var checkout = await _checkoutService.GetAsync(checkoutId);
        if (checkout == null) return false;

        string message = $"👋 Hey {checkout.Customer?.FirstName ?? "there"}! " +
                         $"You left items worth ₹{checkout.TotalPrice} in your cart. " +
                         $"Complete your order here: {checkout.AbandonedCheckoutUrl}";

        if (!string.IsNullOrWhiteSpace(checkout.Phone))
        {
            await _whatsApp.SendOrderUpdateAsync(checkout.Phone, message);
            _logger.LogInformation("WhatsApp reminder sent to {Phone}", checkout.Phone);
        }
        else if (!string.IsNullOrWhiteSpace(checkout.Email))
        {
            // or use email service
            _logger.LogInformation("Email reminder would be sent to {Email}", checkout.Email);
        }

        return true;
    }
}
