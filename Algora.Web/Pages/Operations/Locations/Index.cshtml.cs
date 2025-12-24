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

    public List<LocationDto> Locations { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var locations = await _locationService.GetLocationsAsync(_shopContext.ShopDomain);
            Locations = locations.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading locations");
            ErrorMessage = "Failed to load locations. Please try again.";
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
