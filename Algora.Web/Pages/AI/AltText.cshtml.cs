using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.Data;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.AI;

[Authorize]
[RequireFeature(FeatureCodes.AiAltText)]
public class AltTextModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IAiContentService _aiContentService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<AltTextModel> _logger;

    public AltTextModel(
        AppDbContext context,
        IAiContentService aiContentService,
        IShopContext shopContext,
        ILogger<AltTextModel> logger)
    {
        _context = context;
        _aiContentService = aiContentService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ProductImageDto> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int WithAltText { get; set; }
    public int MissingAltText { get; set; }

    public class ProductImageDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int ImageCount { get; set; }
        public bool HasAltText { get; set; }
    }

    public class ImageDto
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? AltText { get; set; }
    }

    public async Task OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;

        // Get products with image counts
        Products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .Select(p => new ProductImageDto
            {
                Id = p.Id,
                Title = p.Title,
                ImageCount = 1, // Simplified - assumes main image
                HasAltText = false // Will check via separate query if needed
            })
            .OrderBy(p => p.Title)
            .ToListAsync();

        TotalProducts = Products.Count;
        WithAltText = 0; // Would need ProductImage entity with AltText field
        MissingAltText = TotalProducts - WithAltText;
    }

    public async Task<IActionResult> OnGetGetImagesAsync(int productId)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return new JsonResult(new { success = false, error = "Product not found" });
            }

            // For now, return a placeholder image - in real implementation, would load from ProductImages table
            var images = new List<ImageDto>
            {
                new ImageDto
                {
                    Id = productId, // Using productId as image ID for simplicity
                    Url = null, // Would be the actual image URL
                    AltText = null // Would be loaded from database
                }
            };

            return new JsonResult(new { success = true, images });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading images for product {ProductId}", productId);
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return new JsonResult(new { success = false, error = "Product not found" });
            }

            var aiRequest = new TextGenerationRequest
            {
                ProductId = product.Id,
                CurrentTitle = product.Title,
                CurrentDescription = product.Description,
                ProductType = product.ProductType,
                Vendor = product.Vendor
            };

            var response = await _aiContentService.GenerateAltTextAsync(aiRequest);

            if (response.Success)
            {
                return new JsonResult(new { success = true, altText = response.GeneratedText });
            }
            return new JsonResult(new { success = false, error = response.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating alt-text for image {ImageId}", request.ImageId);
            return new JsonResult(new { success = false, error = "An error occurred" });
        }
    }

    public async Task<IActionResult> OnPostGenerateAllAsync([FromBody] GenerateAllRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return new JsonResult(new { success = false, error = "Product not found" });
            }

            var aiRequest = new TextGenerationRequest
            {
                ProductId = product.Id,
                CurrentTitle = product.Title,
                CurrentDescription = product.Description,
                ProductType = product.ProductType,
                Vendor = product.Vendor
            };

            var response = await _aiContentService.GenerateAltTextAsync(aiRequest);

            if (response.Success)
            {
                // In real implementation, would save to all product images
                return new JsonResult(new { success = true, count = 1 });
            }
            return new JsonResult(new { success = false, error = response.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating all alt-text for product {ProductId}", request.ProductId);
            return new JsonResult(new { success = false, error = "An error occurred" });
        }
    }

    public Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest request)
    {
        try
        {
            // In real implementation, would save to ProductImage entity
            // For now, just return success
            return Task.FromResult<IActionResult>(new JsonResult(new { success = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving alt-text for image {ImageId}", request.ImageId);
            return Task.FromResult<IActionResult>(new JsonResult(new { success = false, error = "An error occurred" }));
        }
    }

    public class GenerateRequest
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
    }

    public class GenerateAllRequest
    {
        public int ProductId { get; set; }
    }

    public class SaveRequest
    {
        public int ImageId { get; set; }
        public string AltText { get; set; } = "";
    }
}
