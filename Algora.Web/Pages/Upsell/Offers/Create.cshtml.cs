using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Offers;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IUpsellRecommendationService recommendationService,
        IShopContext shopContext,
        ILogger<CreateModel> logger)
    {
        _recommendationService = recommendationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty]
    public CreateUpsellOfferDto Input { get; set; } = new();

    [BindProperty]
    public string? TriggerProductIdsText { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
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

            await _recommendationService.CreateOfferAsync(_shopContext.ShopDomain, Input);
            TempData["SuccessMessage"] = "Offer created successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating upsell offer");
            ErrorMessage = "Failed to create offer. Please check the form and try again.";
            return Page();
        }
    }
}
