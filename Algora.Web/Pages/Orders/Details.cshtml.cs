using Algora.Application.DTOs;
using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Orders;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IShopifyOrderService orderService,
        IWhatsAppService whatsAppService,
        IShopContext shopContext,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _whatsAppService = whatsAppService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public OrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }

    // WhatsApp messaging properties
    [BindProperty]
    public string? WhatsAppMessage { get; set; }
    public string? WhatsAppSuccess { get; set; }
    public string? WhatsAppError { get; set; }
    public string? CustomerPhone { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        ViewData["Title"] = $"Order #{id}";

        // Check for WhatsApp feedback from TempData
        WhatsAppSuccess = TempData["WhatsAppSuccess"]?.ToString();
        WhatsAppError = TempData["WhatsAppError"]?.ToString();

        try
        {
            Order = await _orderService.GetByIdAsync(id);
            if (Order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                ErrorMessage = $"Order #{id} not found.";
            }
            else
            {
                _logger.LogInformation("Loaded order {OrderId}", id);
                // Extract customer phone for WhatsApp
                CustomerPhone = Order.Customer?.Phone
                    ?? Order.ShippingAddress?.Phone
                    ?? Order.BillingAddress?.Phone;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order {OrderId}", id);
            ErrorMessage = "Failed to load order. Please ensure the shop is connected.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendWhatsAppAsync(long id)
    {
        if (string.IsNullOrWhiteSpace(WhatsAppMessage))
        {
            TempData["WhatsAppError"] = "Message cannot be empty.";
            return RedirectToPage(new { id });
        }

        try
        {
            Order = await _orderService.GetByIdAsync(id);
            if (Order == null)
            {
                TempData["WhatsAppError"] = "Order not found.";
                return RedirectToPage(new { id });
            }

            // Get customer phone
            var phone = Order.Customer?.Phone
                ?? Order.ShippingAddress?.Phone
                ?? Order.BillingAddress?.Phone;

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["WhatsAppError"] = "No phone number available for this customer.";
                return RedirectToPage(new { id });
            }

            // Send WhatsApp message
            var dto = new SendWhatsAppTextMessageDto
            {
                PhoneNumber = phone,
                Content = WhatsAppMessage,
                CustomerId = (int?)Order.Customer?.Id
            };

            var result = await _whatsAppService.SendTextMessageAsync(_shopContext.ShopDomain, dto);

            _logger.LogInformation("WhatsApp message sent to {Phone} for order {OrderId}, MessageId: {MessageId}",
                phone, id, result.Id);

            TempData["WhatsAppSuccess"] = $"WhatsApp message sent successfully to {phone}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message for order {OrderId}", id);
            TempData["WhatsAppError"] = $"Failed to send message: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }
}
