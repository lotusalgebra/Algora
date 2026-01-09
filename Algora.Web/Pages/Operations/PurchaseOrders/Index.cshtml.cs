using Algora.Application.DTOs.Common;
using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Operations.PurchaseOrders;

[Authorize]
[RequireFeature(FeatureCodes.PurchaseOrders)]
public class IndexModel : PageModel
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPurchaseOrderService purchaseOrderService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _purchaseOrderService = purchaseOrderService;
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
        string? statusFilter = null,
        int sortColumn = 0,
        string sortDirection = "desc")
    {
        try
        {
            var filter = new PurchaseOrderFilterDto { Status = statusFilter };
            var orders = await _purchaseOrderService.GetPurchaseOrdersAsync(_shopContext.ShopDomain, filter);
            var allOrders = orders.ToList();
            var totalRecords = allOrders.Count;

            var filtered = allOrders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(o =>
                    (o.OrderNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (o.SupplierName?.ToLower().Contains(searchLower) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.OrderNumber).ToList()
                    : filteredList.OrderByDescending(o => o.OrderNumber).ToList(),
                1 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.SupplierName).ToList()
                    : filteredList.OrderByDescending(o => o.SupplierName).ToList(),
                3 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.Total).ToList()
                    : filteredList.OrderByDescending(o => o.Total).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.CreatedAt).ToList()
                    : filteredList.OrderByDescending(o => o.CreatedAt).ToList(),
                _ => filteredList.OrderByDescending(o => o.CreatedAt).ToList()
            };

            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber,
                    supplierName = o.SupplierName,
                    lineCount = o.Lines?.Count ?? 0,
                    totalAmount = o.Total.ToString("N2"),
                    createdAt = o.CreatedAt.ToString("MMM dd, yyyy"),
                    expectedDelivery = o.ExpectedDeliveryDate?.ToString("MMM dd, yyyy") ?? "-",
                    status = o.Status,
                    statusClass = GetStatusClass(o.Status)
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
            _logger.LogError(ex, "Failed to load purchase orders data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load purchase orders"
            });
        }
    }

    private static string GetStatusClass(string? status)
    {
        return status?.ToLower() switch
        {
            "draft" => "from-gray-400 to-gray-600",
            "pending" => "from-yellow-500 to-amber-300",
            "confirmed" => "from-blue-600 to-cyan-400",
            "shipped" => "from-indigo-600 to-purple-400",
            "received" => "from-green-600 to-lime-400",
            "cancelled" => "from-red-600 to-rose-400",
            _ => "from-gray-400 to-gray-600"
        };
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try
        {
            await _purchaseOrderService.CancelPurchaseOrderAsync(id, "Cancelled by user");
            TempData["SuccessMessage"] = "Purchase order cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling purchase order {OrderId}", id);
            TempData["ErrorMessage"] = "Failed to cancel purchase order.";
        }

        return RedirectToPage();
    }
}
