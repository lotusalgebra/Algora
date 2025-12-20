using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IUpsellExperimentService _experimentService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUpsellRecommendationService recommendationService,
        IUpsellExperimentService experimentService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _recommendationService = recommendationService;
        _experimentService = experimentService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public UpsellDashboardSummary Summary { get; set; } = new();
    public List<UpsellOfferDto> RecentOffers { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Get offers summary
            var offersResult = await _recommendationService.GetOffersAsync(_shopContext.ShopDomain, true, 1, 10);
            RecentOffers = offersResult.Items;

            // Get experiment summary
            var experimentSummary = await _experimentService.GetExperimentSummaryAsync(_shopContext.ShopDomain);

            Summary = new UpsellDashboardSummary
            {
                ActiveOffers = offersResult.TotalCount,
                RunningExperiments = experimentSummary.RunningExperiments,
                TotalConversions = experimentSummary.TotalConversions,
                TotalRevenue = experimentSummary.TotalRevenue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading upsell dashboard");
            ErrorMessage = "Failed to load upsell data. Please try again.";
        }
    }
}

public class UpsellDashboardSummary
{
    public int ActiveOffers { get; set; }
    public int RunningExperiments { get; set; }
    public int TotalConversions { get; set; }
    public decimal TotalRevenue { get; set; }
}
