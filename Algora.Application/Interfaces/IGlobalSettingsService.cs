using Algora.Application.DTOs.Admin;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing global application settings stored in the database.
/// </summary>
public interface IGlobalSettingsService
{
    #region Full Settings

    /// <summary>
    /// Gets all global settings (for admin UI display).
    /// API keys are masked for security.
    /// </summary>
    Task<GlobalSettingsDto> GetGlobalSettingsAsync();

    /// <summary>
    /// Saves global settings. Null values are ignored (partial update).
    /// </summary>
    Task SaveGlobalSettingsAsync(UpdateGlobalSettingsDto dto);

    #endregion

    #region Provider-Specific Settings (for AI providers to consume)

    /// <summary>
    /// Gets OpenAI settings with unmasked API key.
    /// </summary>
    Task<OpenAiSettingsDto> GetOpenAiSettingsAsync();

    /// <summary>
    /// Gets Anthropic settings with unmasked API key.
    /// </summary>
    Task<AnthropicSettingsDto> GetAnthropicSettingsAsync();

    /// <summary>
    /// Gets Gemini settings with unmasked API key.
    /// </summary>
    Task<GeminiSettingsDto> GetGeminiSettingsAsync();

    /// <summary>
    /// Gets StabilityAI settings with unmasked API key.
    /// </summary>
    Task<StabilityAiSettingsDto> GetStabilityAiSettingsAsync();

    /// <summary>
    /// Gets ScraperAPI settings with unmasked API key.
    /// </summary>
    Task<ScraperApiSettingsDto> GetScraperApiSettingsAsync();

    /// <summary>
    /// Gets the default text provider name.
    /// </summary>
    Task<string> GetDefaultTextProviderAsync();

    /// <summary>
    /// Gets the default image provider name.
    /// </summary>
    Task<string> GetDefaultImageProviderAsync();

    #endregion

    #region Connection Testing

    /// <summary>
    /// Tests OpenAI API connection.
    /// </summary>
    Task<ConnectionTestResult> TestOpenAiConnectionAsync();

    /// <summary>
    /// Tests Anthropic API connection.
    /// </summary>
    Task<ConnectionTestResult> TestAnthropicConnectionAsync();

    /// <summary>
    /// Tests Gemini API connection.
    /// </summary>
    Task<ConnectionTestResult> TestGeminiConnectionAsync();

    /// <summary>
    /// Tests StabilityAI API connection.
    /// </summary>
    Task<ConnectionTestResult> TestStabilityAiConnectionAsync();

    /// <summary>
    /// Tests ScraperAPI connection.
    /// </summary>
    Task<ConnectionTestResult> TestScraperApiConnectionAsync();

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalidates the settings cache, forcing a reload from database.
    /// </summary>
    void InvalidateCache();

    #endregion
}
