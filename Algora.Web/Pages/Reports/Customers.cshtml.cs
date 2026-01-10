using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class CustomersModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public CustomersModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public CustomerReportDto Report { get; set; } = null!;
    public List<TopCustomerDto> TopCustomers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        try
        {
            var request = new DateRangeRequest(StartDate, EndDate);
            Report = await _reportingService.GetCustomerReportAsync(shopDomain, request);
            TopCustomers = await _reportingService.GetTopCustomersAsync(shopDomain, request, 10);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Customers report error: {ex.Message}");
            Report = new CustomerReportDto(
                0, 0, 0, 0,
                new Dictionary<string, int>(),
                new List<TimeSeriesDataPoint>(),
                new List<CustomerSegmentDto>()
            );
        }

        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(string format)
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        var request = new DateRangeRequest(StartDate, EndDate);
        var data = await _reportingService.ExportCustomerReportAsync(shopDomain, request, format);

        return File(data, "text/csv", $"customers-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
