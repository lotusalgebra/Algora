using Algora.Application.DTOs.Common;
using Algora.Core.Models;
using Algora.Infrastructure.Services;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Products;

[Authorize]
[RequireFeature(FeatureCodes.Products)]
public class IndexModel : PageModel
{
    private readonly ShopifyProductGraphService _productService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ShopifyProductGraphService productService, ILogger<IndexModel> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }
    public int TotalProducts { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Products";
    }

    /// <summary>
    /// AJAX handler for DataTables server-side processing.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? search = null,
        int sortColumn = 0,
        string sortDirection = "asc")
    {
        try
        {
            _logger.LogInformation("Fetching products page: start={Start}, length={Length}, search={Search}",
                start, length, search);

            // Fetch all products (Shopify API doesn't support server-side pagination well)
            var allProducts = await _productService.GetAllProductsAsync();
            var totalRecords = allProducts.Count;

            // Apply search filter
            var filtered = allProducts.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filtered = filtered.Where(p =>
                    p.Title.ToLower().Contains(searchLower) ||
                    (p.Vendor?.ToLower().Contains(searchLower) ?? false) ||
                    (p.Tags?.ToLower().Contains(searchLower) ?? false) ||
                    p.Id.ToString().Contains(searchLower));
            }

            var filteredList = filtered.ToList();
            var filteredCount = filteredList.Count;

            // Apply sorting
            filteredList = sortColumn switch
            {
                0 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.Title).ToList()
                    : filteredList.OrderByDescending(p => p.Title).ToList(),
                2 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.Price).ToList()
                    : filteredList.OrderByDescending(p => p.Price).ToList(),
                4 => sortDirection == "asc"
                    ? filteredList.OrderBy(p => p.Stock).ToList()
                    : filteredList.OrderByDescending(p => p.Stock).ToList(),
                _ => filteredList
            };

            // Apply pagination
            var pagedData = filteredList
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    vendor = p.Vendor ?? "No vendor",
                    tags = p.Tags ?? "",
                    price = p.Price,
                    priceFormatted = p.Price.ToString("C"),
                    stock = p.Stock,
                    status = p.Stock > 10 ? "In Stock" : p.Stock > 0 ? "Low Stock" : "Out of Stock",
                    statusClass = p.Stock > 10 ? "from-green-600 to-lime-400" :
                                  p.Stock > 0 ? "from-orange-500 to-yellow-300" : "from-red-600 to-rose-400"
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
            _logger.LogError(ex, "Failed to load products data");
            return new JsonResult(new DataTableResponse<object>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = Enumerable.Empty<object>(),
                Error = "Failed to load products"
            });
        }
    }
}
