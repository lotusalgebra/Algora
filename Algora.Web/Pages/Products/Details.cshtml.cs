using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly IShopifyProductService _productService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IShopifyProductService productService, ILogger<DetailsModel> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public ProductDto? Product { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            try
            {
                Product = await _productService.GetProductByIdAsync(id);
                if (Product == null)
                {
                    return NotFound();
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId}", id);
                ErrorMessage = "Error loading product details.";
                return Page();
            }
        }
    }
}
