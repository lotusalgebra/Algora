using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public IndexModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public ReportingDashboardDto Dashboard { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        try
        {
            var request = new DateRangeRequest(StartDate, EndDate);
            Dashboard = await _reportingService.GetDashboardAsync(shopDomain, request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reports dashboard error: {ex.Message}");
            Dashboard = new ReportingDashboardDto(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                new List<TimeSeriesDataPoint>(),
                new List<TimeSeriesDataPoint>(),
                new List<ProductPerformanceDto>(),
                new List<TopCustomerDto>()
            );
        }

        return Page();
    }
}
