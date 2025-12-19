using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Image;

public class StabilityAiImageProvider : IImageGenerationProvider
{
    private readonly HttpClient _http;
    private readonly StabilityAiOptions _options;
    private readonly ILogger<StabilityAiImageProvider> _logger;

    public string ProviderName => "stability";
    public string DisplayName => "Stability AI";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public StabilityAiImageProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<StabilityAiImageProvider> logger)
    {
        _http = httpFactory.CreateClient("StabilityAI");
        _options = options.Value.StabilityAi;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://api.stability.ai/v1/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return new ImageGenerationResponse
            {
                Success = false,
                Error = "Stability AI API key is not configured",
                ProviderUsed = ProviderName
            };
        }

        try
        {
            var prompt = BuildProductImagePrompt(request);
            var (width, height) = ParseSize(request.Size);

            var requestBody = new
            {
                text_prompts = new[]
                {
                    new { text = prompt, weight = 1.0 },
                    new { text = "blurry, low quality, distorted, watermark, text", weight = -1.0 }
                },
                cfg_scale = _options.CfgScale,
                height = height,
                width = width,
                steps = _options.Steps,
                samples = 1
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"generation/{_options.Engine}/text-to-image";
            var response = await _http.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stability AI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Error = $"Stability AI API error: {response.StatusCode}",
                    ProviderUsed = ProviderName
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var base64Image = doc.RootElement
                .GetProperty("artifacts")[0]
                .GetProperty("base64")
                .GetString();

            // Stability AI pricing: ~$0.002-0.006 per image depending on resolution
            var estimatedCost = 0.004m;

            return new ImageGenerationResponse
            {
                Success = true,
                ImageBase64 = base64Image,
                EstimatedCost = estimatedCost,
                ProviderUsed = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Stability AI API");
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

        return $"{basePrompt}. {style}. High quality, detailed, commercial product shot, studio lighting, sharp focus, 8k";
    }

    private static (int width, int height) ParseSize(string size)
    {
        return size switch
        {
            "512x512" => (512, 512),
            "768x768" => (768, 768),
            "1024x1024" => (1024, 1024),
            "1792x1024" => (1792, 1024),
            "1024x1792" => (1024, 1792),
            _ => (1024, 1024)
        };
    }
}
