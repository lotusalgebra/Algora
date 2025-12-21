using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

public class OpenAiTextProvider : ITextGenerationProvider
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiTextProvider> _logger;

    public string ProviderName => "openai";
    public string DisplayName => "OpenAI GPT-4";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public OpenAiTextProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAiTextProvider> logger)
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

        return await CallOpenAiAsync(prompt, 100, ct);
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

        return await CallOpenAiAsync(prompt, _options.MaxTokens, ct);
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

        return await CallOpenAiAsync(prompt, 50, ct);
    }

    private async Task<TextGenerationResponse> CallOpenAiAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return new TextGenerationResponse
            {
                Success = false,
                Error = "OpenAI API key is not configured",
                ProviderUsed = ProviderName
            };
        }

        try
        {
            var requestBody = new
            {
                model = _options.TextModel,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = maxTokens,
                temperature = _options.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("chat/completions", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new TextGenerationResponse
                {
                    Success = false,
                    Error = $"OpenAI API error: {response.StatusCode}",
                    ProviderUsed = ProviderName
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var generatedText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            var tokensUsed = doc.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32();

            // Estimate cost: GPT-4o is ~$2.50/1M input + $10/1M output tokens
            var estimatedCost = tokensUsed * 0.000005m;

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
            _logger.LogError(ex, "Error calling OpenAI API");
            return new TextGenerationResponse
            {
                Success = false,
                Error = ex.Message,
                ProviderUsed = ProviderName
            };
        }
    }
}
