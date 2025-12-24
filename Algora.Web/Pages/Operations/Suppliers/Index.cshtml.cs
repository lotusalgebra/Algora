using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations.Suppliers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ISupplierService _supplierService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISupplierService supplierService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _supplierService = supplierService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<SupplierDto> Suppliers { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            var suppliers = await _supplierService.GetSuppliersAsync(_shopContext.ShopDomain);
            Suppliers = suppliers.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading suppliers");
            ErrorMessage = "Failed to load suppliers. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _supplierService.DeleteSupplierAsync(id);
            TempData["SuccessMessage"] = "Supplier deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
            TempData["ErrorMessage"] = "Failed to delete supplier.";
        }

        return RedirectToPage();
    }
}
