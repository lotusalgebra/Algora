namespace Algora.Application.DTOs.Admin;

#region Read DTOs

public record GlobalSettingsDto
{
    public string DefaultTextProvider { get; init; } = "openai";
    public string DefaultImageProvider { get; init; } = "dalle";

    public OpenAiSettingsDto OpenAi { get; init; } = new();
    public AnthropicSettingsDto Anthropic { get; init; } = new();
    public GeminiSettingsDto Gemini { get; init; } = new();
    public StabilityAiSettingsDto StabilityAi { get; init; } = new();
    public ScraperApiSettingsDto ScraperApi { get; init; } = new();
}

public record OpenAiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? MaskedApiKey { get; init; }
    public bool HasApiKey { get; init; }
    public string TextModel { get; init; } = "gpt-4o";
    public string ImageModel { get; init; } = "dall-e-3";
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 500;
}

public record AnthropicSettingsDto
{
    public string? ApiKey { get; init; }
    public string? MaskedApiKey { get; init; }
    public bool HasApiKey { get; init; }
    public string Model { get; init; } = "claude-3-5-sonnet-20241022";
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 500;
}

public record GeminiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? MaskedApiKey { get; init; }
    public bool HasApiKey { get; init; }
    public string Model { get; init; } = "gemini-1.5-pro";
    public double Temperature { get; init; } = 0.7;
    public int MaxOutputTokens { get; init; } = 500;
}

public record StabilityAiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? MaskedApiKey { get; init; }
    public bool HasApiKey { get; init; }
    public string Engine { get; init; } = "stable-diffusion-xl-1024-v1-0";
    public int Steps { get; init; } = 30;
    public double CfgScale { get; init; } = 7.0;
}

public record ScraperApiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? MaskedApiKey { get; init; }
    public bool HasApiKey { get; init; }
    public string Provider { get; init; } = "ScraperAPI";
    public bool Enabled { get; init; } = true;
    public bool RenderJs { get; init; } = true;
    public string CountryCode { get; init; } = "us";
    public int TimeoutSeconds { get; init; } = 60;
}

#endregion

#region Update DTOs

public record UpdateGlobalSettingsDto
{
    public string? DefaultTextProvider { get; init; }
    public string? DefaultImageProvider { get; init; }

    public UpdateOpenAiSettingsDto? OpenAi { get; init; }
    public UpdateAnthropicSettingsDto? Anthropic { get; init; }
    public UpdateGeminiSettingsDto? Gemini { get; init; }
    public UpdateStabilityAiSettingsDto? StabilityAi { get; init; }
    public UpdateScraperApiSettingsDto? ScraperApi { get; init; }
}

public record UpdateOpenAiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? TextModel { get; init; }
    public string? ImageModel { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public record UpdateAnthropicSettingsDto
{
    public string? ApiKey { get; init; }
    public string? Model { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public record UpdateGeminiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? Model { get; init; }
    public double? Temperature { get; init; }
    public int? MaxOutputTokens { get; init; }
}

public record UpdateStabilityAiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? Engine { get; init; }
    public int? Steps { get; init; }
    public double? CfgScale { get; init; }
}

public record UpdateScraperApiSettingsDto
{
    public string? ApiKey { get; init; }
    public string? Provider { get; init; }
    public bool? Enabled { get; init; }
    public bool? RenderJs { get; init; }
    public string? CountryCode { get; init; }
    public int? TimeoutSeconds { get; init; }
}

#endregion

#region Test Connection Results

public record ConnectionTestResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

#endregion
