using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Upsell.Affinities;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IProductAffinityService _affinityService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProductAffinityService affinityService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _affinityService = affinityService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public AffinitySummaryDto Summary { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            Summary = await _affinityService.GetAffinitySummaryAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading affinities");
            ErrorMessage = "Failed to load product affinities. Please try again.";
        }
    }

    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 25,
        string? search = null,
        int sortColumn = 0,
        string sortDirection = "desc")
    {
        try
        {
            var result = await _affinityService.GetAllAffinitiesAsync(_shopContext.ShopDomain, null, 1, 1000);
            var allAffinities = result.Items;
            var totalRecords = allAffinities.Count;

            var filtered = allAffinities.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(a =>
                    (a.SourceProductTitle?.ToLower().Contains(searchLower) ?? false) ||
                    (a.RelatedProductTitle?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(a => a.SourceProductTitle).ToList()
                    : filteredList.OrderByDescending(a => a.SourceProductTitle).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(a => a.Confidence).ToList()
                    : filteredList.OrderByDescending(a => a.Confidence).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(a => a.Lift).ToList()
                    : filteredList.OrderByDescending(a => a.Lift).ToList(),
                5 => sortDirection == "asc"
                    ? filteredList.OrderBy(a => a.CoOccurrences).ToList()
                    : filteredList.OrderByDescending(a => a.CoOccurrences).ToList(),
                _ => filteredList.OrderByDescending(a => a.Lift).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(a => new
                {
                    sourceProductTitle = a.SourceProductTitle,
                    sourceProductId = a.SourceProductId,
                    relatedProductTitle = a.RelatedProductTitle,
                    relatedProductId = a.RelatedProductId,
                    support = a.Support.ToString("P2"),
                    confidence = a.Confidence.ToString("P1"),
                    confidenceValue = a.Confidence,
                    lift = a.Lift.ToString("N2"),
                    liftValue = a.Lift,
                    coOccurrences = a.CoOccurrences,
                    strengthText = GetStrengthText(a.Lift),
                    strengthClass = GetStrengthClass(a.Lift)
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
            _logger.LogError(ex, "Failed to load affinities data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load affinities"
            });
        }
    }

    private static string GetStrengthText(decimal lift)
    {
        return lift switch
        {
            >= 2.0m => "Strong",
            >= 1.5m => "Moderate",
            >= 1.0m => "Weak",
            _ => "Negative"
        };
    }

    private static string GetStrengthClass(decimal lift)
    {
        return lift switch
        {
            >= 2.0m => "from-green-600 to-lime-400",
            >= 1.5m => "from-blue-600 to-cyan-400",
            >= 1.0m => "from-yellow-500 to-amber-300",
            _ => "from-gray-400 to-gray-600"
        };
    }

    public async Task<IActionResult> OnPostRecalculateAsync()
    {
        try
        {
            var count = await _affinityService.CalculateAffinitiesAsync(_shopContext.ShopDomain, 90);
            TempData["SuccessMessage"] = $"Successfully calculated {count} product affinities.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating affinities");
            TempData["ErrorMessage"] = "Failed to recalculate affinities.";
        }

        return RedirectToPage();
    }
}
