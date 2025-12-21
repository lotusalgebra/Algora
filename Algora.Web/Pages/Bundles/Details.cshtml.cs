using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles;

/// <summary>
/// Customer-facing bundle details page.
/// This page is PUBLIC (no authorization).
/// </summary>
public class DetailsModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<DetailsModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleDto? Bundle { get; set; }
    public BundleSettingsDto? Settings { get; set; }
    public string? ErrorMessage { get; set; }
    public int AvailableQuantity { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        try
        {
            Settings = await _bundleService.GetSettingsAsync(_shopContext.ShopDomain);

            if (Settings == null || !Settings.IsEnabled)
            {
                return NotFound();
            }

            Bundle = await _bundleService.GetBundleBySlugAsync(_shopContext.ShopDomain, slug);

            if (Bundle == null || !Bundle.IsActive || Bundle.Status != "active")
            {
                return NotFound();
            }

            // Redirect mix-and-match bundles to the builder
            if (Bundle.BundleType == "mix_and_match")
            {
                return RedirectToPage("/Bundles/Builder", new { id = Bundle.Id });
            }

            // Calculate available quantity for fixed bundles
            if (Bundle.BundleType == "fixed")
            {
                AvailableQuantity = await _bundleService.CalculateAvailableQuantityAsync(Bundle.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundle details for slug {Slug}", slug);
            ErrorMessage = "Unable to load bundle details.";
        }

        return Page();
    }
}
