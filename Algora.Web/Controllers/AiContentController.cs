using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiContentController : ControllerBase
{
    private readonly IAiContentService _aiService;
    private readonly IShopifyProductService _productService;
    private readonly ILogger<AiContentController> _logger;

    public AiContentController(
        IAiContentService aiService,
        IShopifyProductService productService,
        ILogger<AiContentController> logger)
    {
        _aiService = aiService;
        _productService = productService;
        _logger = logger;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        return Ok(new
        {
            TextProviders = _aiService.GetAvailableTextProviders(),
            ImageProviders = _aiService.GetAvailableImageProviders()
        });
    }

    [HttpPost("generate/title")]
    public async Task<IActionResult> GenerateTitle([FromBody] GenerateTitleApiRequest request)
    {
        var product = await _productService.GetProductByIdAsync(request.ProductId);

        var aiRequest = new TextGenerationRequest
        {
            ProductId = request.ProductId,
            CurrentTitle = product?.Title ?? request.CurrentTitle,
            Category = request.Category,
            ProductType = request.ProductType,
            Tags = request.Tags ?? (product != null ? string.Join(", ", product.Tags) : null),
            Vendor = request.Vendor,
            Material = request.Material,
            Color = request.Color,
            Features = request.Features
        };

        var result = await _aiService.GenerateTitleAsync(aiRequest, request.Provider);
        return Ok(result);
    }

    [HttpPost("generate/description")]
    public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionApiRequest request)
    {
        var product = await _productService.GetProductByIdAsync(request.ProductId);

        var aiRequest = new TextGenerationRequest
        {
            ProductId = request.ProductId,
            CurrentTitle = product?.Title ?? request.CurrentTitle,
            Category = request.Category,
            ProductType = request.ProductType,
            Tags = request.Tags ?? (product != null ? string.Join(", ", product.Tags) : null),
            Vendor = request.Vendor,
            Material = request.Material,
            Color = request.Color,
            Features = request.Features,
            Tone = request.Tone ?? "professional, persuasive",
            MaxWords = request.MaxWords > 0 ? request.MaxWords : 150
        };

        var result = await _aiService.GenerateDescriptionAsync(aiRequest, request.Provider);
        return Ok(result);
    }

    [HttpPost("generate/alt-text")]
    public async Task<IActionResult> GenerateAltText([FromBody] GenerateAltTextApiRequest request)
    {
        var product = await _productService.GetProductByIdAsync(request.ProductId);

        var aiRequest = new TextGenerationRequest
        {
            ProductId = request.ProductId,
            CurrentTitle = product?.Title ?? request.CurrentTitle,
            Category = request.Category,
            ProductType = request.ProductType,
            Color = request.Color,
            Material = request.Material,
            ImageUrl = request.ImageUrl
        };

        var result = await _aiService.GenerateAltTextAsync(aiRequest, request.Provider);
        return Ok(result);
    }

    [HttpPost("generate/image")]
    public async Task<IActionResult> GenerateImage([FromBody] GenerateImageApiRequest request)
    {
        var product = await _productService.GetProductByIdAsync(request.ProductId);

        var aiRequest = new ImageGenerationRequest
        {
            ProductId = request.ProductId,
            Prompt = request.Prompt ?? "",
            ProductTitle = product?.Title ?? request.ProductTitle,
            ProductDescription = request.ProductDescription,
            Style = request.Style,
            Size = request.Size ?? "1024x1024",
            Quality = request.Quality ?? "standard"
        };

        var result = await _aiService.GenerateImageAsync(aiRequest, request.Provider);
        return Ok(result);
    }

    [HttpPost("generate/bulk")]
    public async Task<IActionResult> GenerateBulk([FromBody] BulkGenerationRequest request)
    {
        var result = await _aiService.GenerateBulkContentAsync(request);
        return Ok(result);
    }
}

// API Request DTOs
public record GenerateTitleApiRequest
{
    public long ProductId { get; init; }
    public string? Provider { get; init; }
    public string? CurrentTitle { get; init; }
    public string? Category { get; init; }
    public string? ProductType { get; init; }
    public string? Vendor { get; init; }
    public string? Tags { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Features { get; init; }
}

public record GenerateDescriptionApiRequest
{
    public long ProductId { get; init; }
    public string? Provider { get; init; }
    public string? CurrentTitle { get; init; }
    public string? Category { get; init; }
    public string? ProductType { get; init; }
    public string? Vendor { get; init; }
    public string? Tags { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Features { get; init; }
    public string? Tone { get; init; }
    public int MaxWords { get; init; } = 150;
}

public record GenerateAltTextApiRequest
{
    public long ProductId { get; init; }
    public string? Provider { get; init; }
    public string? CurrentTitle { get; init; }
    public string? Category { get; init; }
    public string? ProductType { get; init; }
    public string? Color { get; init; }
    public string? Material { get; init; }
    public string? ImageUrl { get; init; }
}

public record GenerateImageApiRequest
{
    public long ProductId { get; init; }
    public string? Provider { get; init; }
    public string? Prompt { get; init; }
    public string? ProductTitle { get; init; }
    public string? ProductDescription { get; init; }
    public string? Style { get; init; }
    public string? Size { get; init; }
    public string? Quality { get; init; }
}
