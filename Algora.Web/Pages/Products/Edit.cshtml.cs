using Algora.Application.Interfaces;
using Algora.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Products
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ShopifyProductGraphService _productService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ShopifyProductGraphService productService, ILogger<EditModel> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [BindProperty]
        public ProductEditInput Product { get; set; } = new();

        [BindProperty]
        public List<VariantEditInput> Variants { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            try
            {
                _logger.LogInformation("Loading product {ProductId}", id);
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found", id);
                    ErrorMessage = $"Product with ID {id} was not found. Please check if the product exists in Shopify.";
                    return Page();
                }

                Product = new ProductEditInput
                {
                    Id = product.NumericId,
                    Title = product.Title,
                    Tags = string.Join(", ", product.Tags)
                };

                Variants = product.Variants.Select(v => new VariantEditInput
                {
                    VariantId = v.Id,
                    Title = v.Title,
                    Price = v.Price ?? 0,
                    Sku = v.Sku,
                    Option1 = v.Option1,
                    Option2 = v.Option2,
                    Option3 = v.Option3,
                    IsNew = false
                }).ToList();

                if (Variants.Count == 0)
                {
                    Variants.Add(new VariantEditInput());
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId}", id);
                ErrorMessage = $"Error loading product: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var input = new UpdateProductInput
                {
                    ProductId = Product.Id,
                    Title = Product.Title,
                    Description = Product.Description,
                    Vendor = Product.Vendor,
                    ProductType = Product.ProductType,
                    Tags = string.IsNullOrWhiteSpace(Product.Tags)
                        ? new List<string>()
                        : Product.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList(),
                    Variants = Variants.Select(v => new UpdateVariantInput
                    {
                        VariantId = v.VariantId,
                        Title = v.Title,
                        Price = v.Price,
                        Sku = v.Sku,
                        Option1 = v.Option1,
                        Option2 = v.Option2,
                        Option3 = v.Option3,
                        IsNew = v.IsNew
                    }).ToList()
                };

                var updatedProduct = await _productService.UpdateProductAsync(input);
                _logger.LogInformation("Product updated successfully: {ProductId}", updatedProduct.NumericId);

                TempData["SuccessMessage"] = $"Product '{updatedProduct.Title}' updated successfully!";
                return RedirectToPage("/Products/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", Product.Id);
                ErrorMessage = $"Error updating product: {ex.Message}";
                return Page();
            }
        }
    }

    public class ProductEditInput
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Product title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(255)]
        public string? Vendor { get; set; }

        [StringLength(255)]
        public string? ProductType { get; set; }

        public string? Tags { get; set; }
    }

    public class VariantEditInput
    {
        public string? VariantId { get; set; }
        public string? Title { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Price must be between 0 and 999999.99")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string? Sku { get; set; }

        [StringLength(255)]
        public string? Option1 { get; set; }

        [StringLength(255)]
        public string? Option2 { get; set; }

        [StringLength(255)]
        public string? Option3 { get; set; }

        public bool IsNew { get; set; }
    }
}
