using Algora.Application.DTOs.Common;
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
            var result = await _recommendationService.GetOffersAsync(_shopContext.ShopDomain, null, 1, 500);
            var allOffers = result.Items;
            var totalRecords = allOffers.Count;

            var filtered = allOffers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(o =>
                    (o.RecommendedProductTitle?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.RecommendedProductTitle).ToList()
                    : filteredList.OrderByDescending(o => o.RecommendedProductTitle).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.Impressions).ToList()
                    : filteredList.OrderByDescending(o => o.Impressions).ToList(),
                5 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.Conversions).ToList()
                    : filteredList.OrderByDescending(o => o.Conversions).ToList(),
                6 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.Revenue).ToList()
                    : filteredList.OrderByDescending(o => o.Revenue).ToList(),
                _ => filteredList.OrderByDescending(o => o.Revenue).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(o => new
                {
                    id = o.Id,
                    productTitle = o.RecommendedProductTitle,
                    productImageUrl = o.RecommendedProductImageUrl,
                    productPrice = o.RecommendedProductPrice.ToString("N2"),
                    triggerCount = o.TriggerProductIds?.Count ?? 0,
                    discountedPrice = o.DiscountedPrice?.ToString("N2"),
                    impressions = o.Impressions,
                    clicks = o.Clicks,
                    conversions = o.Conversions,
                    revenue = o.Revenue.ToString("N2"),
                    isActive = o.IsActive
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
            _logger.LogError(ex, "Failed to load offers data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load offers"
            });
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
