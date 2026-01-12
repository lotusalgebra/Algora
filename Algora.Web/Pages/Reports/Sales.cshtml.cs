using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class SalesModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public SalesModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "day";

    public SalesReportDto Report { get; set; } = null!;
    public List<SalesByPeriodDto> PeriodData { get; set; } = new();

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
            Report = await _reportingService.GetSalesReportAsync(shopDomain, request);
            PeriodData = await _reportingService.GetSalesByPeriodAsync(shopDomain, request, Period);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sales report error: {ex.Message}");
            Report = new SalesReportDto(0, 0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, decimal>(),
                new Dictionary<string, int>(),
                new List<TimeSeriesDataPoint>(),
                new List<TimeSeriesDataPoint>());
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

        var request = new DateRangeRequest(StartDate, EndDate);
        var data = await _reportingService.ExportSalesReportAsync(shopDomain, request, format);

        return File(data, "text/csv", $"sales-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
