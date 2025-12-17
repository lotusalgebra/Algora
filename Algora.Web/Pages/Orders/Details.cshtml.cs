using Algora.Application.DTOs;
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
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IShopifyOrderService orderService, ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public OrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        ViewData["Title"] = $"Order #{id}";

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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order {OrderId}", id);
            ErrorMessage = "Failed to load order. Please ensure the shop is connected.";
        }

        return Page();
    }
}
