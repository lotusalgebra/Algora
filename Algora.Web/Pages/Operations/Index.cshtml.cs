using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations;

[Authorize]
[RequireFeature(FeatureCodes.SupplierManagement)]
public class IndexModel : PageModel
{
    private readonly ISupplierService _supplierService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILocationService _locationService;
    private readonly IInventoryPredictionService _predictionService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISupplierService supplierService,
        IPurchaseOrderService purchaseOrderService,
        ILocationService locationService,
        IInventoryPredictionService predictionService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _supplierService = supplierService;
        _purchaseOrderService = purchaseOrderService;
        _locationService = locationService;
        _predictionService = predictionService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public int ActiveSupplierCount { get; set; }
    public int PendingPurchaseOrderCount { get; set; }
    public int LocationCount { get; set; }
    public int LowStockProductCount { get; set; }
    public decimal TotalPendingOrderValue { get; set; }
    public List<PurchaseOrderDto> RecentPurchaseOrders { get; set; } = new();
    public List<SuggestedPurchaseOrderDto> SuggestedOrders { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            // Get supplier count
            var suppliers = await _supplierService.GetSuppliersAsync(shopDomain);
            ActiveSupplierCount = suppliers.Count(s => s.IsActive);

            // Get pending purchase orders
            var purchaseOrders = await _purchaseOrderService.GetPurchaseOrdersAsync(shopDomain, new PurchaseOrderFilterDto());
            var pendingStatuses = new[] { "draft", "sent", "confirmed", "shipped" };
            var pendingOrders = purchaseOrders.Where(po => pendingStatuses.Contains(po.Status)).ToList();
            PendingPurchaseOrderCount = pendingOrders.Count;
            TotalPendingOrderValue = pendingOrders.Sum(po => po.Total);
            RecentPurchaseOrders = purchaseOrders.OrderByDescending(po => po.CreatedAt).Take(5).ToList();

            // Get locations
            var locations = await _locationService.GetLocationsAsync(shopDomain);
            LocationCount = locations.Count();

            // Get low stock prediction count
            var predictions = await _predictionService.GetPredictionSummaryAsync(shopDomain);
            LowStockProductCount = (predictions?.LowStockCount ?? 0) + (predictions?.CriticalStockCount ?? 0) + (predictions?.OutOfStockCount ?? 0);

            // Get suggested orders
            SuggestedOrders = (await _purchaseOrderService.GenerateSuggestedOrdersAsync(shopDomain)).Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading operations dashboard");
            ErrorMessage = "Failed to load operations data. Please try again.";
        }
    }
}
