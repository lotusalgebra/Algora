using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

public class GeminiTextProvider : ITextGenerationProvider
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiTextProvider> _logger;

    public string ProviderName => "gemini";
    public string DisplayName => "Google Gemini";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public GeminiTextProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<GeminiTextProvider> logger)
    {
        _http = httpFactory.CreateClient("Gemini");
        _options = options.Value.Gemini;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        }
    }

    public async Task<TextGenerationResponse> GenerateTitleAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        var prompt = $"""
            Generate a concise, SEO-optimized product title for an e-commerce product.

            Product details:
            - Current title: {request.CurrentTitle ?? "N/A"}
            - Category: {request.Category ?? "N/A"}
            - Product type: {request.ProductType ?? "N/A"}
            - Vendor/Brand: {request.Vendor ?? "N/A"}
            - Tags: {request.Tags ?? "N/A"}
            - Material: {request.Material ?? "N/A"}
            - Color: {request.Color ?? "N/A"}

            Requirements:
            - Maximum 70 characters
            - Include key product attributes (brand, type, material if relevant)
            - Be descriptive but concise
            - Optimize for search engines
            - Use title case

            Return only the title text, no quotes or explanation.
            """;

        return await CallGeminiAsync(prompt, 100, ct);
    }

    public async Task<TextGenerationResponse> GenerateDescriptionAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        var prompt = $"""
            Generate an SEO-optimized product description for an e-commerce product.

            Product details:
            - Title: {request.CurrentTitle ?? "N/A"}
            - Category: {request.Category ?? "N/A"}
            - Product type: {request.ProductType ?? "N/A"}
            - Vendor/Brand: {request.Vendor ?? "N/A"}
            - Tags: {request.Tags ?? "N/A"}
            - Material: {request.Material ?? "N/A"}
            - Color: {request.Color ?? "N/A"}
            - Features: {request.Features ?? "N/A"}

            Requirements:
            - Maximum {request.MaxWords} words
            - Tone: {request.Tone}
            - Include relevant keywords naturally
            - Highlight key benefits and features
            - Use short paragraphs for readability
            - Include a call-to-action at the end
            - Do NOT use markdown formatting

            Return only the description text, no quotes or explanation.
            """;

        return await CallGeminiAsync(prompt, _options.MaxOutputTokens, ct);
    }

    public async Task<TextGenerationResponse> GenerateAltTextAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        var prompt = $"""
            Generate SEO-friendly alt text for a product image.

            Product details:
            - Title: {request.CurrentTitle ?? "N/A"}
            - Category: {request.Category ?? "N/A"}
            - Product type: {request.ProductType ?? "N/A"}
            - Color: {request.Color ?? "N/A"}
            - Material: {request.Material ?? "N/A"}

            Requirements:
            - Maximum 125 characters
            - Describe the product clearly for accessibility
            - Include relevant keywords
            - Be specific but concise
            - Do not start with "Image of" or "Picture of"

            Return only the alt text, no quotes or explanation.
            """;

        return await CallGeminiAsync(prompt, 50, ct);
    }

    private async Task<TextGenerationResponse> CallGeminiAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "Gemini API key is not configured",
                ProviderUsed = ProviderName
            };
        }

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxTokens,
                    temperature = _options.Temperature
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"models/{_options.Model}:generateContent?key={_options.ApiKey}";
            var response = await _http.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new TextGenerationResponse
                {
                    Success = false,
                    Error = $"Gemini API error: {response.StatusCode}",
                    ProviderUsed = ProviderName
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var generatedText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim();

            // Gemini doesn't return token count in the same way, estimate based on text length
            var tokensUsed = (prompt.Length + (generatedText?.Length ?? 0)) / 4;

            // Estimate cost: Gemini 1.5 Pro is ~$1.25/1M input + $5/1M output tokens
            var estimatedCost = tokensUsed * 0.000003m;

            return new TextGenerationResponse
            {
                Success = true,
                GeneratedText = generatedText,
                TokensUsed = tokensUsed,
                EstimatedCost = estimatedCost,
                ProviderUsed = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return new TextGenerationResponse
            {
                Success = false,
                Error = ex.Message,
                ProviderUsed = ProviderName
            };
        }
    }
}
