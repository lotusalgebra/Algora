using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class FinancialModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public FinancialModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public FinancialReportDto Report { get; set; } = null!;

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
            Report = await _reportingService.GetFinancialReportAsync(shopDomain, request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Financial report error: {ex.Message}");
            Report = new FinancialReportDto(
                0, 0, 0, 0, 0, 0, 0, 0, 0,
                new List<TimeSeriesDataPoint>(),
                new List<TimeSeriesDataPoint>(),
                new Dictionary<string, decimal>(),
                new List<RefundSummaryDto>()
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

        // Use sales report export as financial data is derived from sales
        var request = new DateRangeRequest(StartDate, EndDate);
        var data = await _reportingService.ExportSalesReportAsync(shopDomain, request, format);

        return File(data, "text/csv", $"financial-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
