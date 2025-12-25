using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

/// <summary>
/// Simple Anthropic text generation provider for general-purpose prompts.
/// Used as fallback when OpenAI is unavailable.
/// </summary>
public class AnthropicTextSimpleProvider : IAiTextProvider
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicTextSimpleProvider> _logger;
    private const string DefaultModel = "claude-3-5-sonnet-20241022";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public AnthropicTextSimpleProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<AnthropicTextSimpleProvider> logger)
    {
        _http = httpFactory.CreateClient("Anthropic");
        _options = options.Value.Anthropic;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            _http.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            _http.Timeout = TimeSpan.FromSeconds(60);
        }
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Anthropic API key not configured");
            return "AI suggestions are not available. Please configure the Anthropic API key.";
        }

        try
        {
            var model = _options.Model ?? DefaultModel;
            var requestBody = new
            {
                model,
                max_tokens = _options.MaxTokens > 0 ? _options.MaxTokens : 500,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                system = "You are a helpful customer service assistant. Provide professional, friendly, and concise responses."
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("messages", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return text?.Trim() ?? string.Empty;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Anthropic API request timed out");
            return "The AI service is taking too long to respond. Please try again.";
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Anthropic API request was cancelled");
            return "The request was cancelled. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Anthropic API");
            return "Unable to connect to AI service. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Anthropic");
            return "Unable to generate suggestion at this time.";
        }
    }

    public (string ProviderName, string ModelName) GetProviderInfo()
    {
        return ("anthropic", _options.Model ?? DefaultModel);
    }
}
