using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

public class AnthropicTextProvider : ITextGenerationProvider
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicTextProvider> _logger;

    public string ProviderName => "anthropic";
    public string DisplayName => "Claude (Anthropic)";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public AnthropicTextProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<AnthropicTextProvider> logger)
    {
        _http = httpFactory.CreateClient("Anthropic");
        _options = options.Value.Anthropic;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            _http.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
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

        return await CallAnthropicAsync(prompt, 100, ct);
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

        return await CallAnthropicAsync(prompt, _options.MaxTokens, ct);
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

        return await CallAnthropicAsync(prompt, 50, ct);
    }

    private async Task<TextGenerationResponse> CallAnthropicAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "Anthropic API key is not configured",
                ProviderUsed = ProviderName
            };
        }

        try
        {
            var requestBody = new
            {
                model = _options.Model,
                max_tokens = maxTokens,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("messages", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new TextGenerationResponse
                {
                    Success = false,
                    Error = $"Anthropic API error: {response.StatusCode}",
                    ProviderUsed = ProviderName
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var generatedText = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString()?.Trim();

            var inputTokens = doc.RootElement.GetProperty("usage").GetProperty("input_tokens").GetInt32();
            var outputTokens = doc.RootElement.GetProperty("usage").GetProperty("output_tokens").GetInt32();
            var tokensUsed = inputTokens + outputTokens;

            // Estimate cost: Claude 3.5 Sonnet is ~$3/1M input + $15/1M output tokens
            var estimatedCost = (inputTokens * 0.000003m) + (outputTokens * 0.000015m);

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
            _logger.LogError(ex, "Error calling Anthropic API");
            return new TextGenerationResponse
            {
                Success = false,
                Error = ex.Message,
                ProviderUsed = ProviderName
            };
        }
    }
}
