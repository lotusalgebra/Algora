using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

/// <summary>
/// Simple OpenAI text generation provider for general-purpose prompts.
/// Used for AI-powered response suggestions.
/// </summary>
public class OpenAiTextSimpleProvider : IAiTextProvider
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiTextSimpleProvider> _logger;
    private const string DefaultModel = "gpt-4o-mini";

    public OpenAiTextSimpleProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAiTextSimpleProvider> logger)
    {
        _http = httpFactory.CreateClient("OpenAI");
        _options = options.Value.OpenAi;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _http.BaseAddress = new Uri("https://api.openai.com/v1/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured");
            return "AI suggestions are not available. Please configure the OpenAI API key.";
        }

        try
        {
            var model = _options.TextModel ?? DefaultModel;
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful customer service assistant. Provide professional, friendly, and concise responses." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with OpenAI");
            return "Unable to generate suggestion at this time.";
        }
    }

    public (string ProviderName, string ModelName) GetProviderInfo()
    {
        return ("openai", _options.TextModel ?? DefaultModel);
    }
}
