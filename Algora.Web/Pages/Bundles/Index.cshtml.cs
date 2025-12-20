using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles;

/// <summary>
/// Customer-facing bundle listing page.
/// This page is PUBLIC (no authorization).
/// </summary>
public class IndexModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<BundleDto> Bundles { get; set; } = new();
    public BundleSettingsDto? Settings { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Settings = await _bundleService.GetSettingsAsync(_shopContext.ShopDomain);

            if (Settings == null || !Settings.IsEnabled || !Settings.ShowOnStorefront)
            {
                return NotFound();
            }

            Bundles = await _bundleService.GetActiveBundlesAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundles page");
            ErrorMessage = "Unable to load bundles.";
        }

        return Page();
    }
}
