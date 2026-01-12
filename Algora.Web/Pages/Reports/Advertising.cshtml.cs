using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class AdvertisingModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public AdvertisingModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public AdvertisingReportDto Report { get; set; } = null!;
    public List<CampaignPerformanceDto> TopCampaigns { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        // Ensure valid date range
        if (EndDate < StartDate)
            (StartDate, EndDate) = (EndDate, StartDate);

        try
        {
            var request = new DateRangeRequest(StartDate, EndDate);
            Report = await _reportingService.GetAdvertisingReportAsync(shopDomain, request);
            TopCampaigns = await _reportingService.GetCampaignPerformanceAsync(shopDomain, request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Advertising report error: {ex.Message}");
            Report = new AdvertisingReportDto(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, AdPlatformMetricsDto>(),
                new List<CampaignPerformanceDto>(),
                new List<TimeSeriesDataPoint>()
            );
        }

        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(string format)
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        // Ensure valid date range
        if (EndDate < StartDate)
            (StartDate, EndDate) = (EndDate, StartDate);

        // Use product report export as a fallback
        var request = new DateRangeRequest(StartDate, EndDate);
        var data = await _reportingService.ExportProductReportAsync(shopDomain, request, format);

        return File(data, "text/csv", $"advertising-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
