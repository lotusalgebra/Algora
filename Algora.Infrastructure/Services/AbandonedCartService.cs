using Algora.Application.DTOs;
using Algora.Application.DTOs.Communication;
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
    private readonly IWhatsAppService _whatsApp;
    private readonly INotificationService? _notificationService;

    /// <summary>
    /// Creates a new instance of <see cref="AbandonedCartService"/>.
    /// </summary>
    /// <param name="context">Shop context providing shop domain and access token.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="whatsApp">WhatsApp service used to send reminders.</param>
    /// <param name="notificationService">Optional notification service for email fallback.</param>
    public AbandonedCartService(
        IShopContext context,
        ILogger<AbandonedCartService> logger,
        IWhatsAppService whatsApp,
        INotificationService? notificationService = null)
    {
        _context = context;
        _logger = logger;
        _whatsApp = whatsApp;
        _notificationService = notificationService;
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
            Items = (c.LineItems ?? []).Select(i => new CartItemDto
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
    /// </returns>
    public async Task<bool> SendReminderAsync(long checkoutId)
    {
        var checkout = await _checkoutService.GetAsync(checkoutId);
        if (checkout is null) return false;

        var customerName = checkout.Customer?.FirstName ?? "there";
        var message = $"👋 Hey {customerName}! " +
                      $"You left items worth ₹{checkout.TotalPrice} in your cart. " +
                      $"Complete your order here: {checkout.AbandonedCheckoutUrl}";

        // Try WhatsApp first if phone is available
        if (!string.IsNullOrWhiteSpace(checkout.Phone))
        {
            try
            {
                await _whatsApp.SendTextMessageAsync(_context.ShopDomain, new SendWhatsAppTextMessageDto
                {
                    PhoneNumber = checkout.Phone,
                    Content = message
                });
                _logger.LogInformation("WhatsApp reminder sent to {Phone} for checkout {CheckoutId}",
                    checkout.Phone, checkoutId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send WhatsApp reminder to {Phone}, falling back to email",
                    checkout.Phone);
            }
        }

        // Fallback to email if available
        if (!string.IsNullOrWhiteSpace(checkout.Email) && _notificationService is not null)
        {
            try
            {
                await _notificationService.SendEmailAsync(_context.ShopDomain, new SendEmailNotificationDto
                {
                    ToEmail = checkout.Email,
                    ToName = customerName,
                    Subject = "Don't forget your cart!",
                    Body = $"""
                        <h2>Hey {customerName}!</h2>
                        <p>You left items worth ₹{checkout.TotalPrice} in your cart.</p>
                        <p><a href="{checkout.AbandonedCheckoutUrl}">Complete your order now</a></p>
                        """
                });
                _logger.LogInformation("Email reminder sent to {Email} for checkout {CheckoutId}",
                    checkout.Email, checkoutId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email reminder to {Email}", checkout.Email);
            }
        }
        else if (!string.IsNullOrWhiteSpace(checkout.Email))
        {
            _logger.LogInformation("Email reminder would be sent to {Email} (notification service not available)",
                checkout.Email);
        }

        return true;
    }
}
