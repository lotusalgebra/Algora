using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Chatbot.Infrastructure.AI.Providers;

public class OpenAiChatProvider : IChatbotAiProvider
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatProvider> _logger;

    public string ProviderName => "openai";
    public string DisplayName => "OpenAI GPT-4";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
    public int Priority => 1;

    public OpenAiChatProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAiChatProvider> logger)
    {
        _http = httpFactory.CreateClient("OpenAI");
        _options = options.Value.OpenAi;
        _logger = logger;

        if (IsConfigured)
        {
            _http.BaseAddress = new Uri("https://api.openai.com/v1/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
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
                Error = "OpenAI API key not configured"
            };
        }

        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = context.SystemPrompt }
            };

            foreach (var msg in context.History)
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }

            messages.Add(new { role = "user", content = context.CurrentMessage });

            var requestBody = new
            {
                model = _options.Model,
                messages,
                max_tokens = context.MaxTokens > 0 ? context.MaxTokens : _options.MaxTokens,
                temperature = context.Temperature > 0 ? context.Temperature : _options.Temperature,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("chat/completions", content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new ChatCompletionResult
                {
                    Success = false,
                    Error = $"OpenAI API error: {response.StatusCode}"
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var text = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var usage = root.GetProperty("usage");
            var tokensUsed = usage.GetProperty("total_tokens").GetInt32();

            // Parse the JSON response
            var parsed = ParseAiResponse(text);

            return new ChatCompletionResult
            {
                Success = true,
                Response = parsed.Response,
                DetectedIntent = parsed.Intent,
                Confidence = parsed.Confidence,
                SuggestedActions = parsed.Actions,
                TokensUsed = tokensUsed,
                EstimatedCost = CalculateCost(tokensUsed),
                ProviderUsed = ProviderName,
                ModelUsed = _options.Model
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("OpenAI API request timed out");
            return new ChatCompletionResult { Success = false, Error = "Request timed out" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return new ChatCompletionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<IntentClassificationResult> ClassifyIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        // Simplified intent classification using the main API
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

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return false;

        try
        {
            var response = await _http.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static (string Response, string? Intent, decimal? Confidence, List<SuggestedAction>? Actions) ParseAiResponse(string text)
    {
        try
        {
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

    private static decimal CalculateCost(int tokensUsed)
    {
        // GPT-4o pricing: $2.50/1M input + $10/1M output (approx average)
        return (decimal)tokensUsed * 0.000006m;
    }
}
