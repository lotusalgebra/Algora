using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Image;

public class DallEImageProvider : IImageGenerationProvider
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<DallEImageProvider> _logger;

    public string ProviderName => "dalle";
    public string DisplayName => "DALL-E 3 (OpenAI)";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public DallEImageProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<DallEImageProvider> logger)
    {
        _http = httpFactory.CreateClient("OpenAI");
        _options = options.Value.OpenAi;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://api.openai.com/v1/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new ImageGenerationResponse
            {
                Success = false,
                Error = "OpenAI API key is not configured",
                ProviderUsed = ProviderName
            };
        }

        try
        {
            // Build a detailed prompt for product image generation
            var prompt = BuildProductImagePrompt(request);

            var requestBody = new
            {
                model = _options.ImageModel,
                prompt = prompt,
                n = 1,
                size = request.Size,
                quality = request.Quality,
                response_format = "url"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("images/generations", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DALL-E API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Error = $"DALL-E API error: {response.StatusCode}",
                    ProviderUsed = ProviderName
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var imageUrl = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("url")
                .GetString();

            // DALL-E 3 pricing: Standard 1024x1024 = $0.04, HD = $0.08
            var estimatedCost = request.Quality == "hd" ? 0.08m : 0.04m;
            if (request.Size == "1792x1024" || request.Size == "1024x1792")
            {
                estimatedCost = request.Quality == "hd" ? 0.12m : 0.08m;
            }

            return new ImageGenerationResponse
            {
                Success = true,
                ImageUrl = imageUrl,
                EstimatedCost = estimatedCost,
                ProviderUsed = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DALL-E API");
            return new ImageGenerationResponse
            {
                Success = false,
                Error = ex.Message,
                ProviderUsed = ProviderName
            };
        }
    }

    private static string BuildProductImagePrompt(ImageGenerationRequest request)
    {
        var basePrompt = !string.IsNullOrWhiteSpace(request.Prompt)
            ? request.Prompt
            : $"A professional product photograph of {request.ProductTitle ?? "a product"}";

        var style = request.Style ?? "product photography, white background, professional lighting";

        return $"{basePrompt}. Style: {style}. High quality, detailed, commercial product shot suitable for e-commerce.";
    }
}
