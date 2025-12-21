using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class AnalyticsModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<AnalyticsModel> _logger;

    public AnalyticsModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<AnalyticsModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleAnalyticsSummaryDto? Analytics { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Analytics = await _bundleService.GetAnalyticsSummaryAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundle analytics");
            ErrorMessage = "Failed to load analytics. Please try again.";
        }
    }
}
