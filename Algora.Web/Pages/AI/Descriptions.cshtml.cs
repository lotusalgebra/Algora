using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.AI;

public class DescriptionsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IAiContentService _aiContentService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DescriptionsModel> _logger;

    public DescriptionsModel(
        AppDbContext context,
        IAiContentService aiContentService,
        IShopContext shopContext,
        ILogger<DescriptionsModel> logger)
    {
        _context = context;
        _aiContentService = aiContentService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ProductDto> Products { get; set; } = new();

    public class ProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public string? Tags { get; set; }
    }

    public async Task OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;

        Products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .OrderBy(p => p.Title)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Vendor = p.Vendor,
                ProductType = p.ProductType,
                Tags = p.Tags
            })
            .ToListAsync();
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
                Vendor = product.Vendor,
                ProductType = product.ProductType,
                Tags = product.Tags,
                Tone = request.Tone ?? "professional, persuasive",
                MaxWords = request.MaxWords > 0 ? request.MaxWords : 150,
                Features = request.AdditionalContext
            };

            var response = await _aiContentService.GenerateDescriptionAsync(aiRequest);

            if (response.Success)
            {
                return new JsonResult(new
                {
                    success = true,
                    description = response.GeneratedText,
                    tokensUsed = response.TokensUsed,
                    estimatedCost = response.EstimatedCost
                });
            }
            else
            {
                return new JsonResult(new { success = false, error = response.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating description for product {ProductId}", request.ProductId);
            return new JsonResult(new { success = false, error = "An error occurred while generating the description" });
        }
    }

    public async Task<IActionResult> OnPostApplyAsync([FromBody] ApplyRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return new JsonResult(new { success = false, error = "Product not found" });
            }

            product.Description = request.Description;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying description to product {ProductId}", request.ProductId);
            return new JsonResult(new { success = false, error = "An error occurred while applying the description" });
        }
    }

    public class GenerateRequest
    {
        public int ProductId { get; set; }
        public string? Tone { get; set; }
        public int MaxWords { get; set; }
        public string? AdditionalContext { get; set; }
    }

    public class ApplyRequest
    {
        public int ProductId { get; set; }
        public string Description { get; set; } = "";
    }
}
