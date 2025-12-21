using Algora.Application.DTOs;
using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Shopify;
using Algora.WhatsApp.DTOs;
using Algora.WhatsApp.Services;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service that provides operations to query abandoned carts (checkouts) and send reminders.
/// This implementation uses Shopify's Checkout API to fetch abandoned checkout data
/// and uses WhatsApp/Email services to send recovery reminders.
/// </summary>
public class AbandonedCartService : IAbandonedCartService
{
    private readonly IShopContext _context;
    private readonly ILogger<AbandonedCartService> _logger;
    private readonly IWhatsAppService _whatsApp;
    private readonly INotificationService? _notificationService;

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
    }

    private AbandonedCheckoutService CreateCheckoutService() =>
        new AbandonedCheckoutService(_context.ShopDomain, _context.AccessToken, _logger);

    /// <summary>
    /// Lists abandoned carts (checkouts) for the configured shop.
    /// </summary>
    public async Task<IEnumerable<AbandonedCartDto>> GetAllAsync(DateTime? since = null)
    {
        _logger.LogInformation("Fetching abandoned checkouts for shop: {ShopDomain}", _context.ShopDomain);

        var filter = new AbandonedCheckoutListFilter
        {
            Limit = 50,
            CreatedAtMin = since ?? DateTime.UtcNow.AddDays(-30), // Default to last 30 days
            Status = "open"
        };

        try
        {
            var checkoutService = CreateCheckoutService();
            var checkouts = await checkoutService.ListAsync(filter);

            _logger.LogInformation("Retrieved {Count} abandoned checkouts", checkouts.Count);

            return checkouts.Select(c => new AbandonedCartDto
            {
                Id = c.Id ?? 0,
                Email = c.Email ?? c.Customer?.Email,
                Phone = c.Phone ?? c.Customer?.Phone,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching abandoned checkouts from Shopify");
            throw;
        }
    }

    /// <summary>
    /// Gets the count of abandoned checkouts.
    /// </summary>
    public async Task<int> GetCountAsync(DateTime? since = null)
    {
        var filter = new AbandonedCheckoutListFilter
        {
            CreatedAtMin = since ?? DateTime.UtcNow.AddDays(-30),
            Status = "open"
        };

        var checkoutService = CreateCheckoutService();
        return await checkoutService.CountAsync(filter);
    }

    /// <summary>
    /// Sends a reminder (WhatsApp or email) for a specific abandoned checkout.
    /// </summary>
    public async Task<bool> SendReminderAsync(long checkoutId)
    {
        _logger.LogInformation("Sending reminder for abandoned checkout {CheckoutId}", checkoutId);

        try
        {
            var checkoutService = CreateCheckoutService();
            var checkout = await checkoutService.GetAsync(checkoutId);

            if (checkout is null)
            {
                _logger.LogWarning("Abandoned checkout {CheckoutId} not found", checkoutId);
                return false;
            }

            var customerName = checkout.Customer?.FirstName ?? "there";
            var totalPrice = checkout.TotalPrice ?? "0";
            var recoveryUrl = checkout.AbandonedCheckoutUrl ?? checkout.RecoveryUrl ?? "#";

            var message = $"👋 Hey {customerName}! " +
                          $"You left items worth ${totalPrice} in your cart. " +
                          $"Complete your order here: {recoveryUrl}";

            // Try WhatsApp first if phone is available
            var phone = checkout.Phone ?? checkout.Customer?.Phone;
            if (!string.IsNullOrWhiteSpace(phone))
            {
                try
                {
                    await _whatsApp.SendTextMessageAsync(_context.ShopDomain, new SendWhatsAppTextMessageDto
                    {
                        PhoneNumber = phone,
                        Content = message
                    });
                    _logger.LogInformation("WhatsApp reminder sent to {Phone} for checkout {CheckoutId}",
                        phone, checkoutId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WhatsApp reminder to {Phone}, falling back to email", phone);
                }
            }

            // Fallback to email if available
            var email = checkout.Email ?? checkout.Customer?.Email;
            if (!string.IsNullOrWhiteSpace(email) && _notificationService is not null)
            {
                try
                {
                    await _notificationService.SendEmailAsync(_context.ShopDomain, new SendEmailNotificationDto
                    {
                        ToEmail = email,
                        ToName = customerName,
                        Subject = "Don't forget your cart!",
                        Body = $"""
                            <h2>Hey {customerName}!</h2>
                            <p>You left items worth ${totalPrice} in your cart.</p>
                            <p><a href="{recoveryUrl}" style="background-color: #7928ca; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; display: inline-block;">Complete your order now</a></p>
                            """
                    });
                    _logger.LogInformation("Email reminder sent to {Email} for checkout {CheckoutId}",
                        email, checkoutId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email reminder to {Email}", email);
                }
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                _logger.LogInformation("Email reminder would be sent to {Email} (notification service not available)", email);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for checkout {CheckoutId}", checkoutId);
            return false;
        }
    }
}
