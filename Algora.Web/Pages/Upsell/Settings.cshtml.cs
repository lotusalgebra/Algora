using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        IUpsellRecommendationService recommendationService,
        IShopContext shopContext,
        ILogger<SettingsModel> logger)
    {
        _recommendationService = recommendationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty]
    public UpdateUpsellSettingsDto Settings { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var settings = await _recommendationService.GetSettingsAsync(_shopContext.ShopDomain);
            Settings = new UpdateUpsellSettingsDto
            {
                PageTitle = settings.PageTitle,
                ThankYouMessage = settings.ThankYouMessage,
                UpsellSectionTitle = settings.UpsellSectionTitle,
                DisplayLayout = settings.DisplayLayout,
                LogoUrl = settings.LogoUrl,
                PrimaryColor = settings.PrimaryColor ?? "#4f46e5",
                SecondaryColor = settings.SecondaryColor ?? "#6366f1",
                MaxOffersToShow = settings.MaxOffersToShow,
                AffinityLookbackDays = settings.AffinityLookbackDays,
                MinAffinityConfidence = settings.MinAffinityConfidence,
                EnableAutoRecommendations = settings.EnableAutoRecommendations,
                EnableAbTesting = settings.EnableAbTesting,
                TrackImpressions = settings.TrackImpressions,
                CustomCss = settings.CustomCss
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading upsell settings");
            ErrorMessage = "Failed to load settings.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _recommendationService.UpdateSettingsAsync(_shopContext.ShopDomain, Settings);
            TempData["SuccessMessage"] = "Settings saved successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving upsell settings");
            ErrorMessage = "Failed to save settings. Please try again.";
            return Page();
        }
    }
}
