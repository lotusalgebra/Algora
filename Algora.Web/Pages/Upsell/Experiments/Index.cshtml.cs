using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Experiments;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUpsellExperimentService _experimentService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUpsellExperimentService experimentService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _experimentService = experimentService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<UpsellExperimentDto> Experiments { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var result = await _experimentService.GetExperimentsAsync(_shopContext.ShopDomain, null, 1, 100);
            Experiments = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading experiments");
            ErrorMessage = "Failed to load experiments. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostStartAsync(int id)
    {
        try
        {
            await _experimentService.StartExperimentAsync(id);
            TempData["SuccessMessage"] = "Experiment started successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting experiment {ExperimentId}", id);
            TempData["ErrorMessage"] = "Failed to start experiment.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPauseAsync(int id)
    {
        try
        {
            await _experimentService.PauseExperimentAsync(id);
            TempData["SuccessMessage"] = "Experiment paused successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing experiment {ExperimentId}", id);
            TempData["ErrorMessage"] = "Failed to pause experiment.";
        }

        return RedirectToPage();
    }
}
