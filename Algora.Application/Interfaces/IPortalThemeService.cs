using Algora.Application.DTOs.CustomerPortal;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Customer Portal theme settings
/// </summary>
public interface IPortalThemeService
{
    /// <summary>
    /// Gets the theme settings for a shop
    /// </summary>
    Task<ThemeSettingsDto> GetThemeSettingsAsync(string shopDomain);

    /// <summary>
    /// Saves theme settings for a shop
    /// </summary>
    Task SaveThemeSettingsAsync(string shopDomain, UpdateThemeSettingsDto dto);

    /// <summary>
    /// Resets theme settings to defaults for a shop
    /// </summary>
    Task ResetToDefaultAsync(string shopDomain);

    /// <summary>
    /// Generates CSS variables from theme settings
    /// </summary>
    Task<string> GenerateThemeCssAsync(string shopDomain);
}
