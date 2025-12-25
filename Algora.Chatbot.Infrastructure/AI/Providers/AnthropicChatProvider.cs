using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Chatbot.Infrastructure.AI.Providers;

public class AnthropicChatProvider : IChatbotAiProvider
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicChatProvider> _logger;

    public string ProviderName => "anthropic";
    public string DisplayName => "Anthropic Claude";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
    public int Priority => 2;

    public AnthropicChatProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<AnthropicChatProvider> logger)
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

    public async Task<ChatCompletionResult> GenerateResponseAsync(ChatContext context, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ChatCompletionResult
            {
                Success = false,
                Error = "Anthropic API key not configured"
            };
        }

        try
        {
            var messages = new List<object>();
            foreach (var msg in context.History)
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }
            messages.Add(new { role = "user", content = context.CurrentMessage });

            var requestBody = new
            {
                model = _options.Model,
                max_tokens = context.MaxTokens > 0 ? context.MaxTokens : _options.MaxTokens,
                system = context.SystemPrompt + "\n\nRespond with valid JSON containing: response, intent, confidence, suggestedActions",
                messages
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("messages", content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new ChatCompletionResult
                {
                    Success = false,
                    Error = $"Anthropic API error: {response.StatusCode}"
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var text = root
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var usage = root.GetProperty("usage");
            var inputTokens = usage.GetProperty("input_tokens").GetInt32();
            var outputTokens = usage.GetProperty("output_tokens").GetInt32();
            var tokensUsed = inputTokens + outputTokens;

            var parsed = ParseAiResponse(text);

            return new ChatCompletionResult
            {
                Success = true,
                Response = parsed.Response,
                DetectedIntent = parsed.Intent,
                Confidence = parsed.Confidence,
                SuggestedActions = parsed.Actions,
                TokensUsed = tokensUsed,
                EstimatedCost = CalculateCost(inputTokens, outputTokens),
                ProviderUsed = ProviderName,
                ModelUsed = _options.Model
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Anthropic API request timed out");
            return new ChatCompletionResult { Success = false, Error = "Request timed out" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            return new ChatCompletionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<IntentClassificationResult> ClassifyIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var context = new ChatContext
        {
            SystemPrompt = "Classify the user's intent. Return JSON: {\"intent\": \"order_status|product_inquiry|return_request|shipping_info|general\", \"confidence\": 0.0-1.0}",
            CurrentMessage = message,
            MaxTokens = 100
        };

        var result = await GenerateResponseAsync(context, cancellationToken);

        if (!result.Success)
        {
            return new IntentClassificationResult { Intent = "general", Confidence = 0.5m };
        }

        return new IntentClassificationResult
        {
            Intent = result.DetectedIntent ?? "general",
            Confidence = result.Confidence ?? 0.5m
        };
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsConfigured);
    }

    private static (string Response, string? Intent, decimal? Confidence, List<SuggestedAction>? Actions) ParseAiResponse(string text)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = text.IndexOf('{');
            var jsonEnd = text.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                text = text.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var response = root.TryGetProperty("response", out var respProp) ? respProp.GetString() ?? text : text;
            var intent = root.TryGetProperty("intent", out var intentProp) ? intentProp.GetString() : null;
            var confidence = root.TryGetProperty("confidence", out var confProp) ? (decimal?)confProp.GetDecimal() : null;

            List<SuggestedAction>? actions = null;
            if (root.TryGetProperty("suggestedActions", out var actionsProp) && actionsProp.ValueKind == JsonValueKind.Array)
            {
                actions = new List<SuggestedAction>();
                foreach (var action in actionsProp.EnumerateArray())
                {
                    actions.Add(new SuggestedAction
                    {
                        Label = action.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                        Type = action.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                        Value = action.TryGetProperty("value", out var v) ? v.GetString() ?? "" : ""
                    });
                }
            }

            return (response, intent, confidence, actions);
        }
        catch
        {
            return (text, null, null, null);
        }
    }

    private static decimal CalculateCost(int inputTokens, int outputTokens)
    {
        // Claude 3.5 Sonnet: $3/1M input + $15/1M output
        return (decimal)inputTokens * 0.000003m + (decimal)outputTokens * 0.000015m;
    }
}
