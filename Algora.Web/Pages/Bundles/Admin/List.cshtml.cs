using Algora.Application.DTOs.Bundles;
using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class ListModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ListModel> _logger;

    public ListModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<ListModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public PaginatedResult<BundleListDto> Bundles { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BundleType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            Bundles = await _bundleService.GetBundlesAsync(
                _shopContext.ShopDomain,
                BundleType,
                Status,
                Search,
                PageNumber,
                20);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bundles list");
            ErrorMessage = "Failed to load bundles. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var success = await _bundleService.DeleteBundleAsync(id);
            if (success)
            {
                SuccessMessage = "Bundle deleted successfully.";
            }
            else
            {
                ErrorMessage = "Bundle not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bundle {BundleId}", id);
            ErrorMessage = "Failed to delete bundle. Please try again.";
        }

        return RedirectToPage();
    }
}
