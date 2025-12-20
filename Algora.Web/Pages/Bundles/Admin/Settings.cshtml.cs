using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<SettingsModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleSettingsDto? Settings { get; set; }

    [BindProperty]
    public UpdateBundleSettingsDto Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Settings = await _bundleService.GetSettingsAsync(_shopContext.ShopDomain);

            Input = new UpdateBundleSettingsDto
            {
                IsEnabled = Settings.IsEnabled,
                DefaultDiscountType = Settings.DefaultDiscountType,
                DefaultDiscountValue = Settings.DefaultDiscountValue,
                ShowInventoryWarnings = Settings.ShowInventoryWarnings,
                LowInventoryThreshold = Settings.LowInventoryThreshold,
                BundlePageTitle = Settings.BundlePageTitle,
                BundlePageDescription = Settings.BundlePageDescription,
                ShowOnStorefront = Settings.ShowOnStorefront,
                DisplayLayout = Settings.DisplayLayout
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundle settings");
            ErrorMessage = "Failed to load settings. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Settings = await _bundleService.UpdateSettingsAsync(_shopContext.ShopDomain, Input);
            SuccessMessage = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving bundle settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        return Page();
    }
}
