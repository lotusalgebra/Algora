using Algora.Application.DTOs.Analytics;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class ClvModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ClvModel> _logger;

    public ClvModel(
        IAnalyticsService analyticsService,
        IShopContext shopContext,
        ILogger<ClvModel> logger)
    {
        _analyticsService = analyticsService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ClvReportDto ClvReport { get; set; } = new ClvReportDto(
        new ClvSummaryDto(0, 0, 0, 0, 0),
        new List<CustomerLifetimeValueDto>(),
        new List<CustomerLifetimeValueDto>(),
        new List<ClvSegmentDto>()
    );

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            ClvReport = await _analyticsService.GetClvReportAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CLV report");
            ErrorMessage = "Failed to load customer lifetime value data. Please try again.";
        }
    }
}
