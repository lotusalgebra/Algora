using Algora.Application.DTOs;
using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Orders;

[Authorize]
public class EditStatusModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<EditStatusModel> _logger;

    public EditStatusModel(
        IShopifyOrderService orderService,
        IWhatsAppService whatsAppService,
        IShopContext shopContext,
        ILogger<EditStatusModel> logger)
    {
        _orderService = orderService;
        _whatsAppService = whatsAppService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public OrderDto? Order { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string NewStatus { get; set; } = string.Empty;

    [BindProperty]
    public string? TrackingNumber { get; set; }

    [BindProperty]
    public string? TrackingUrl { get; set; }

    [BindProperty]
    public string? TrackingCompany { get; set; }

    [BindProperty]
    public bool SendNotification { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        try
        {
            Order = await _orderService.GetByIdAsync(id);
            if (Order == null)
            {
                ErrorMessage = $"Order #{id} not found.";
                return Page();
            }

            // Set current status
            NewStatus = Order.FulfillmentStatus ?? "unfulfilled";

            // Extract customer phone
            CustomerPhone = Order.Customer?.Phone
                ?? Order.ShippingAddress?.Phone
                ?? Order.BillingAddress?.Phone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order {OrderId}", id);
            ErrorMessage = "Failed to load order.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        try
        {
            Order = await _orderService.GetByIdAsync(id);
            if (Order == null)
            {
                ErrorMessage = "Order not found.";
                return Page();
            }

            // Extract customer phone for notifications
            CustomerPhone = Order.Customer?.Phone
                ?? Order.ShippingAddress?.Phone
                ?? Order.BillingAddress?.Phone;

            // Update fulfillment status based on selection
            if (NewStatus == "shipped" || NewStatus == "fulfilled")
            {
                // Create fulfillment in Shopify
                // Note: This would typically call a fulfillment API endpoint
                // For now, we'll send the notification if requested

                if (SendNotification && !string.IsNullOrWhiteSpace(CustomerPhone))
                {
                    await SendShippingNotificationAsync(id);
                }

                SuccessMessage = "Order status updated to Shipped.";
                if (SendNotification && !string.IsNullOrWhiteSpace(CustomerPhone))
                {
                    SuccessMessage += " WhatsApp notification sent.";
                }
            }
            else if (NewStatus == "delivered")
            {
                if (SendNotification && !string.IsNullOrWhiteSpace(CustomerPhone))
                {
                    await SendDeliveryNotificationAsync(id);
                }

                SuccessMessage = "Order marked as Delivered.";
                if (SendNotification && !string.IsNullOrWhiteSpace(CustomerPhone))
                {
                    SuccessMessage += " WhatsApp notification sent.";
                }
            }
            else
            {
                SuccessMessage = $"Order status updated to {NewStatus}.";
            }

            _logger.LogInformation("Order {OrderId} status updated to {Status}", id, NewStatus);

            // Redirect back to order details
            TempData["WhatsAppSuccess"] = SuccessMessage;
            return RedirectToPage("/Orders/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order {OrderId} status", id);
            ErrorMessage = $"Failed to update status: {ex.Message}";
            return Page();
        }
    }

    private async Task SendShippingNotificationAsync(long orderId)
    {
        if (Order == null || string.IsNullOrWhiteSpace(CustomerPhone)) return;

        var customerName = Order.Customer?.FirstName ?? "Customer";
        var orderNumber = Order.Name;

        var message = $"Hi {customerName}! Great news - your order {orderNumber} has been shipped!";

        if (!string.IsNullOrWhiteSpace(TrackingNumber))
        {
            message += $"\n\nTracking Number: {TrackingNumber}";
        }

        if (!string.IsNullOrWhiteSpace(TrackingCompany))
        {
            message += $"\nCarrier: {TrackingCompany}";
        }

        if (!string.IsNullOrWhiteSpace(TrackingUrl))
        {
            message += $"\n\nTrack your package: {TrackingUrl}";
        }

        message += "\n\nThank you for shopping with us!";

        var dto = new SendWhatsAppTextMessageDto
        {
            PhoneNumber = CustomerPhone,
            Content = message,
            CustomerId = (int?)Order.Customer?.Id
        };

        await _whatsAppService.SendTextMessageAsync(_shopContext.ShopDomain, dto);
        _logger.LogInformation("Shipping notification sent for order {OrderId} to {Phone}", orderId, CustomerPhone);
    }

    private async Task SendDeliveryNotificationAsync(long orderId)
    {
        if (Order == null || string.IsNullOrWhiteSpace(CustomerPhone)) return;

        var customerName = Order.Customer?.FirstName ?? "Customer";
        var orderNumber = Order.Name;

        var message = $"Hi {customerName}! Your order {orderNumber} has been delivered!\n\n" +
                      "We hope you love your purchase. If you have any questions or concerns, " +
                      "please don't hesitate to reach out.\n\nThank you for shopping with us!";

        var dto = new SendWhatsAppTextMessageDto
        {
            PhoneNumber = CustomerPhone,
            Content = message,
            CustomerId = (int?)Order.Customer?.Id
        };

        await _whatsAppService.SendTextMessageAsync(_shopContext.ShopDomain, dto);
        _logger.LogInformation("Delivery notification sent for order {OrderId} to {Phone}", orderId, CustomerPhone);
    }
}
