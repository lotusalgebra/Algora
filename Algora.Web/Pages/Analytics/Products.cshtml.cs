using Algora.Application.DTOs.Analytics;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class ProductsModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ProductsModel> _logger;

    public ProductsModel(
        IAnalyticsService analyticsService,
        IShopContext shopContext,
        ILogger<ProductsModel> logger)
    {
        _analyticsService = analyticsService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ProductHeatmapDto ProductHeatmap { get; set; } = new ProductHeatmapDto(
        new List<ProductPerformanceDto>(),
        "revenue",
        0
    );

    [BindProperty(SupportsGet = true)]
    public string SelectedPeriod { get; set; } = "30days";

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "revenue";

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var period = GetAnalyticsPeriod(SelectedPeriod);
            ProductHeatmap = await _analyticsService.GetProductHeatmapAsync(
                _shopContext.ShopDomain,
                period,
                50
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product performance data");
            ErrorMessage = "Failed to load product performance data. Please try again.";
        }
    }

    private AnalyticsTimePeriod GetAnalyticsPeriod(string period)
    {
        return period switch
        {
            "today" => new AnalyticsTimePeriod("today", DateTime.UtcNow.Date, DateTime.UtcNow),
            "7days" => new AnalyticsTimePeriod("7days", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            "30days" => new AnalyticsTimePeriod("30days", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            "90days" => new AnalyticsTimePeriod("90days", DateTime.UtcNow.AddDays(-90), DateTime.UtcNow),
            "12months" => new AnalyticsTimePeriod("12months", DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow),
            _ => new AnalyticsTimePeriod("30days", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow)
        };
    }

    public string GetPeriodLabel()
    {
        return SelectedPeriod switch
        {
            "today" => "Today",
            "7days" => "Last 7 days",
            "30days" => "Last 30 days",
            "90days" => "Last 90 days",
            "12months" => "Last 12 months",
            _ => "Last 30 days"
        };
    }

    public string GetPerformanceColorClass(string performanceLevel)
    {
        return performanceLevel.ToLower() switch
        {
            "excellent" => "from-green-600 to-lime-400",
            "good" => "from-blue-600 to-cyan-400",
            "average" => "from-orange-500 to-yellow-300",
            "poor" => "from-red-600 to-rose-400",
            _ => "from-gray-400 to-gray-600"
        };
    }
}
