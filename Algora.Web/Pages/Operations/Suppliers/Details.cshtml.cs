using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations.Suppliers;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ISupplierService supplierService,
        ILogger<DetailsModel> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    public SupplierDto? Supplier { get; set; }
    public List<SupplierProductDto> Products { get; set; } = new();
    public SupplierAnalyticsDto? Analytics { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            Supplier = await _supplierService.GetSupplierAsync(id);
            if (Supplier == null)
            {
                return NotFound();
            }

            Products = (await _supplierService.GetSupplierProductsAsync(id)).ToList();
            Analytics = await _supplierService.GetSupplierAnalyticsAsync(id);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading supplier details {SupplierId}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostRemoveProductAsync(int id, int supplierProductId)
    {
        try
        {
            await _supplierService.RemoveProductFromSupplierAsync(supplierProductId);
            TempData["SuccessMessage"] = "Product removed from supplier.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product from supplier");
            TempData["ErrorMessage"] = "Failed to remove product.";
        }

        return RedirectToPage(new { id });
    }
}
