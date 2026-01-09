using Algora.Application.DTOs.Analytics;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
[RequireFeature(FeatureCodes.AdvancedReports)]
public class IndexModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IAnalyticsService analyticsService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _analyticsService = analyticsService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public DashboardSummaryDto DashboardSummary { get; set; } = new DashboardSummaryDto(
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    );

    public List<TopProductDto> TopProducts { get; set; } = new();
    public SalesTrendDto SalesTrend { get; set; } = new SalesTrendDto(new List<SalesTrendPointDto>(), 0, 0, 0);
    public CostBreakdownDto CostBreakdown { get; set; } = new CostBreakdownDto(0, 0, 0, 0, 0, 0);

    [BindProperty(SupportsGet = true)]
    public string SelectedPeriod { get; set; } = "30days";

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var period = GetAnalyticsPeriod(SelectedPeriod);

            // Fetch dashboard data sequentially to avoid DbContext threading issues
            DashboardSummary = await _analyticsService.GetDashboardSummaryAsync(_shopContext.ShopDomain, period);
            TopProducts = await _analyticsService.GetTopProductsAsync(_shopContext.ShopDomain, period, "revenue", 5);
            SalesTrend = await _analyticsService.GetSalesTrendAsync(_shopContext.ShopDomain, period);
            CostBreakdown = await _analyticsService.GetCostBreakdownAsync(_shopContext.ShopDomain, period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics dashboard");
            ErrorMessage = "Failed to load analytics data. Please try again.";
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
}
