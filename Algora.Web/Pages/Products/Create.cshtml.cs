using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Products
{
    public class CreateModel : PageModel
    {
        private readonly IShopifyProductService _productService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IShopifyProductService productService, ILogger<CreateModel> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [BindProperty]
        public ProductInput Product { get; set; } = new();

        [BindProperty]
        public List<VariantInput> Variants { get; set; } = new();

        [BindProperty]
        public List<string> ImageUrls { get; set; } = new();

        [BindProperty]
        public List<IFormFile> ImageFiles { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Don't initialize with default variant - let user choose to add variants
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Filter out empty variants (variants where all key fields are empty)
                var validVariants = Variants
                    .Where(v => v.Price > 0 || !string.IsNullOrWhiteSpace(v.Sku) ||
                                !string.IsNullOrWhiteSpace(v.Option1) ||
                                !string.IsNullOrWhiteSpace(v.Option2) ||
                                !string.IsNullOrWhiteSpace(v.Option3))
                    .ToList();

                // Filter out empty image URLs
                var validImageUrls = ImageUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                var input = new CreateProductInput
                {
                    Title = Product.Title,
                    Description = Product.Description,
                    Vendor = Product.Vendor,
                    ProductType = Product.ProductType,
                    Tags = string.IsNullOrWhiteSpace(Product.Tags)
                        ? new List<string>()
                        : Product.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList(),
                    Variants = validVariants.Select(v => new CreateVariantInput
                    {
                        Title = v.Title,
                        Price = v.Price,
                        Sku = v.Sku,
                        Option1 = v.Option1,
                        Option2 = v.Option2,
                        Option3 = v.Option3,
                        InventoryQuantity = v.InventoryQuantity
                    }).ToList(),
                    ImageUrls = validImageUrls
                };

                var product = await _productService.CreateProductAsync(input);
                _logger.LogInformation("Product created successfully: {ProductId}", product.NumericId);

                // Upload image files if any
                var validImageFiles = ImageFiles?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();
                foreach (var file in validImageFiles)
                {
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var base64 = Convert.ToBase64String(memoryStream.ToArray());

                        var uploadInput = new Algora.Application.DTOs.UploadProductImageInput
                        {
                            ProductId = product.NumericId,
                            Base64Data = base64,
                            FileName = file.FileName,
                            ContentType = file.ContentType,
                            Alt = Path.GetFileNameWithoutExtension(file.FileName)
                        };

                        await _productService.UploadProductImageAsync(uploadInput);
                        _logger.LogInformation("Uploaded image {FileName} to product {ProductId}", file.FileName, product.NumericId);
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "Failed to upload image {FileName}", file.FileName);
                        // Continue with other images even if one fails
                    }
                }

                TempData["SuccessMessage"] = $"Product '{product.Title}' created successfully!";
                return RedirectToPage("/Products/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ErrorMessage = $"Error creating product: {ex.Message}";
                return Page();
            }
        }
    }

    public class ProductInput
    {
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

    public class VariantInput
    {
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

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
        public int InventoryQuantity { get; set; }
    }
}
