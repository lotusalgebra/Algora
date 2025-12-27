using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations.Locations;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILocationService _locationService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ILocationService locationService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _locationService = locationService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

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
            var locations = await _locationService.GetLocationsAsync(_shopContext.ShopDomain);
            var allLocations = locations.ToList();
            var totalRecords = allLocations.Count;

            var filtered = allLocations.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(l =>
                    (l.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Address1?.ToLower().Contains(searchLower) ?? false) ||
                    (l.City?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(l => l.Name).ToList()
                    : filteredList.OrderByDescending(l => l.Name).ToList(),
                2 => sortDirection == "asc"
                    ? filteredList.OrderBy(l => l.TotalInventory).ToList()
                    : filteredList.OrderByDescending(l => l.TotalInventory).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(l => l.TotalProducts).ToList()
                    : filteredList.OrderByDescending(l => l.TotalProducts).ToList(),
                _ => filteredList.OrderBy(l => l.Name).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(l => new
                {
                    id = l.Id,
                    shopifyLocationId = l.ShopifyLocationId,
                    name = l.Name,
                    address = l.Address1,
                    city = l.City,
                    province = l.Province,
                    country = l.Country,
                    totalInventory = l.TotalInventory,
                    productCount = l.TotalProducts,
                    isActive = l.IsActive
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
            _logger.LogError(ex, "Failed to load locations data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load locations"
            });
        }
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            await _locationService.SyncLocationsFromShopifyAsync(_shopContext.ShopDomain);
            TempData["SuccessMessage"] = "Locations synced from Shopify.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing locations");
            TempData["ErrorMessage"] = "Failed to sync locations. Please try again.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSyncInventoryAsync()
    {
        try
        {
            await _locationService.SyncInventoryLevelsAsync(_shopContext.ShopDomain);
            TempData["SuccessMessage"] = "Inventory levels synced from Shopify.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory levels");
            TempData["ErrorMessage"] = "Failed to sync inventory levels. Please try again.";
        }

        return RedirectToPage();
    }
}
