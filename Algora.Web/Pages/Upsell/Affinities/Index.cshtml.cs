using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Affinities;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IProductAffinityService _affinityService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProductAffinityService affinityService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _affinityService = affinityService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public AffinitySummaryDto Summary { get; set; } = new();
    public List<ProductAffinityDto> Affinities { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            Summary = await _affinityService.GetAffinitySummaryAsync(_shopContext.ShopDomain);

            var result = await _affinityService.GetAllAffinitiesAsync(_shopContext.ShopDomain, null, 1, 100);
            Affinities = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading affinities");
            ErrorMessage = "Failed to load product affinities. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostRecalculateAsync()
    {
        try
        {
            var count = await _affinityService.CalculateAffinitiesAsync(_shopContext.ShopDomain, 90);
            TempData["SuccessMessage"] = $"Successfully calculated {count} product affinities.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating affinities");
            TempData["ErrorMessage"] = "Failed to recalculate affinities.";
        }

        return RedirectToPage();
    }
}
