using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.Admin;
using Algora.Application.Interfaces;
using Algora.Infrastructure.AI.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.Services;

public class GlobalSettingsService : IGlobalSettingsService
{
    private readonly IAppConfigurationService _configService;
    private readonly IEncryptionService _encryption;
    private readonly IOptions<AiOptions> _fallbackOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GlobalSettingsService> _logger;

    private const string CacheKey = "GlobalSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GlobalSettingsService(
        IAppConfigurationService configService,
        IEncryptionService encryption,
        IOptions<AiOptions> fallbackOptions,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<GlobalSettingsService> logger)
    {
        _configService = configService;
        _encryption = encryption;
        _fallbackOptions = fallbackOptions;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    #region Full Settings

    public async Task<GlobalSettingsDto> GetGlobalSettingsAsync()
    {
        var openAi = await GetOpenAiSettingsAsync();
        var anthropic = await GetAnthropicSettingsAsync();
        var gemini = await GetGeminiSettingsAsync();
        var stabilityAi = await GetStabilityAiSettingsAsync();
        var scraperApi = await GetScraperApiSettingsAsync();

        return new GlobalSettingsDto
        {
            DefaultTextProvider = await GetDefaultTextProviderAsync(),
            DefaultImageProvider = await GetDefaultImageProviderAsync(),
            OpenAi = openAi with
            {
                ApiKey = null, // Don't expose raw key
                MaskedApiKey = _encryption.MaskValue(openAi.ApiKey ?? "")
            },
            Anthropic = anthropic with
            {
                ApiKey = null,
                MaskedApiKey = _encryption.MaskValue(anthropic.ApiKey ?? "")
            },
            Gemini = gemini with
            {
                ApiKey = null,
                MaskedApiKey = _encryption.MaskValue(gemini.ApiKey ?? "")
            },
            StabilityAi = stabilityAi with
            {
                ApiKey = null,
                MaskedApiKey = _encryption.MaskValue(stabilityAi.ApiKey ?? "")
            },
            ScraperApi = scraperApi with
            {
                ApiKey = null,
                MaskedApiKey = _encryption.MaskValue(scraperApi.ApiKey ?? "")
            }
        };
    }

    public async Task SaveGlobalSettingsAsync(UpdateGlobalSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.DefaultTextProvider))
            await SetValueAsync("AI:DefaultTextProvider", dto.DefaultTextProvider);

        if (!string.IsNullOrEmpty(dto.DefaultImageProvider))
            await SetValueAsync("AI:DefaultImageProvider", dto.DefaultImageProvider);

        if (dto.OpenAi is not null)
            await SaveOpenAiSettingsAsync(dto.OpenAi);

        if (dto.Anthropic is not null)
            await SaveAnthropicSettingsAsync(dto.Anthropic);

        if (dto.Gemini is not null)
            await SaveGeminiSettingsAsync(dto.Gemini);

        if (dto.StabilityAi is not null)
            await SaveStabilityAiSettingsAsync(dto.StabilityAi);

        if (dto.ScraperApi is not null)
            await SaveScraperApiSettingsAsync(dto.ScraperApi);

        InvalidateCache();
    }

    private async Task SaveOpenAiSettingsAsync(UpdateOpenAiSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ApiKey))
            await SetEncryptedValueAsync("AI:OpenAi:ApiKey", dto.ApiKey);
        if (!string.IsNullOrEmpty(dto.TextModel))
            await SetValueAsync("AI:OpenAi:TextModel", dto.TextModel);
        if (!string.IsNullOrEmpty(dto.ImageModel))
            await SetValueAsync("AI:OpenAi:ImageModel", dto.ImageModel);
        if (dto.Temperature.HasValue)
            await SetValueAsync("AI:OpenAi:Temperature", dto.Temperature.Value.ToString());
        if (dto.MaxTokens.HasValue)
            await SetValueAsync("AI:OpenAi:MaxTokens", dto.MaxTokens.Value.ToString());
    }

    private async Task SaveAnthropicSettingsAsync(UpdateAnthropicSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ApiKey))
            await SetEncryptedValueAsync("AI:Anthropic:ApiKey", dto.ApiKey);
        if (!string.IsNullOrEmpty(dto.Model))
            await SetValueAsync("AI:Anthropic:Model", dto.Model);
        if (dto.Temperature.HasValue)
            await SetValueAsync("AI:Anthropic:Temperature", dto.Temperature.Value.ToString());
        if (dto.MaxTokens.HasValue)
            await SetValueAsync("AI:Anthropic:MaxTokens", dto.MaxTokens.Value.ToString());
    }

    private async Task SaveGeminiSettingsAsync(UpdateGeminiSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ApiKey))
            await SetEncryptedValueAsync("AI:Gemini:ApiKey", dto.ApiKey);
        if (!string.IsNullOrEmpty(dto.Model))
            await SetValueAsync("AI:Gemini:Model", dto.Model);
        if (dto.Temperature.HasValue)
            await SetValueAsync("AI:Gemini:Temperature", dto.Temperature.Value.ToString());
        if (dto.MaxOutputTokens.HasValue)
            await SetValueAsync("AI:Gemini:MaxOutputTokens", dto.MaxOutputTokens.Value.ToString());
    }

    private async Task SaveStabilityAiSettingsAsync(UpdateStabilityAiSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ApiKey))
            await SetEncryptedValueAsync("AI:StabilityAi:ApiKey", dto.ApiKey);
        if (!string.IsNullOrEmpty(dto.Engine))
            await SetValueAsync("AI:StabilityAi:Engine", dto.Engine);
        if (dto.Steps.HasValue)
            await SetValueAsync("AI:StabilityAi:Steps", dto.Steps.Value.ToString());
        if (dto.CfgScale.HasValue)
            await SetValueAsync("AI:StabilityAi:CfgScale", dto.CfgScale.Value.ToString());
    }

    private async Task SaveScraperApiSettingsAsync(UpdateScraperApiSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ApiKey))
            await SetEncryptedValueAsync("ScraperApi:ApiKey", dto.ApiKey);
        if (!string.IsNullOrEmpty(dto.Provider))
            await SetValueAsync("ScraperApi:Provider", dto.Provider);
        if (dto.Enabled.HasValue)
            await SetValueAsync("ScraperApi:Enabled", dto.Enabled.Value.ToString());
        if (dto.RenderJs.HasValue)
            await SetValueAsync("ScraperApi:RenderJs", dto.RenderJs.Value.ToString());
        if (!string.IsNullOrEmpty(dto.CountryCode))
            await SetValueAsync("ScraperApi:CountryCode", dto.CountryCode);
        if (dto.TimeoutSeconds.HasValue)
            await SetValueAsync("ScraperApi:TimeoutSeconds", dto.TimeoutSeconds.Value.ToString());
    }

    #endregion

    #region Provider-Specific Settings

    public async Task<OpenAiSettingsDto> GetOpenAiSettingsAsync()
    {
        var fallback = _fallbackOptions.Value.OpenAi;

        var apiKey = await GetDecryptedValueAsync("AI:OpenAi:ApiKey") ?? fallback.ApiKey;
        var textModel = await GetValueAsync("AI:OpenAi:TextModel") ?? fallback.TextModel;
        var imageModel = await GetValueAsync("AI:OpenAi:ImageModel") ?? fallback.ImageModel;
        var temperature = await GetDoubleValueAsync("AI:OpenAi:Temperature") ?? fallback.Temperature;
        var maxTokens = await GetIntValueAsync("AI:OpenAi:MaxTokens") ?? fallback.MaxTokens;

        return new OpenAiSettingsDto
        {
            ApiKey = apiKey,
            HasApiKey = !string.IsNullOrWhiteSpace(apiKey),
            TextModel = textModel,
            ImageModel = imageModel,
            Temperature = temperature,
            MaxTokens = maxTokens
        };
    }

    public async Task<AnthropicSettingsDto> GetAnthropicSettingsAsync()
    {
        var fallback = _fallbackOptions.Value.Anthropic;

        var apiKey = await GetDecryptedValueAsync("AI:Anthropic:ApiKey") ?? fallback.ApiKey;
        var model = await GetValueAsync("AI:Anthropic:Model") ?? fallback.Model;
        var temperature = await GetDoubleValueAsync("AI:Anthropic:Temperature") ?? fallback.Temperature;
        var maxTokens = await GetIntValueAsync("AI:Anthropic:MaxTokens") ?? fallback.MaxTokens;

        return new AnthropicSettingsDto
        {
            ApiKey = apiKey,
            HasApiKey = !string.IsNullOrWhiteSpace(apiKey),
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens
        };
    }

    public async Task<GeminiSettingsDto> GetGeminiSettingsAsync()
    {
        var fallback = _fallbackOptions.Value.Gemini;

        var apiKey = await GetDecryptedValueAsync("AI:Gemini:ApiKey") ?? fallback.ApiKey;
        var model = await GetValueAsync("AI:Gemini:Model") ?? fallback.Model;
        var temperature = await GetDoubleValueAsync("AI:Gemini:Temperature") ?? fallback.Temperature;
        var maxOutputTokens = await GetIntValueAsync("AI:Gemini:MaxOutputTokens") ?? fallback.MaxOutputTokens;

        return new GeminiSettingsDto
        {
            ApiKey = apiKey,
            HasApiKey = !string.IsNullOrWhiteSpace(apiKey),
            Model = model,
            Temperature = temperature,
            MaxOutputTokens = maxOutputTokens
        };
    }

    public async Task<StabilityAiSettingsDto> GetStabilityAiSettingsAsync()
    {
        var fallback = _fallbackOptions.Value.StabilityAi;

        var apiKey = await GetDecryptedValueAsync("AI:StabilityAi:ApiKey") ?? fallback.ApiKey;
        var engine = await GetValueAsync("AI:StabilityAi:Engine") ?? fallback.Engine;
        var steps = await GetIntValueAsync("AI:StabilityAi:Steps") ?? fallback.Steps;
        var cfgScale = await GetDoubleValueAsync("AI:StabilityAi:CfgScale") ?? fallback.CfgScale;

        return new StabilityAiSettingsDto
        {
            ApiKey = apiKey,
            HasApiKey = !string.IsNullOrWhiteSpace(apiKey),
            Engine = engine,
            Steps = steps,
            CfgScale = cfgScale
        };
    }

    public async Task<ScraperApiSettingsDto> GetScraperApiSettingsAsync()
    {
        var apiKey = await GetDecryptedValueAsync("ScraperApi:ApiKey");
        var provider = await GetValueAsync("ScraperApi:Provider") ?? "ScraperAPI";
        var enabled = await GetBoolValueAsync("ScraperApi:Enabled") ?? true;
        var renderJs = await GetBoolValueAsync("ScraperApi:RenderJs") ?? true;
        var countryCode = await GetValueAsync("ScraperApi:CountryCode") ?? "us";
        var timeoutSeconds = await GetIntValueAsync("ScraperApi:TimeoutSeconds") ?? 60;

        return new ScraperApiSettingsDto
        {
            ApiKey = apiKey,
            HasApiKey = !string.IsNullOrWhiteSpace(apiKey),
            Provider = provider,
            Enabled = enabled,
            RenderJs = renderJs,
            CountryCode = countryCode,
            TimeoutSeconds = timeoutSeconds
        };
    }

    public async Task<string> GetDefaultTextProviderAsync()
    {
        return await GetValueAsync("AI:DefaultTextProvider") ?? _fallbackOptions.Value.DefaultTextProvider;
    }

    public async Task<string> GetDefaultImageProviderAsync()
    {
        return await GetValueAsync("AI:DefaultImageProvider") ?? _fallbackOptions.Value.DefaultImageProvider;
    }

    #endregion

    #region Connection Testing

    public async Task<ConnectionTestResult> TestOpenAiConnectionAsync()
    {
        try
        {
            var settings = await GetOpenAiSettingsAsync();
            if (!settings.HasApiKey)
                return new ConnectionTestResult { Success = false, Error = "API key not configured" };

            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            var response = await client.GetAsync("models");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, Message = "Connection successful" };

            var error = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"API returned {response.StatusCode}: {error}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI connection test failed");
            return new ConnectionTestResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ConnectionTestResult> TestAnthropicConnectionAsync()
    {
        try
        {
            var settings = await GetAnthropicSettingsAsync();
            if (!settings.HasApiKey)
                return new ConnectionTestResult { Success = false, Error = "API key not configured" };

            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            client.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = settings.Model,
                max_tokens = 10,
                messages = new[] { new { role = "user", content = "Hi" } }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("messages", content);

            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, Message = "Connection successful" };

            var error = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"API returned {response.StatusCode}: {error}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic connection test failed");
            return new ConnectionTestResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ConnectionTestResult> TestGeminiConnectionAsync()
    {
        try
        {
            var settings = await GetGeminiSettingsAsync();
            if (!settings.HasApiKey)
                return new ConnectionTestResult { Success = false, Error = "API key not configured" };

            using var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={settings.ApiKey}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, Message = "Connection successful" };

            var error = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"API returned {response.StatusCode}: {error}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini connection test failed");
            return new ConnectionTestResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ConnectionTestResult> TestStabilityAiConnectionAsync()
    {
        try
        {
            var settings = await GetStabilityAiSettingsAsync();
            if (!settings.HasApiKey)
                return new ConnectionTestResult { Success = false, Error = "API key not configured" };

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            var response = await client.GetAsync("https://api.stability.ai/v1/user/account");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, Message = "Connection successful" };

            var error = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"API returned {response.StatusCode}: {error}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StabilityAI connection test failed");
            return new ConnectionTestResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ConnectionTestResult> TestScraperApiConnectionAsync()
    {
        try
        {
            var settings = await GetScraperApiSettingsAsync();
            if (!settings.HasApiKey)
                return new ConnectionTestResult { Success = false, Error = "API key not configured" };

            using var client = _httpClientFactory.CreateClient();
            var testUrl = $"https://api.scraperapi.com/?api_key={settings.ApiKey}&url=https://httpbin.org/ip";

            var response = await client.GetAsync(testUrl);
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, Message = "Connection successful" };

            var error = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"API returned {response.StatusCode}: {error}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScraperAPI connection test failed");
            return new ConnectionTestResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Cache Management

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    #endregion

    #region Helper Methods

    private async Task<string?> GetValueAsync(string key)
    {
        return await _configService.GetValueAsync(key);
    }

    private async Task<string?> GetDecryptedValueAsync(string key)
    {
        var value = await _configService.GetValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return null;

        return _encryption.IsSensitiveKey(key) ? _encryption.Decrypt(value) : value;
    }

    private async Task SetValueAsync(string key, string value)
    {
        await _configService.SetValueAsync(key, value);
    }

    private async Task SetEncryptedValueAsync(string key, string value)
    {
        var encryptedValue = _encryption.IsSensitiveKey(key) ? _encryption.Encrypt(value) : value;
        await _configService.SetValueAsync(key, encryptedValue);
    }

    private async Task<int?> GetIntValueAsync(string key)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : null;
    }

    private async Task<double?> GetDoubleValueAsync(string key)
    {
        var value = await GetValueAsync(key);
        return double.TryParse(value, out var result) ? result : null;
    }

    private async Task<bool?> GetBoolValueAsync(string key)
    {
        var value = await GetValueAsync(key);
        return bool.TryParse(value, out var result) ? result : null;
    }

    #endregion
}
