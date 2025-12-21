using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Offers;

[Authorize]
public class EditModel : PageModel
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IUpsellRecommendationService recommendationService,
        IShopContext shopContext,
        ILogger<EditModel> logger)
    {
        _recommendationService = recommendationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public CreateUpsellOfferDto Input { get; set; } = new();

    [BindProperty]
    public string? TriggerProductIdsText { get; set; }

    public UpsellOfferDto? Offer { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Offer = await _recommendationService.GetOfferByIdAsync(Id);
            if (Offer == null)
            {
                return NotFound();
            }

            // Map to input
            Input = new CreateUpsellOfferDto
            {
                RecommendedProductId = Offer.RecommendedProductId,
                RecommendedVariantId = Offer.RecommendedVariantId,
                TriggerProductIds = Offer.TriggerProductIds,
                DiscountType = Offer.DiscountType,
                DiscountValue = Offer.DiscountValue,
                DiscountCode = Offer.DiscountCode,
                Headline = Offer.Headline,
                BodyText = Offer.BodyText,
                ButtonText = Offer.ButtonText,
                Priority = Offer.Priority,
                StartDate = Offer.StartDate,
                EndDate = Offer.EndDate,
                IsActive = Offer.IsActive
            };

            if (Offer.TriggerProductIds?.Count > 0)
            {
                TriggerProductIdsText = string.Join(", ", Offer.TriggerProductIds);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading offer {OfferId}", Id);
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Parse trigger product IDs
            if (!string.IsNullOrWhiteSpace(TriggerProductIdsText))
            {
                Input.TriggerProductIds = TriggerProductIdsText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }
            else
            {
                Input.TriggerProductIds = new List<long>();
            }

            await _recommendationService.UpdateOfferAsync(Id, Input);
            TempData["SuccessMessage"] = "Offer updated successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offer {OfferId}", Id);
            ErrorMessage = "Failed to update offer. Please try again.";

            // Reload offer data for display
            Offer = await _recommendationService.GetOfferByIdAsync(Id);
            return Page();
        }
    }
}
