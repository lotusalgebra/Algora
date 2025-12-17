using Algora.Core.Models;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.Products;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ShopifyProductGraphService _productService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ShopifyProductGraphService productService, ILogger<IndexModel> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public List<ProductViewModel> Products { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Products";

        try
        {
            _logger.LogInformation("Fetching products from Shopify");
            Products = await _productService.GetAllProductsAsync();
            _logger.LogInformation("Retrieved {Count} products", Products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load products");
            ErrorMessage = "Failed to load products. Please ensure the shop is connected.";
        }
    }
}
