using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class EditModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IBundleShopifyService _shopifyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IBundleService bundleService,
        IBundleShopifyService shopifyService,
        IShopContext shopContext,
        ILogger<EditModel> logger)
    {
        _bundleService = bundleService;
        _shopifyService = shopifyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public BundleDto? Bundle { get; set; }

    [BindProperty]
    public UpdateBundleDto Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Bundle = await _bundleService.GetBundleByIdAsync(id);

        if (Bundle == null)
        {
            return NotFound();
        }

        // Map to input model
        Input = new UpdateBundleDto
        {
            Name = Bundle.Name,
            Description = Bundle.Description,
            Status = Bundle.Status,
            DiscountType = Bundle.DiscountType,
            DiscountValue = Bundle.DiscountValue,
            DiscountCode = Bundle.DiscountCode,
            ImageUrl = Bundle.ImageUrl,
            MinItems = Bundle.MinItems,
            MaxItems = Bundle.MaxItems,
            IsActive = Bundle.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Bundle = await _bundleService.GetBundleByIdAsync(id);

        if (Bundle == null)
        {
            return NotFound();
        }

        try
        {
            var result = await _bundleService.UpdateBundleAsync(id, Input);
            if (result != null)
            {
                SuccessMessage = "Bundle updated successfully.";
                Bundle = result;
            }
            else
            {
                ErrorMessage = "Failed to update bundle.";
            }
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle {BundleId}", id);
            ErrorMessage = "Failed to update bundle. Please try again.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSyncShopifyAsync(int id)
    {
        try
        {
            var result = await _shopifyService.SyncBundleToShopifyAsync(id);
            if (result != null)
            {
                SuccessMessage = result.ShopifySyncStatus == "synced"
                    ? "Bundle synced to Shopify successfully."
                    : $"Sync failed: {result.ShopifySyncError}";
            }
            else
            {
                ErrorMessage = "Bundle not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bundle {BundleId} to Shopify", id);
            ErrorMessage = "Failed to sync to Shopify. Please try again.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveShopifyAsync(int id)
    {
        try
        {
            var success = await _shopifyService.RemoveBundleFromShopifyAsync(id);
            SuccessMessage = success
                ? "Bundle removed from Shopify successfully."
                : "Failed to remove bundle from Shopify.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle {BundleId} from Shopify", id);
            ErrorMessage = "Failed to remove from Shopify. Please try again.";
        }

        return RedirectToPage(new { id });
    }
}
