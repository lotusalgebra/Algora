using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Inventory;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IInventoryPredictionService _predictionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IInventoryPredictionService predictionService, ILogger<IndexModel> logger)
    {
        _predictionService = predictionService;
        _logger = logger;
    }

    public InventoryPredictionSummaryDto? Summary { get; set; }
    public List<InventoryPredictionDto> Predictions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            Summary = await _predictionService.GetPredictionSummaryAsync(HttpContext.GetShopDomain());

            var result = await _predictionService.GetPredictionsAsync(
                HttpContext.GetShopDomain(),
                StatusFilter,
                1,
                100);

            Predictions = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inventory predictions");
            ErrorMessage = "Failed to load inventory predictions. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostRecalculateAsync()
    {
        try
        {
            var count = await _predictionService.CalculatePredictionsAsync(HttpContext.GetShopDomain());
            TempData["SuccessMessage"] = $"Successfully recalculated {count} predictions.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating predictions");
            TempData["ErrorMessage"] = "Failed to recalculate predictions.";
        }

        return RedirectToPage();
    }
}

public static class HttpContextExtensions
{
    public static string GetShopDomain(this HttpContext context)
    {
        var shopContext = context.RequestServices.GetRequiredService<IShopContext>();
        return shopContext.ShopDomain;
    }
}
