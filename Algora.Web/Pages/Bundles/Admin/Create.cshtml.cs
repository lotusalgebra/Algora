using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Bundles.Admin;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IBundleService _bundleService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IBundleService bundleService,
        IShopContext shopContext,
        ILogger<CreateModel> logger)
    {
        _bundleService = bundleService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty]
    public CreateBundleDto Bundle { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Initialize with defaults
        Bundle = new CreateBundleDto
        {
            BundleType = "fixed",
            DiscountType = "percentage",
            DiscountValue = 10,
            IsActive = false
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _bundleService.CreateBundleAsync(_shopContext.ShopDomain, Bundle);
            return RedirectToPage("/Bundles/Admin/Edit", new { id = result.Id });
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bundle");
            ErrorMessage = "Failed to create bundle. Please try again.";
            return Page();
        }
    }
}
