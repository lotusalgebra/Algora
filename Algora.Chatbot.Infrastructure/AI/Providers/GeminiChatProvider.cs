using System.Text;
using System.Text.Json;
using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Chatbot.Infrastructure.AI.Providers;

public class GeminiChatProvider : IChatbotAiProvider
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiChatProvider> _logger;

    public string ProviderName => "gemini";
    public string DisplayName => "Google Gemini";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
    public int Priority => 3;

    public GeminiChatProvider(
        IHttpClientFactory httpFactory,
        IOptions<AiOptions> options,
        ILogger<GeminiChatProvider> logger)
    {
        _http = httpFactory.CreateClient("Gemini");
        _options = options.Value.Gemini;
        _logger = logger;

        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<ChatCompletionResult> GenerateResponseAsync(ChatContext context, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ChatCompletionResult
            {
                Success = false,
                Error = "Gemini API key not configured"
            };
        }

        try
        {
            var contents = new List<object>();

            // Add system instruction as first user message
            var systemMessage = context.SystemPrompt + "\n\nRespond with valid JSON containing: response, intent, confidence, suggestedActions";

            foreach (var msg in context.History)
            {
                var role = msg.Role.ToLower() == "assistant" ? "model" : "user";
                contents.Add(new { role, parts = new[] { new { text = msg.Content } } });
            }

            contents.Add(new { role = "user", parts = new[] { new { text = context.CurrentMessage } } });

            var requestBody = new
            {
                contents,
                systemInstruction = new { parts = new[] { new { text = systemMessage } } },
                generationConfig = new
                {
                    temperature = context.Temperature > 0 ? context.Temperature : _options.Temperature,
                    maxOutputTokens = context.MaxTokens > 0 ? context.MaxTokens : _options.MaxOutputTokens,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";
            var response = await _http.PostAsync(url, content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new ChatCompletionResult
                {
                    Success = false,
                    Error = $"Gemini API error: {response.StatusCode}"
                };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var text = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var tokensUsed = 0;
            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("totalTokenCount", out var total))
                {
                    tokensUsed = total.GetInt32();
                }
            }

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
            _logger.LogWarning("Gemini API request timed out");
            return new ChatCompletionResult { Success = false, Error = "Request timed out" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
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

    private static decimal CalculateCost(int tokensUsed)
    {
        // Gemini 1.5 Pro: $1.25/1M input + $5/1M output (approx average)
        return (decimal)tokensUsed * 0.000003m;
    }
}
