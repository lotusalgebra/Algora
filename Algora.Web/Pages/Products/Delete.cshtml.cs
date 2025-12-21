using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Products
{
    public class DeleteModel : PageModel
    {
        private readonly IShopifyProductService _productService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(IShopifyProductService productService, ILogger<DeleteModel> logger)
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
                _logger.LogError(ex, "Error loading product {ProductId} for deletion", id);
                ErrorMessage = $"Error loading product: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                var productTitle = product?.Title ?? $"#{id}";

                await _productService.DeleteProductAsync(id);
                _logger.LogInformation("Product {ProductId} deleted successfully", id);

                TempData["SuccessMessage"] = $"Product '{productTitle}' deleted successfully!";
                return RedirectToPage("/Products/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                ErrorMessage = $"Error deleting product: {ex.Message}";

                // Reload product for display
                try
                {
                    Product = await _productService.GetProductByIdAsync(id);
                }
                catch { }

                return Page();
            }
        }
    }
}
