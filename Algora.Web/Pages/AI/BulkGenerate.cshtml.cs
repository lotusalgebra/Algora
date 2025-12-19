using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.AI;

[Authorize]
public class BulkGenerateModel : PageModel
{
    private readonly IShopifyProductService _productService;
    private readonly IAiContentService _aiService;
    private readonly ILogger<BulkGenerateModel> _logger;

    public BulkGenerateModel(
        IShopifyProductService productService,
        IAiContentService aiService,
        ILogger<BulkGenerateModel> logger)
    {
        _productService = productService;
        _aiService = aiService;
        _logger = logger;
    }

    public List<ProductDto> Products { get; set; } = new();
    public List<TextProviderViewModel> TextProviders { get; set; } = new();
    public List<ImageProviderViewModel> ImageProviders { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            // Load products
            var products = await _productService.GetProductsAsync(null);
            Products = products.ToList();

            // Load available providers
            var textProviders = _aiService.GetAvailableTextProviders();
            TextProviders = textProviders.Select(p => new TextProviderViewModel
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                IsConfigured = p.IsConfigured
            }).ToList();

            var imageProviders = _aiService.GetAvailableImageProviders();
            ImageProviders = imageProviders.Select(p => new ImageProviderViewModel
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                IsConfigured = p.IsConfigured
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bulk generation page");
        }
    }

    public class TextProviderViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
    }

    public class ImageProviderViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
    }
}
