using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Experiments;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IUpsellExperimentService _experimentService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IUpsellExperimentService experimentService,
        ILogger<DetailsModel> logger)
    {
        _experimentService = experimentService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public UpsellExperimentDto? Experiment { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (TempData["SuccessMessage"] != null)
            SuccessMessage = TempData["SuccessMessage"]?.ToString();

        Experiment = await _experimentService.GetExperimentAsync(Id);
        if (Experiment == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostPauseAsync()
    {
        try
        {
            await _experimentService.PauseExperimentAsync(Id);
            TempData["SuccessMessage"] = "Experiment paused.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing experiment {ExperimentId}", Id);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostEndAsync()
    {
        try
        {
            await _experimentService.EndExperimentAsync(Id, null);
            TempData["SuccessMessage"] = "Experiment ended.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending experiment {ExperimentId}", Id);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostSelectWinnerAsync(string variant)
    {
        try
        {
            await _experimentService.EndExperimentAsync(Id, variant);
            TempData["SuccessMessage"] = $"Variant '{variant}' selected as winner.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting winner for experiment {ExperimentId}", Id);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRecalculateAsync()
    {
        try
        {
            await _experimentService.RecalculateStatisticsAsync(Id);
            TempData["SuccessMessage"] = "Statistics recalculated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating statistics for experiment {ExperimentId}", Id);
        }

        return RedirectToPage(new { id = Id });
    }
}
