using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Orders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IShopifyOrderService orderService, ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public IEnumerable<OrderDto> Orders { get; set; } = [];
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Limit { get; set; } = 25;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Orders";
        
        try
        {
            Orders = await _orderService.GetAllAsync(Limit);
            _logger.LogInformation("Loaded {Count} orders", Orders.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load orders");
            ErrorMessage = "Failed to load orders. Please ensure the shop is connected.";
        }

        return Page();
    }
}
