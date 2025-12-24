using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations.PurchaseOrders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPurchaseOrderService purchaseOrderService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<PurchaseOrderDto> PurchaseOrders { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var filter = new PurchaseOrderFilterDto { Status = StatusFilter };
            var orders = await _purchaseOrderService.GetPurchaseOrdersAsync(_shopContext.ShopDomain, filter);
            PurchaseOrders = orders.OrderByDescending(o => o.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading purchase orders");
            ErrorMessage = "Failed to load purchase orders. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try
        {
            await _purchaseOrderService.CancelPurchaseOrderAsync(id, "Cancelled by user");
            TempData["SuccessMessage"] = "Purchase order cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling purchase order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to cancel purchase order.";
        }

        return RedirectToPage();
    }
}
