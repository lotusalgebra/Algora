using Algora.Application.DTOs;
using Algora.Application.DTOs.Common;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Orders;

[Authorize]
[RequireFeature(FeatureCodes.Orders)]
public class IndexModel : PageModel
{
    private readonly IShopifyOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IShopifyOrderService orderService, ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Orders";
    }

    /// <summary>
    /// AJAX handler for DataTables server-side processing.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? search = null,
        string? statusFilter = null,
        int sortColumn = 0,
        string sortDirection = "desc")
    {
        try
        {
            _logger.LogInformation("Fetching orders page: start={Start}, length={Length}, search={Search}",
                start, length, search);

            // Fetch orders (limited to reasonable amount for performance)
            var allOrders = await _orderService.GetAllAsync(500);
            var ordersList = allOrders.ToList();
            var totalRecords = ordersList.Count;

            // Apply status filter
            var filtered = ordersList.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var statuses = statusFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                filtered = filtered.Where(o =>
                    statuses.Contains(o.FinancialStatus?.ToLower() ?? "unknown") ||
                    (o.FinancialStatus?.ToLower() == "canceled" && statuses.Contains("cancelled")));
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(o =>
                    (o.Name?.ToLower().Contains(searchLower) ?? false) ||
                    o.Id.ToString().Contains(searchLower) ||
                    (o.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (o.Customer != null && ($"{o.Customer.FirstName} {o.Customer.LastName}").ToLower().Contains(searchLower)) ||
                    (o.LineItems?.Any(li => li.Title?.ToLower().Contains(searchLower) ?? false) ?? false));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            // Apply sorting
            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.Name).ToList()
                    : filteredList.OrderByDescending(o => o.Name).ToList(),
                1 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.CreatedAt).ToList()
                    : filteredList.OrderByDescending(o => o.CreatedAt).ToList(),
                5 => sortDirection == "asc"
                    ? filteredList.OrderBy(o => o.TotalPrice).ToList()
                    : filteredList.OrderByDescending(o => o.TotalPrice).ToList(),
                _ => filteredList.OrderByDescending(o => o.CreatedAt).ToList()
            };

            // Apply pagination
            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(o =>
                {
                    var customerName = o.Customer != null
                        ? $"{o.Customer.FirstName} {o.Customer.LastName}".Trim()
                        : (o.Email ?? "Guest");

                    var firstProduct = o.LineItems?.FirstOrDefault();
                    var productCount = o.LineItems?.Count() ?? 0;
                    var productDisplay = firstProduct != null
                        ? (productCount > 1 ? $"{firstProduct.Title} (+{productCount - 1})" : firstProduct.Title)
                        : "No products";

                    return new
                    {
                        id = o.Id,
                        name = o.Name,
                        createdAt = o.CreatedAt.ToString("MMM dd, yyyy"),
                        createdAtTime = o.CreatedAt.ToString("h:mm tt"),
                        status = o.FinancialStatus ?? "Unknown",
                        statusClass = GetStatusClass(o.FinancialStatus),
                        customerName = customerName,
                        email = o.Email ?? "No email",
                        product = productDisplay?.Length > 30 ? productDisplay.Substring(0, 30) + "..." : productDisplay,
                        productFull = firstProduct?.Title ?? "",
                        quantity = firstProduct?.Quantity ?? 0,
                        totalQuantity = o.LineItems?.Sum(li => li.Quantity) ?? 0,
                        productCount = productCount,
                        totalPrice = o.TotalPrice,
                        totalPriceFormatted = o.TotalPrice.ToString("C")
                    };
                })
                .ToList();

            var response = new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredCount,
                Data = pagedData
            };

            return new JsonResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load orders data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load orders"
            });
        }
    }

    private static string GetStatusClass(string? status)
    {
        return status?.ToLower() switch
        {
            "paid" => "from-green-600 to-lime-400",
            "pending" => "from-yellow-500 to-yellow-300",
            "refunded" => "from-red-600 to-rose-400",
            "cancelled" or "canceled" => "from-slate-600 to-slate-400",
            "voided" => "from-slate-600 to-slate-400",
            _ => "from-blue-600 to-cyan-400"
        };
    }
}
