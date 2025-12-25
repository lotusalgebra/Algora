namespace Algora.Chatbot.Infrastructure.AI.Configuration;

public class AiOptions
{
    public string DefaultProvider { get; set; } = "openai";
    public string? FallbackProvider { get; set; } = "anthropic";
    public int MaxConcurrentRequests { get; set; } = 10;
    public int RateLimitPerMinute { get; set; } = 100;

    public OpenAiOptions OpenAi { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
}

public class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
}

public class AnthropicOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
}

public class GeminiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-pro";
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 500;
}
