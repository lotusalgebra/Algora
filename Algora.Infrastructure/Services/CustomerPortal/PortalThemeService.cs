using Algora.Application.DTOs.CustomerPortal;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerPortal;

/// <summary>
/// Service for managing Customer Portal theme settings
/// </summary>
public class PortalThemeService : IPortalThemeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PortalThemeService> _logger;

    public PortalThemeService(AppDbContext dbContext, ILogger<PortalThemeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ThemeSettingsDto> GetThemeSettingsAsync(string shopDomain)
    {
        var settings = await _dbContext.Set<PortalThemeSettings>()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            // Return default settings
            return new ThemeSettingsDto();
        }

        return MapToDto(settings);
    }

    public async Task SaveThemeSettingsAsync(string shopDomain, UpdateThemeSettingsDto dto)
    {
        var settings = await _dbContext.Set<PortalThemeSettings>()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new PortalThemeSettings
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Set<PortalThemeSettings>().Add(settings);
        }

        // Update all properties
        settings.LogoUrl = dto.LogoUrl;
        settings.FaviconUrl = dto.FaviconUrl;
        settings.StoreName = dto.StoreName;
        settings.PrimaryColor = dto.PrimaryColor;
        settings.PrimaryHoverColor = dto.PrimaryHoverColor;
        settings.SecondaryColor = dto.SecondaryColor;
        settings.AccentColor = dto.AccentColor;
        settings.BackgroundColor = dto.BackgroundColor;
        settings.SurfaceColor = dto.SurfaceColor;
        settings.TextColor = dto.TextColor;
        settings.TextMutedColor = dto.TextMutedColor;
        settings.BorderColor = dto.BorderColor;
        settings.ErrorColor = dto.ErrorColor;
        settings.SuccessColor = dto.SuccessColor;
        settings.WarningColor = dto.WarningColor;
        settings.DarkBackgroundColor = dto.DarkBackgroundColor;
        settings.DarkSurfaceColor = dto.DarkSurfaceColor;
        settings.DarkTextColor = dto.DarkTextColor;
        settings.DarkTextMutedColor = dto.DarkTextMutedColor;
        settings.DarkBorderColor = dto.DarkBorderColor;
        settings.FontFamily = dto.FontFamily;
        settings.HeadingFontFamily = dto.HeadingFontFamily;
        settings.FontSizeBase = dto.FontSizeBase;
        settings.ButtonStyle = dto.ButtonStyle;
        settings.ButtonSize = dto.ButtonSize;
        settings.CardStyle = dto.CardStyle;
        settings.CardRadius = dto.CardRadius;
        settings.InputStyle = dto.InputStyle;
        settings.EnableDarkMode = dto.EnableDarkMode;
        settings.EnableAnimations = dto.EnableAnimations;
        settings.ShowPoweredBy = dto.ShowPoweredBy;
        settings.CustomCss = dto.CustomCss;
        settings.CustomHeadHtml = dto.CustomHeadHtml;
        settings.CustomFooterHtml = dto.CustomFooterHtml;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Saved portal theme settings for {ShopDomain}", shopDomain);
    }

    public async Task ResetToDefaultAsync(string shopDomain)
    {
        var settings = await _dbContext.Set<PortalThemeSettings>()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings != null)
        {
            _dbContext.Set<PortalThemeSettings>().Remove(settings);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Reset portal theme settings for {ShopDomain}", shopDomain);
        }
    }

    public async Task<string> GenerateThemeCssAsync(string shopDomain)
    {
        var settings = await GetThemeSettingsAsync(shopDomain);

        var btnRadius = settings.ButtonStyle switch
        {
            "pill" => "9999px",
            "square" => "0",
            _ => "0.5rem"
        };

        return $@"
:root {{
    --primary: {settings.PrimaryColor};
    --primary-hover: {settings.PrimaryHoverColor};
    --secondary: {settings.SecondaryColor};
    --accent: {settings.AccentColor};
    --background: {settings.BackgroundColor};
    --surface: {settings.SurfaceColor};
    --text: {settings.TextColor};
    --text-muted: {settings.TextMutedColor};
    --border: {settings.BorderColor};
    --error: {settings.ErrorColor};
    --success: {settings.SuccessColor};
    --warning: {settings.WarningColor};
    --font-family: '{settings.FontFamily}', sans-serif;
    --heading-font: '{settings.HeadingFontFamily}', sans-serif;
    --font-size-base: {settings.FontSizeBase};
    --btn-radius: {btnRadius};
    --card-radius: {settings.CardRadius};
}}

.dark {{
    --background: {settings.DarkBackgroundColor};
    --surface: {settings.DarkSurfaceColor};
    --text: {settings.DarkTextColor};
    --text-muted: {settings.DarkTextMutedColor};
    --border: {settings.DarkBorderColor};
}}

{settings.CustomCss ?? ""}
";
    }

    private static ThemeSettingsDto MapToDto(PortalThemeSettings settings)
    {
        return new ThemeSettingsDto
        {
            LogoUrl = settings.LogoUrl,
            FaviconUrl = settings.FaviconUrl,
            StoreName = settings.StoreName,
            PrimaryColor = settings.PrimaryColor,
            PrimaryHoverColor = settings.PrimaryHoverColor,
            SecondaryColor = settings.SecondaryColor,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            SurfaceColor = settings.SurfaceColor,
            TextColor = settings.TextColor,
            TextMutedColor = settings.TextMutedColor,
            BorderColor = settings.BorderColor,
            ErrorColor = settings.ErrorColor,
            SuccessColor = settings.SuccessColor,
            WarningColor = settings.WarningColor,
            DarkBackgroundColor = settings.DarkBackgroundColor,
            DarkSurfaceColor = settings.DarkSurfaceColor,
            DarkTextColor = settings.DarkTextColor,
            DarkTextMutedColor = settings.DarkTextMutedColor,
            DarkBorderColor = settings.DarkBorderColor,
            FontFamily = settings.FontFamily,
            HeadingFontFamily = settings.HeadingFontFamily,
            FontSizeBase = settings.FontSizeBase,
            ButtonStyle = settings.ButtonStyle,
            ButtonSize = settings.ButtonSize,
            CardStyle = settings.CardStyle,
            CardRadius = settings.CardRadius,
            InputStyle = settings.InputStyle,
            EnableDarkMode = settings.EnableDarkMode,
            EnableAnimations = settings.EnableAnimations,
            ShowPoweredBy = settings.ShowPoweredBy,
            CustomCss = settings.CustomCss,
            CustomHeadHtml = settings.CustomHeadHtml,
            CustomFooterHtml = settings.CustomFooterHtml
        };
    }
}
