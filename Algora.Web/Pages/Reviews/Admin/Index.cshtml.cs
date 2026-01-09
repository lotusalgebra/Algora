using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reviews.Admin;

[Authorize]
[RequireFeature(FeatureCodes.Reviews)]
public class IndexModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IReviewService reviewService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _reviewService = reviewService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ReviewAnalyticsSummaryDto Analytics { get; set; } = new();
    public List<ReviewListDto> RecentReviews { get; set; } = new();
    public int PendingCount { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Analytics = await _reviewService.GetAnalyticsSummaryAsync(_shopContext.ShopDomain);
            PendingCount = await _reviewService.GetPendingReviewCountAsync(_shopContext.ShopDomain);

            // Get recent reviews pending moderation
            var filter = new ReviewFilterDto
            {
                Status = "pending",
                Page = 1,
                PageSize = 5,
                SortDescending = true
            };
            var result = await _reviewService.GetReviewsAsync(_shopContext.ShopDomain, filter);
            RecentReviews = result.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading review dashboard");
            ErrorMessage = "Failed to load review data. Please try again.";
        }
    }
}
