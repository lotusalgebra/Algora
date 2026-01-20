namespace Algora.Application.DTOs.CustomerPortal;

/// <summary>
/// DTO for reading theme settings
/// </summary>
public class ThemeSettingsDto
{
    // Branding
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string StoreName { get; set; } = "My Store";

    // Colors
    public string PrimaryColor { get; set; } = "#7c3aed";
    public string PrimaryHoverColor { get; set; } = "#6d28d9";
    public string SecondaryColor { get; set; } = "#ec4899";
    public string AccentColor { get; set; } = "#06b6d4";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string SurfaceColor { get; set; } = "#f9fafb";
    public string TextColor { get; set; } = "#1f2937";
    public string TextMutedColor { get; set; } = "#6b7280";
    public string BorderColor { get; set; } = "#e5e7eb";
    public string ErrorColor { get; set; } = "#ef4444";
    public string SuccessColor { get; set; } = "#10b981";
    public string WarningColor { get; set; } = "#f59e0b";

    // Dark mode
    public string DarkBackgroundColor { get; set; } = "#111827";
    public string DarkSurfaceColor { get; set; } = "#1f2937";
    public string DarkTextColor { get; set; } = "#f9fafb";
    public string DarkTextMutedColor { get; set; } = "#9ca3af";
    public string DarkBorderColor { get; set; } = "#374151";

    // Typography
    public string FontFamily { get; set; } = "Inter";
    public string HeadingFontFamily { get; set; } = "Inter";
    public string FontSizeBase { get; set; } = "16px";

    // Layout
    public string ButtonStyle { get; set; } = "rounded";
    public string ButtonSize { get; set; } = "medium";
    public string CardStyle { get; set; } = "shadow";
    public string CardRadius { get; set; } = "0.75rem";
    public string InputStyle { get; set; } = "bordered";

    // Features
    public bool EnableDarkMode { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
    public bool ShowPoweredBy { get; set; } = true;

    // Custom
    public string? CustomCss { get; set; }
    public string? CustomHeadHtml { get; set; }
    public string? CustomFooterHtml { get; set; }
}

/// <summary>
/// DTO for updating theme settings
/// </summary>
public record UpdateThemeSettingsDto(
    string? LogoUrl,
    string? FaviconUrl,
    string StoreName,
    string PrimaryColor,
    string PrimaryHoverColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string SurfaceColor,
    string TextColor,
    string TextMutedColor,
    string BorderColor,
    string ErrorColor,
    string SuccessColor,
    string WarningColor,
    string DarkBackgroundColor,
    string DarkSurfaceColor,
    string DarkTextColor,
    string DarkTextMutedColor,
    string DarkBorderColor,
    string FontFamily,
    string HeadingFontFamily,
    string FontSizeBase,
    string ButtonStyle,
    string ButtonSize,
    string CardStyle,
    string CardRadius,
    string InputStyle,
    bool EnableDarkMode,
    bool EnableAnimations,
    bool ShowPoweredBy,
    string? CustomCss,
    string? CustomHeadHtml,
    string? CustomFooterHtml
);
