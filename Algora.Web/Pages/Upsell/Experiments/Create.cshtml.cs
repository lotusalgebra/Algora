using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Experiments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IUpsellExperimentService _experimentService;
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IUpsellExperimentService experimentService,
        IUpsellRecommendationService recommendationService,
        IShopContext shopContext,
        ILogger<CreateModel> logger)
    {
        _experimentService = experimentService;
        _recommendationService = recommendationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty]
    public CreateExperimentDto Input { get; set; } = new()
    {
        TrafficPercentage = 100,
        MinSampleSize = 100,
        SignificanceLevel = 0.05m,
        AutoSelectWinner = true
    };

    [BindProperty]
    public List<int> OfferIds { get; set; } = new();

    public List<UpsellOfferDto> AvailableOffers { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOffersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (OfferIds.Count < 2)
            {
                ErrorMessage = "Please select at least 2 offers for the experiment.";
                await LoadOffersAsync();
                return Page();
            }

            Input.OfferIds = OfferIds;
            await _experimentService.CreateExperimentAsync(_shopContext.ShopDomain, Input);
            TempData["SuccessMessage"] = "Experiment created successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating experiment");
            ErrorMessage = "Failed to create experiment. Please try again.";
            await LoadOffersAsync();
            return Page();
        }
    }

    private async Task LoadOffersAsync()
    {
        var result = await _recommendationService.GetOffersAsync(_shopContext.ShopDomain, true, 1, 100);
        AvailableOffers = result.Items;
    }
}
