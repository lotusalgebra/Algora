using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Algora.Infrastructure.AI;

public class AiProductDescriptionService : IAiProductDescriptionService
{
    private readonly ILogger<AiProductDescriptionService> _logger;
    private readonly string _apiKey;
    private readonly HttpClient _http;
    private readonly string? _defaultModel;
    private readonly double? _defaultTemperature;
    private readonly int? _defaultMaxTokens;

    public AiProductDescriptionService(IConfiguration config, ILogger<AiProductDescriptionService> logger, IHttpClientFactory? httpFactory = null)
    {
        _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI API key");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read optional defaults from configuration. We DO NOT hardcode the model or parameters here.
        _defaultModel = config["OpenAI:Model"];
        if (double.TryParse(config["OpenAI:Temperature"], out var t)) _defaultTemperature = t;
        if (int.TryParse(config["OpenAI:MaxTokens"], out var m)) _defaultMaxTokens = m;

        // Prefer IHttpClientFactory if available (better for DNS/handler reuse).
        _http = httpFactory?.CreateClient() ?? new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    // Backwards-compatible convenience method. This does NOT hardcode model/params;
    // it will use configuration or require caller to set them when using the generic method.
    public Task<ProductDescriptionDto> GenerateDescriptionAsync(string title, string category, string color, string material, string features)
    {
        var model = new
        {
            Title = title,
            Category = category,
            Color = color,
            Material = material,
            Features = features
        };

        // This call will require either OpenAI:Model be set in configuration or the caller to use the generic method
        // that accepts explicit model/temperature/maxTokens parameters.
        return GenerateDescriptionAsync(model, includeProperties: new[] { "Title", "Category", "Color", "Material", "Features" }, tone: "persuasive, premium", approxWords: 180);
    }

    /// <summary>
    /// Generate a product description for the provided model.
    /// All OpenAI-related runtime parameters (model, temperature, maxTokens) must be supplied either:
    /// - via parameters to this method, or
    /// - via configuration keys: OpenAI:Model, OpenAI:Temperature, OpenAI:MaxTokens.
    /// This enforces "no hardcoded model/params" inside the implementation.
    /// </summary>
    public async Task<ProductDescriptionDto> GenerateDescriptionAsync<TModel>(TModel model, string[]? includeProperties = null, string? tone = null, int approxWords = 150, string? modelName = null, double? temperature = null, int? maxTokens = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        // Resolve AI parameters: prefer explicit args, then configuration defaults, otherwise fail.
        var resolvedModel = modelName ?? _defaultModel;
        if (string.IsNullOrWhiteSpace(resolvedModel))
            throw new InvalidOperationException("AI model not specified. Provide modelName parameter or set OpenAI:Model in configuration.");

        var resolvedTemperature = temperature ?? _defaultTemperature;
        if (resolvedTemperature == null)
            throw new InvalidOperationException("AI temperature not specified. Provide temperature parameter or set OpenAI:Temperature in configuration.");

        var resolvedMaxTokens = maxTokens ?? _defaultMaxTokens;
        if (resolvedMaxTokens == null)
            throw new InvalidOperationException("AI maxTokens not specified. Provide maxTokens parameter or set OpenAI:MaxTokens in configuration.");

        // Extract properties -> string values
        var src = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var props = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(p => p.GetMethod != null);

        foreach (var p in props)
        {
            if (includeProperties != null && includeProperties.Length > 0 && !includeProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                continue;

            object? val = null;
            try { val = p.GetValue(model); } catch { val = null; }

            if (val != null)
            {
                var s = val switch
                {
                    string str => str,
                    DateTime dt => dt.ToString("o"),
                    DateTimeOffset dto => dto.ToString("o"),
                    _ => val.ToString() ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(s))
                    src[p.Name] = s;
            }
        }

        // Fallback if nothing selected
        if (src.Count == 0 && props.Any())
        {
            foreach (var p in props.Take(6))
            {
                try
                {
                    var val = p.GetValue(model);
                    if (val != null)
                    {
                        var s = val.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(s)) src[p.Name] = s;
                    }
                }
                catch { }
            }
        }

        // Build prompt
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert eCommerce copywriter.");
        sb.AppendLine($"Write an engaging, SEO-optimized product description (approx. {approxWords} words) using the following product details:");
        sb.AppendLine();

        foreach (var kv in src)
            sb.AppendLine($"- {SplitCamelCase(kv.Key)}: {kv.Value}");

        sb.AppendLine();
        sb.AppendLine("Tone: " + (string.IsNullOrWhiteSpace(tone) ? "persuasive, premium, emotionally appealing" : tone));
        sb.AppendLine("Write 2 short paragraphs, use natural formatting and descriptive language. Return only the description text.");

        var prompt = sb.ToString();

        try
        {
            var chatRequest = new
            {
                model = resolvedModel,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = resolvedMaxTokens,
                temperature = resolvedTemperature
            };

            var reqJson = JsonSerializer.Serialize(chatRequest);
            using var content = new StringContent(reqJson, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var respJson = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI request failed: {Status} {Body}", resp.StatusCode, respJson);
                throw new InvalidOperationException($"OpenAI request failed: {resp.StatusCode}");
            }

            var description = ExtractChatCompletionText(respJson);

            src.TryGetValue("Title", out var titleVal);

            // Map extracted source fields into the existing ProductDescriptionDto shape.
            static string GetSrc(IDictionary<string, string> d, string key) =>
                d.TryGetValue(key, out var v) ? v : string.Empty;

            return new ProductDescriptionDto
            {
                ProductId = 0, // unknown here — caller can set if needed
                Title = titleVal ?? string.Empty,
                Category = GetSrc(src, "Category"),
                Color = GetSrc(src, "Color"),
                Material = GetSrc(src, "Material"),
                Features = GetSrc(src, "Features"),
                Description = description ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI product description generation failed.");
            throw;
        }
    }

    private static string ExtractChatCompletionText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var first = choices[0];

                if (first.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var contentEl))
                    {
                        if (contentEl.ValueKind == JsonValueKind.String) return contentEl.GetString()!.Trim();
                        if (contentEl.ValueKind == JsonValueKind.Array && contentEl.GetArrayLength() > 0)
                        {
                            var item = contentEl[0];
                            if (item.ValueKind == JsonValueKind.String) return item.GetString()!.Trim();
                            if (item.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String) return t.GetString()!.Trim();
                        }
                    }
                }

                if (first.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    return textEl.GetString()!.Trim();
            }

            if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
            {
                var o0 = output[0];
                if (o0.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String) return c.GetString()!.Trim();
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string SplitCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(input, "([a-z0-9])([A-Z])", "$1 $2");
    }
}
