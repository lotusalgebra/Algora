using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Inventory;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IInventoryPredictionService _predictionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IInventoryPredictionService predictionService, ILogger<IndexModel> logger)
    {
        _predictionService = predictionService;
        _logger = logger;
    }

    public InventoryPredictionSummaryDto? Summary { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            Summary = await _predictionService.GetPredictionSummaryAsync(HttpContext.GetShopDomain());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inventory predictions");
            ErrorMessage = "Failed to load inventory predictions. Please try again.";
        }
    }

    /// <summary>
    /// AJAX handler for DataTables server-side processing.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 25,
        string? search = null,
        string? statusFilter = null,
        int sortColumn = 0,
        string sortDirection = "asc")
    {
        try
        {
            _logger.LogInformation("Fetching inventory predictions: start={Start}, length={Length}, search={Search}, status={Status}",
                start, length, search, statusFilter);

            // Fetch all predictions
            var result = await _predictionService.GetPredictionsAsync(
                HttpContext.GetShopDomain(),
                null,
                1,
                1000);

            var allPredictions = result.Items;
            var totalRecords = allPredictions.Count;

            // Apply status filter
            var filtered = allPredictions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var statuses = statusFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                filtered = filtered.Where(p => statuses.Contains(p.Status?.ToLower() ?? "healthy"));
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductTitle?.ToLower().Contains(searchLower) ?? false) ||
                    (p.VariantTitle?.ToLower().Contains(searchLower) ?? false) ||
                    (p.Sku?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            // Apply sorting
            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.ProductTitle).ToList()
                    : filteredList.OrderByDescending(p => p.ProductTitle).ToList(),
                2 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.CurrentQuantity).ToList()
                    : filteredList.OrderByDescending(p => p.CurrentQuantity).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.AverageDailySales).ToList()
                    : filteredList.OrderByDescending(p => p.AverageDailySales).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.DaysUntilStockout).ToList()
                    : filteredList.OrderByDescending(p => p.DaysUntilStockout).ToList(),
                5 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.SuggestedReorderQuantity).ToList()
                    : filteredList.OrderByDescending(p => p.SuggestedReorderQuantity).ToList(),
                _ => filteredList.OrderBy(p => p.DaysUntilStockout).ToList()
            };

            // Apply pagination
            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    productTitle = p.ProductTitle,
                    variantTitle = p.VariantTitle,
                    sku = p.Sku ?? "-",
                    currentQuantity = p.CurrentQuantity,
                    averageDailySales = p.AverageDailySales.ToString("F2"),
                    daysUntilStockout = p.DaysUntilStockout >= 9999 ? "∞" : p.DaysUntilStockout.ToString(),
                    daysUntilStockoutRaw = p.DaysUntilStockout,
                    suggestedReorderQuantity = p.SuggestedReorderQuantity,
                    status = p.Status ?? "healthy",
                    statusText = GetStatusText(p.Status),
                    statusClass = GetStatusClass(p.Status),
                    confidenceLevel = p.ConfidenceLevel ?? "low",
                    confidenceClass = GetConfidenceClass(p.ConfidenceLevel)
                })
                .ToList();

            var response = new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredCount,
                Data = pagedData
            };

            return new JsonResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load inventory predictions data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load predictions"
            });
        }
    }

    private static string GetStatusText(string? status)
    {
        return status?.ToLower() switch
        {
            "out_of_stock" => "Out of Stock",
            "critical" => "Critical",
            "low_stock" => "Low Stock",
            _ => "Healthy"
        };
    }

    private static string GetStatusClass(string? status)
    {
        return status?.ToLower() switch
        {
            "out_of_stock" => "from-red-600 to-rose-400",
            "critical" => "from-orange-500 to-yellow-300",
            "low_stock" => "from-yellow-500 to-amber-300",
            _ => "from-green-600 to-lime-400"
        };
    }

    private static string GetConfidenceClass(string? confidence)
    {
        return confidence?.ToLower() switch
        {
            "high" => "from-green-600 to-lime-400",
            "medium" => "from-yellow-500 to-amber-300",
            _ => "from-gray-400 to-gray-600"
        };
    }

    public async Task<IActionResult> OnPostRecalculateAsync()
    {
        try
        {
            var count = await _predictionService.CalculatePredictionsAsync(HttpContext.GetShopDomain());
            TempData["SuccessMessage"] = $"Successfully recalculated {count} predictions.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating predictions");
            TempData["ErrorMessage"] = "Failed to recalculate predictions.";
        }

        return RedirectToPage();
    }
}

public static class HttpContextExtensions
{
    public static string GetShopDomain(this HttpContext context)
    {
        var shopContext = context.RequestServices.GetRequiredService<IShopContext>();
        return shopContext.ShopDomain;
    }
}
