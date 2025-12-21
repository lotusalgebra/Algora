using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Services;

public class AiContentService : IAiContentService
{
    private readonly IEnumerable<ITextGenerationProvider> _textProviders;
    private readonly IEnumerable<IImageGenerationProvider> _imageProviders;
    private readonly AiOptions _options;
    private readonly ILogger<AiContentService> _logger;

    public AiContentService(
        IEnumerable<ITextGenerationProvider> textProviders,
        IEnumerable<IImageGenerationProvider> imageProviders,
        IOptions<AiOptions> options,
        ILogger<AiContentService> logger)
    {
        _textProviders = textProviders;
        _imageProviders = imageProviders;
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<AiProviderInfo> GetAvailableTextProviders()
    {
        return _textProviders.Select(p => new AiProviderInfo(
            p.ProviderName,
            p.DisplayName,
            p.IsConfigured,
            new[] { "title", "description", "alt-text" }
        )).ToList();
    }

    public IReadOnlyList<AiProviderInfo> GetAvailableImageProviders()
    {
        return _imageProviders.Select(p => new AiProviderInfo(
            p.ProviderName,
            p.DisplayName,
            p.IsConfigured,
            new[] { "image" }
        )).ToList();
    }

    public async Task<TextGenerationResponse> GenerateTitleAsync(
        TextGenerationRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        var provider = GetTextProvider(preferredProvider);
        if (provider == null)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "No text generation provider is configured"
            };
        }

        _logger.LogInformation("Generating title using {Provider} for product {ProductId}",
            provider.ProviderName, request.ProductId);

        return await provider.GenerateTitleAsync(request, ct);
    }

    public async Task<TextGenerationResponse> GenerateDescriptionAsync(
        TextGenerationRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        var provider = GetTextProvider(preferredProvider);
        if (provider == null)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "No text generation provider is configured"
            };
        }

        _logger.LogInformation("Generating description using {Provider} for product {ProductId}",
            provider.ProviderName, request.ProductId);

        return await provider.GenerateDescriptionAsync(request, ct);
    }

    public async Task<TextGenerationResponse> GenerateAltTextAsync(
        TextGenerationRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        var provider = GetTextProvider(preferredProvider);
        if (provider == null)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "No text generation provider is configured"
            };
        }

        _logger.LogInformation("Generating alt text using {Provider} for product {ProductId}",
            provider.ProviderName, request.ProductId);

        return await provider.GenerateAltTextAsync(request, ct);
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(
        ImageGenerationRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        var provider = GetImageProvider(preferredProvider);
        if (provider == null)
        {
            return new ImageGenerationResponse
            {
                Success = false,
                Error = "No image generation provider is configured"
            };
        }

        _logger.LogInformation("Generating image using {Provider} for product {ProductId}",
            provider.ProviderName, request.ProductId);

        return await provider.GenerateImageAsync(request, ct);
    }

    public async Task<BulkGenerationResult> GenerateBulkContentAsync(
        BulkGenerationRequest request,
        CancellationToken ct = default)
    {
        var results = new List<BulkGenerationItemResult>();
        var totalCost = 0m;
        var successCount = 0;
        var failureCount = 0;

        var textProvider = GetTextProvider(request.TextProvider);
        var imageProvider = GetImageProvider(request.ImageProvider);

        foreach (var productId in request.ProductIds)
        {
            if (ct.IsCancellationRequested) break;

            var itemResult = new BulkGenerationItemResult
            {
                ProductId = productId,
                Success = true
            };

            try
            {
                var textRequest = new TextGenerationRequest
                {
                    ProductId = productId,
                    Tone = request.Tone,
                    MaxWords = request.MaxDescriptionWords
                };

                // Generate title
                if (request.GenerateTitles && textProvider != null)
                {
                    var titleResult = await textProvider.GenerateTitleAsync(textRequest, ct);
                    if (titleResult.Success)
                    {
                        itemResult = itemResult with { GeneratedTitle = titleResult.GeneratedText };
                        totalCost += titleResult.EstimatedCost;
                    }
                    else
                    {
                        itemResult = itemResult with { Success = false, Error = titleResult.Error };
                    }
                }

                // Generate description
                if (request.GenerateDescriptions && textProvider != null && itemResult.Success)
                {
                    var descResult = await textProvider.GenerateDescriptionAsync(textRequest, ct);
                    if (descResult.Success)
                    {
                        itemResult = itemResult with { GeneratedDescription = descResult.GeneratedText };
                        totalCost += descResult.EstimatedCost;
                    }
                    else
                    {
                        itemResult = itemResult with { Success = false, Error = descResult.Error };
                    }
                }

                // Generate alt text
                if (request.GenerateAltText && textProvider != null && itemResult.Success)
                {
                    var altResult = await textProvider.GenerateAltTextAsync(textRequest, ct);
                    if (altResult.Success)
                    {
                        itemResult = itemResult with { GeneratedAltText = altResult.GeneratedText };
                        totalCost += altResult.EstimatedCost;
                    }
                    else
                    {
                        itemResult = itemResult with { Success = false, Error = altResult.Error };
                    }
                }

                // Generate image
                if (request.GenerateImages && imageProvider != null && itemResult.Success)
                {
                    var imageRequest = new ImageGenerationRequest
                    {
                        ProductId = productId,
                        ProductTitle = itemResult.GeneratedTitle
                    };

                    var imageResult = await imageProvider.GenerateImageAsync(imageRequest, ct);
                    if (imageResult.Success)
                    {
                        itemResult = itemResult with { GeneratedImageUrl = imageResult.ImageUrl ?? imageResult.ImageBase64 };
                        totalCost += imageResult.EstimatedCost;
                    }
                    else
                    {
                        itemResult = itemResult with { Success = false, Error = imageResult.Error };
                    }
                }

                if (itemResult.Success)
                    successCount++;
                else
                    failureCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content for product {ProductId}", productId);
                itemResult = itemResult with { Success = false, Error = ex.Message };
                failureCount++;
            }

            results.Add(itemResult);

            // Small delay to respect rate limits
            await Task.Delay(100, ct);
        }

        return new BulkGenerationResult
        {
            TotalProducts = request.ProductIds.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            TotalEstimatedCost = totalCost
        };
    }

    private ITextGenerationProvider? GetTextProvider(string? preferredProvider)
    {
        var providerName = preferredProvider ?? _options.DefaultTextProvider;

        var provider = _textProviders.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && p.IsConfigured);

        // Fallback to any configured provider
        return provider ?? _textProviders.FirstOrDefault(p => p.IsConfigured);
    }

    private IImageGenerationProvider? GetImageProvider(string? preferredProvider)
    {
        var providerName = preferredProvider ?? _options.DefaultImageProvider;

        var provider = _imageProviders.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && p.IsConfigured);

        // Fallback to any configured provider
        return provider ?? _imageProviders.FirstOrDefault(p => p.IsConfigured);
    }
}
