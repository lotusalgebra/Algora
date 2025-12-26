using Algora.Application.DTOs;
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

        [BindProperty]
        public List<IFormFile> NewImageFiles { get; set; } = new();

        [BindProperty]
        public List<string> NewImageUrls { get; set; } = new();

        [BindProperty]
        public List<string> DeleteImageIds { get; set; } = new();

        public List<ProductImageDto> ExistingImages { get; set; } = new();

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
                    Description = product.Description,
                    Vendor = product.Vendor,
                    ProductType = product.ProductType,
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
                    IsNew = false,
                    ImageId = v.ImageId,
                    ImageSrc = v.ImageSrc
                }).ToList();

                if (Variants.Count == 0)
                {
                    Variants.Add(new VariantEditInput());
                }

                // Load existing images
                ExistingImages = product.Images?.ToList() ?? new List<ProductImageDto>();

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
                // Delete images first
                var validDeleteIds = DeleteImageIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();
                foreach (var imageId in validDeleteIds)
                {
                    try
                    {
                        await _productService.DeleteProductImageAsync(Product.Id, imageId);
                        _logger.LogInformation("Deleted image {ImageId} from product {ProductId}", imageId, Product.Id);
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "Failed to delete image {ImageId}", imageId);
                    }
                }

                // Update product info
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

                // Upload new image files
                var validImageFiles = NewImageFiles?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();
                foreach (var file in validImageFiles)
                {
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var base64 = Convert.ToBase64String(memoryStream.ToArray());

                        var uploadInput = new UploadProductImageInput
                        {
                            ProductId = Product.Id,
                            Base64Data = base64,
                            FileName = file.FileName,
                            ContentType = file.ContentType,
                            Alt = Path.GetFileNameWithoutExtension(file.FileName)
                        };

                        await _productService.UploadProductImageAsync(uploadInput);
                        _logger.LogInformation("Uploaded image {FileName} to product {ProductId}", file.FileName, Product.Id);
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "Failed to upload image {FileName}", file.FileName);
                    }
                }

                // Upload new image URLs
                var validImageUrls = NewImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).ToList() ?? new List<string>();
                foreach (var url in validImageUrls)
                {
                    try
                    {
                        var uploadInput = new UploadProductImageInput
                        {
                            ProductId = Product.Id,
                            ImageUrl = url
                        };

                        await _productService.UploadProductImageAsync(uploadInput);
                        _logger.LogInformation("Uploaded image from URL to product {ProductId}", Product.Id);
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "Failed to upload image from URL {Url}", url);
                    }
                }

                // Update variant images
                foreach (var variant in Variants.Where(v => !v.IsNew && !string.IsNullOrEmpty(v.VariantId)))
                {
                    try
                    {
                        // Only update if ImageId is set (even if empty to remove)
                        if (variant.ImageId != null)
                        {
                            var imageIdToSet = string.IsNullOrWhiteSpace(variant.ImageId) ? null : variant.ImageId;
                            await _productService.UpdateVariantImageAsync(Product.Id, variant.VariantId!, imageIdToSet);
                            _logger.LogInformation("Updated variant {VariantId} image to {ImageId}", variant.VariantId, imageIdToSet ?? "none");
                        }
                    }
                    catch (Exception varEx)
                    {
                        _logger.LogWarning(varEx, "Failed to update variant {VariantId} image", variant.VariantId);
                    }
                }

                TempData["SuccessMessage"] = $"Product '{updatedProduct.Title}' updated successfully!";
                return RedirectToPage("/Products/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", Product.Id);
                ErrorMessage = $"Error updating product: {ex.Message}";
                // Reload images on error
                try
                {
                    var product = await _productService.GetProductByIdAsync(Product.Id);
                    ExistingImages = product?.Images?.ToList() ?? new List<ProductImageDto>();
                }
                catch { }
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

        /// <summary>
        /// Image GID associated with this variant.
        /// </summary>
        public string? ImageId { get; set; }

        /// <summary>
        /// Image URL for display (not submitted).
        /// </summary>
        public string? ImageSrc { get; set; }
    }
}
