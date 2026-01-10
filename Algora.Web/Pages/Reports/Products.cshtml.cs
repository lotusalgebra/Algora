using Algora.Application.DTOs.Reporting;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reports;

public class ProductsModel : PageModel
{
    private readonly IReportingService _reportingService;
    private readonly IShopContext _shopContext;

    public ProductsModel(IReportingService reportingService, IShopContext shopContext)
    {
        _reportingService = reportingService;
        _shopContext = shopContext;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public ProductReportSummaryDto Report { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;
        if (string.IsNullOrEmpty(shopDomain))
            return RedirectToPage("/Auth/Login");

        try
        {
            var request = new DateRangeRequest(StartDate, EndDate);
            Report = await _reportingService.GetProductReportAsync(shopDomain, request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Products report error: {ex.Message}");
            Report = new ProductReportSummaryDto(
                0, 0, 0, 0, 0,
                new List<ProductPerformanceDto>(),
                new List<ProductPerformanceDto>(),
                new List<ProductPerformanceDto>(),
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>()
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
        var data = await _reportingService.ExportProductReportAsync(shopDomain, request, format);

        return File(data, "text/csv", $"products-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
