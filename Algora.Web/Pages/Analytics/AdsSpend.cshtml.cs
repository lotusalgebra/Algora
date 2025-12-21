using Algora.Application.DTOs.Analytics;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class AdsSpendModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<AdsSpendModel> _logger;

    public AdsSpendModel(
        IAnalyticsService analyticsService,
        IShopContext shopContext,
        ILogger<AdsSpendModel> logger)
    {
        _analyticsService = analyticsService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<AdsSpendDto> AdsSpendList { get; set; } = new();
    public AdsSpendSummaryDto Summary { get; set; } = new AdsSpendSummaryDto(
        0, 0, 0, 0,
        new List<AdsPlatformSummaryDto>()
    );

    [BindProperty(SupportsGet = true)]
    public string SelectedPeriod { get; set; } = "30days";

    [BindProperty]
    public AdsSpendInput Input { get; set; } = new();

    [BindProperty]
    public int? EditId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var createDto = new AdsSpendCreateDto(
                Input.Platform,
                Input.CampaignName,
                Input.CampaignId,
                Input.SpendDate,
                Input.Amount,
                Input.Currency,
                Input.Impressions,
                Input.Clicks,
                Input.Conversions,
                Input.Revenue,
                Input.Notes
            );

            await _analyticsService.SaveAdsSpendAsync(_shopContext.ShopDomain, createDto, EditId);

            TempData["SuccessMessage"] = EditId.HasValue
                ? "Ads spend entry updated successfully!"
                : "Ads spend entry created successfully!";

            return RedirectToPage(new { period = SelectedPeriod });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ads spend entry");
            ErrorMessage = $"Failed to save ads spend entry: {ex.Message}";
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var deleted = await _analyticsService.DeleteAdsSpendAsync(_shopContext.ShopDomain, id);

            if (deleted)
            {
                TempData["SuccessMessage"] = "Ads spend entry deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete ads spend entry.";
            }

            return RedirectToPage(new { period = SelectedPeriod });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ads spend entry");
            TempData["ErrorMessage"] = $"Failed to delete ads spend entry: {ex.Message}";
            return RedirectToPage(new { period = SelectedPeriod });
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var period = GetAnalyticsPeriod(SelectedPeriod);

            // Load ads spend data and summary in parallel
            var listTask = _analyticsService.GetAdsSpendAsync(_shopContext.ShopDomain, period);
            var summaryTask = _analyticsService.GetAdsSpendSummaryAsync(_shopContext.ShopDomain, period);

            await Task.WhenAll(listTask, summaryTask);

            AdsSpendList = await listTask;
            Summary = await summaryTask;

            // Check for success message from TempData
            if (TempData["SuccessMessage"] is string successMsg)
            {
                SuccessMessage = successMsg;
            }

            if (TempData["ErrorMessage"] is string errorMsg)
            {
                ErrorMessage = errorMsg;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ads spend data");
            ErrorMessage = "Failed to load ads spend data. Please try again.";
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

public class AdsSpendInput
{
    [Required(ErrorMessage = "Platform is required")]
    public string Platform { get; set; } = "Facebook";

    public string? CampaignName { get; set; }

    public string? CampaignId { get; set; }

    [Required(ErrorMessage = "Spend date is required")]
    public DateTime SpendDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    public string Currency { get; set; } = "USD";

    [Range(0, int.MaxValue, ErrorMessage = "Impressions must be a positive number")]
    public int? Impressions { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Clicks must be a positive number")]
    public int? Clicks { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Conversions must be a positive number")]
    public int? Conversions { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Revenue must be between 0 and 999,999.99")]
    public decimal? Revenue { get; set; }

    public string? Notes { get; set; }
}
