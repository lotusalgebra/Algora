using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Offers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUpsellRecommendationService recommendationService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _recommendationService = recommendationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<UpsellOfferDto> Offers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var result = await _recommendationService.GetOffersAsync(_shopContext.ShopDomain, null, 1, 100);
            Offers = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading upsell offers");
            ErrorMessage = "Failed to load offers. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _recommendationService.DeleteOfferAsync(id);
            TempData["SuccessMessage"] = "Offer deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting offer {OfferId}", id);
            TempData["ErrorMessage"] = "Failed to delete offer.";
        }

        return RedirectToPage();
    }
}
