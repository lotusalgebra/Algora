using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Shopify;

/// <summary>
/// Service that provides operations to query abandoned carts (checkouts) and send reminders.
/// This implementation delegates to a lightweight <see cref="AbandonedCheckoutService"/> to
/// read checkout data and uses <see cref="IWhatsAppService"/> to send WhatsApp reminders when a phone is available.
/// </summary>
public class AbandonedCartService : IAbandonedCartService
{
    private readonly IShopContext _context;
    private readonly ILogger<AbandonedCartService> _logger;
    private readonly AbandonedCheckoutService _checkoutService;
    private readonly IWhatsAppService _whatsApp; // optional if you added WhatsAppService

    /// <summary>
    /// Creates a new instance of <see cref="AbandonedCartService"/>.
    /// </summary>
    /// <param name="context">Shop context providing shop domain and access token.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="whatsApp">WhatsApp service used to send reminders (may be a no-op implementation).</param>
    public AbandonedCartService(IShopContext context, ILogger<AbandonedCartService> logger, IWhatsAppService whatsApp)
    {
        _context = context;
        _logger = logger;
        _whatsApp = whatsApp;
        // Use the local AbandonedCheckoutService placeholder (constructed from shop context)
        _checkoutService = new AbandonedCheckoutService(context.ShopDomain, context.AccessToken);
    }

    /// <summary>
    /// Lists abandoned carts (checkouts) for the configured shop.
    /// </summary>
    /// <param name="since">
    /// Optional UTC date/time; when provided only carts abandoned on or after this instant are returned.
    /// When null the method defaults to a recent window (7 days).
    /// </param>
    /// <returns>
    /// A task that resolves to an enumerable of <see cref="AbandonedCartDto"/> mapped from the underlying checkout records.
    /// </returns>
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

    /// <summary>
    /// Sends a reminder (WhatsApp or email) for a specific abandoned checkout.
    /// </summary>
    /// <param name="checkoutId">The platform-specific checkout id to remind.</param>
    /// <returns>
    /// A task that resolves to true when the reminder was queued/sent; false when the checkout does not exist.
    /// Implementations should log failures and may throw for unrecoverable errors.
    /// </returns>
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
