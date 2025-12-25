using Algora.Application.Interfaces.AI;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.AI.Providers.Text;

/// <summary>
/// AI text provider with fallback support.
/// Tries providers in order: OpenAI -> Anthropic -> Gemini
/// </summary>
public class FallbackAiTextProvider : IAiTextProvider
{
    private readonly OpenAiTextSimpleProvider _openAi;
    private readonly AnthropicTextSimpleProvider _anthropic;
    private readonly ILogger<FallbackAiTextProvider> _logger;
    private string _lastUsedProvider = "none";

    public FallbackAiTextProvider(
        OpenAiTextSimpleProvider openAi,
        AnthropicTextSimpleProvider anthropic,
        ILogger<FallbackAiTextProvider> logger)
    {
        _openAi = openAi;
        _anthropic = anthropic;
        _logger = logger;
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Try OpenAI first
        var openAiResult = await TryProviderAsync("OpenAI", _openAi, prompt, cancellationToken);
        if (openAiResult.success)
        {
            _lastUsedProvider = "openai";
            return openAiResult.result;
        }

        // Fallback to Anthropic
        _logger.LogInformation("OpenAI failed, falling back to Anthropic");

        if (!_anthropic.IsConfigured)
        {
            _logger.LogWarning("Anthropic is not configured, cannot fallback");
            return openAiResult.result; // Return OpenAI error message
        }

        var anthropicResult = await TryProviderAsync("Anthropic", _anthropic, prompt, cancellationToken);
        if (anthropicResult.success)
        {
            _lastUsedProvider = "anthropic";
            return anthropicResult.result;
        }

        // All providers failed
        _logger.LogError("All AI providers failed");
        _lastUsedProvider = "none";
        return "I'm here to help! Could you please rephrase your question?";
    }

    private async Task<(bool success, string result)> TryProviderAsync(
        string providerName,
        IAiTextProvider provider,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Trying AI provider: {Provider}", providerName);
            var result = await provider.GenerateTextAsync(prompt, cancellationToken);

            // Check if result indicates an error
            if (IsErrorResponse(result))
            {
                _logger.LogWarning("{Provider} returned error response: {Response}", providerName, result);
                return (false, result);
            }

            _logger.LogInformation("{Provider} succeeded", providerName);
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} threw exception", providerName);
            return (false, $"Error from {providerName}: {ex.Message}");
        }
    }

    private static bool IsErrorResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return true;

        // Check for common error response patterns
        var errorPhrases = new[]
        {
            "API key not configured",
            "Unable to connect",
            "Unable to generate",
            "taking too long",
            "request was cancelled",
            "try again"
        };

        return errorPhrases.Any(phrase =>
            response.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    public (string ProviderName, string ModelName) GetProviderInfo()
    {
        return _lastUsedProvider switch
        {
            "openai" => _openAi.GetProviderInfo(),
            "anthropic" => _anthropic.GetProviderInfo(),
            _ => ("fallback", "auto")
        };
    }
}
