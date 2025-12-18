using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Orders;

[Authorize]
public class InvoiceModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly ILogger<InvoiceModel> _logger;

    public InvoiceModel(IShopifyOrderService orderService, ILogger<InvoiceModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public OrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        ViewData["Title"] = $"Invoice #{id}";

        try
        {
            Order = await _orderService.GetByIdAsync(id);
            if (Order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for invoice view", id);
                ErrorMessage = $"Order #{id} not found.";
            }
            else
            {
                ViewData["Title"] = $"Invoice {Order.Name}";
                _logger.LogInformation("Loaded invoice for order {OrderId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order {OrderId} for invoice", id);
            ErrorMessage = "Failed to load order. Please ensure the shop is connected.";
        }

        return Page();
    }
}
