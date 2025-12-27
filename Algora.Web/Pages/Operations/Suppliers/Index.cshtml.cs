using Algora.Application.DTOs.Common;
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
            var suppliers = await _supplierService.GetSuppliersAsync(_shopContext.ShopDomain);
            var allSuppliers = suppliers.ToList();
            var totalRecords = allSuppliers.Count;

            var filtered = allSuppliers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(s =>
                    (s.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Code?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Email?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(s => s.Name).ToList()
                    : filteredList.OrderByDescending(s => s.Name).ToList(),
                2 => sortDirection == "asc"
                    ? filteredList.OrderBy(s => s.DefaultLeadTimeDays).ToList()
                    : filteredList.OrderByDescending(s => s.DefaultLeadTimeDays).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(s => s.TotalOrders).ToList()
                    : filteredList.OrderByDescending(s => s.TotalOrders).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(s => s.TotalSpent).ToList()
                    : filteredList.OrderByDescending(s => s.TotalSpent).ToList(),
                _ => filteredList.OrderBy(s => s.Name).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    code = s.Code,
                    email = s.Email,
                    phone = s.Phone,
                    leadTimeDays = s.DefaultLeadTimeDays,
                    totalOrders = s.TotalOrders,
                    totalSpent = s.TotalSpent.ToString("N2"),
                    isActive = s.IsActive
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
            _logger.LogError(ex, "Failed to load suppliers data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load suppliers"
            });
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
