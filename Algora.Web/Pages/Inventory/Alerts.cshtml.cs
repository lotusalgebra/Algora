using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Inventory;

[Authorize]
public class AlertsModel : PageModel
{
    private readonly IInventoryAlertService _alertService;
    private readonly ILogger<AlertsModel> _logger;

    public AlertsModel(IInventoryAlertService alertService, ILogger<AlertsModel> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    public List<InventoryAlertDto> Alerts { get; set; } = new();
    public Dictionary<string, int> AlertCounts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SeverityFilter { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var shopDomain = HttpContext.GetShopDomain();
            AlertCounts = await _alertService.GetAlertCountsBySeverityAsync(shopDomain);

            var result = await _alertService.GetAlertsAsync(
                shopDomain,
                StatusFilter,
                SeverityFilter,
                1,
                100);

            Alerts = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading alerts");
            ErrorMessage = "Failed to load alerts. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        try
        {
            await _alertService.AcknowledgeAlertAsync(id);
            TempData["SuccessMessage"] = "Alert acknowledged.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            TempData["ErrorMessage"] = "Failed to acknowledge alert.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        try
        {
            await _alertService.DismissAlertAsync(id);
            TempData["SuccessMessage"] = "Alert dismissed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", id);
            TempData["ErrorMessage"] = "Failed to dismiss alert.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        try
        {
            await _alertService.ResolveAlertAsync(id);
            TempData["SuccessMessage"] = "Alert resolved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            TempData["ErrorMessage"] = "Failed to resolve alert.";
        }

        return RedirectToPage();
    }
}
