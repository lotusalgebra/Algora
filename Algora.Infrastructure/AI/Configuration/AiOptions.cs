namespace Algora.Infrastructure.AI.Configuration;

public class AiOptions
{
    public const string SectionName = "AI";

    public string DefaultTextProvider { get; set; } = "openai";
    public string DefaultImageProvider { get; set; } = "dalle";
    public int MaxConcurrentRequests { get; set; } = 5;
    public int RateLimitPerMinute { get; set; } = 60;

    public OpenAiOptions OpenAi { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
    public StabilityAiOptions StabilityAi { get; set; } = new();
}

public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string TextModel { get; set; } = "gpt-4o";
    public string ImageModel { get; set; } = "dall-e-3";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
}

public class AnthropicOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;
}

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-pro";
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 500;
}

public class StabilityAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Engine { get; set; } = "stable-diffusion-xl-1024-v1-0";
    public int Steps { get; set; } = 30;
    public double CfgScale { get; set; } = 7.0;
}
