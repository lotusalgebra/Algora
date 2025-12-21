using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleDashboardSummary Summary { get; set; } = new();
    public List<BundleListDto> RecentBundles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Get bundles summary
            var bundlesResult = await _bundleService.GetBundlesAsync(_shopContext.ShopDomain, page: 1, pageSize: 5);
            RecentBundles = bundlesResult.Items.ToList();

            // Get analytics
            var analytics = await _bundleService.GetAnalyticsSummaryAsync(_shopContext.ShopDomain);

            Summary = new BundleDashboardSummary
            {
                TotalBundles = bundlesResult.TotalCount,
                ActiveBundles = RecentBundles.Count(b => b.IsActive),
                TotalSales = analytics.TotalOrders,
                TotalRevenue = analytics.TotalRevenue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundle dashboard");
            ErrorMessage = "Failed to load bundle data. Please try again.";
        }
    }
}

public class BundleDashboardSummary
{
    public int TotalBundles { get; set; }
    public int ActiveBundles { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
}
