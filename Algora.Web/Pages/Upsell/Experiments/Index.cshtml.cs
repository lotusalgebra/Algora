using Algora.Application.DTOs.Common;
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

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        if (TempData["SuccessMessage"] != null)
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
    }

    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 25,
        string? search = null,
        int sortColumn = 0,
        string sortDirection = "asc")
    {
        try
        {
            var result = await _experimentService.GetExperimentsAsync(_shopContext.ShopDomain, null, 1, 500);
            var allExperiments = result.Items;
            var totalRecords = allExperiments.Count;

            var filtered = allExperiments.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(e =>
                    (e.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (e.Description?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(e => e.Name).ToList()
                    : filteredList.OrderByDescending(e => e.Name).ToList(),
                2 => sortDirection == "asc"
                    ? filteredList.OrderBy(e => e.TotalImpressions).ToList()
                    : filteredList.OrderByDescending(e => e.TotalImpressions).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(e => e.TotalConversions).ToList()
                    : filteredList.OrderByDescending(e => e.TotalConversions).ToList(),
                _ => filteredList.OrderByDescending(e => e.TotalImpressions).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    description = e.Description,
                    variantCount = e.VariantCount,
                    totalImpressions = e.TotalImpressions,
                    totalConversions = e.TotalConversions,
                    status = e.Status,
                    statusClass = GetStatusClass(e.Status),
                    isStatisticallySignificant = e.IsStatisticallySignificant
                })
                .ToList();

            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredCount,
                Data = pagedData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load experiments data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load experiments"
            });
        }
    }

    private static string GetStatusClass(string? status)
    {
        return status switch
        {
            "running" => "from-green-600 to-lime-400",
            "paused" => "from-yellow-500 to-amber-300",
            "completed" => "from-blue-600 to-cyan-400",
            _ => "from-gray-400 to-gray-600"
        };
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
