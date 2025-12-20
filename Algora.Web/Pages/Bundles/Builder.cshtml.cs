using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles;

/// <summary>
/// Customer-facing mix-and-match bundle builder page.
/// This page is PUBLIC (no authorization).
/// </summary>
public class BuilderModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<BuilderModel> _logger;

    public BuilderModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<BuilderModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleDto? Bundle { get; set; }
    public BundleSettingsDto? Settings { get; set; }
    public List<EligibleProductDto> EligibleProducts { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Settings = await _bundleService.GetSettingsAsync(_shopContext.ShopDomain);

            if (Settings == null || !Settings.IsEnabled)
            {
                return NotFound();
            }

            Bundle = await _bundleService.GetBundleByIdAsync(id);

            if (Bundle == null || !Bundle.IsActive || Bundle.Status != "active")
            {
                return NotFound();
            }

            if (Bundle.BundleType != "mix_and_match")
            {
                return RedirectToPage("/Bundles/Details", new { slug = Bundle.Slug });
            }

            EligibleProducts = await _bundleService.GetEligibleProductsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundle builder for bundle {BundleId}", id);
            ErrorMessage = "Unable to load bundle builder.";
        }

        return Page();
    }
}
