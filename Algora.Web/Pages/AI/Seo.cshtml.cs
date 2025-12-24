using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.AI;

public class SeoModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ISeoOptimizerService _seoService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SeoModel> _logger;

    public SeoModel(
        AppDbContext context,
        ISeoOptimizerService seoService,
        IShopContext shopContext,
        ILogger<SeoModel> logger)
    {
        _context = context;
        _seoService = seoService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ProductSeoDto> Products { get; set; } = new();

    public class ProductSeoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? ProductType { get; set; }
        public string? Vendor { get; set; }
        public string? Tags { get; set; }
        public int SeoScore { get; set; }
    }

    public async Task OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;

        Products = await _context.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .OrderBy(p => p.Title)
            .Select(p => new ProductSeoDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                ProductType = p.ProductType,
                Vendor = p.Vendor,
                Tags = p.Tags,
                SeoScore = 0 // Will be populated from ProductSeoData if available
            })
            .ToListAsync();

        // Load SEO scores
        var productIds = Products.Select(p => p.Id).ToList();
        var seoData = await _context.Set<Algora.Domain.Entities.ProductSeoData>()
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, s => s.SeoScore ?? 0);

        foreach (var product in Products)
        {
            if (seoData.TryGetValue(product.Id, out var score))
            {
                product.SeoScore = score;
            }
        }
    }

    public async Task<IActionResult> OnGetGetSeoDataAsync(int productId)
    {
        try
        {
            var data = await _seoService.GetSeoDataAsync(productId);
            if (data != null)
            {
                return new JsonResult(new { success = true, data });
            }
            return new JsonResult(new { success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SEO data for product {ProductId}", productId);
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostOptimizeAsync([FromBody] OptimizeRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return new JsonResult(new { success = false, error = "Product not found" });
            }

            var seoRequest = new SeoOptimizationRequest
            {
                ProductId = product.Id,
                Title = product.Title,
                Description = product.Description,
                Category = product.ProductType,
                Vendor = product.Vendor,
                Tags = product.Tags
            };

            var result = await _seoService.OptimizeAsync(seoRequest);

            if (result.Success)
            {
                return new JsonResult(new { success = true, data = result });
            }
            return new JsonResult(new { success = false, error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing SEO for product {ProductId}", request.ProductId);
            return new JsonResult(new { success = false, error = "An error occurred" });
        }
    }

    public async Task<IActionResult> OnPostApplyAsync([FromBody] ApplyRequest request)
    {
        try
        {
            var data = await _seoService.GetSeoDataAsync(request.ProductId);
            if (data != null)
            {
                await _seoService.ApplySeoDataAsync(request.ProductId, data);
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, error = "No SEO data to apply" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying SEO data for product {ProductId}", request.ProductId);
            return new JsonResult(new { success = false, error = "An error occurred" });
        }
    }

    public class OptimizeRequest
    {
        public int ProductId { get; set; }
    }

    public class ApplyRequest
    {
        public int ProductId { get; set; }
    }
}
