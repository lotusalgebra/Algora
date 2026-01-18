using Algora.CustomerPortal.Application.DTOs;
using Algora.CustomerPortal.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Settings;

public class PortalThemeModel : PageModel
{
    private readonly IPortalThemeService _themeService;
    private readonly AppDbContext _dbContext;

    public PortalThemeModel(IPortalThemeService themeService, AppDbContext dbContext)
    {
        _themeService = themeService;
        _dbContext = dbContext;
    }

    public ThemeSettingsDto? Theme { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ThemeInputModel Input { get; set; } = new();

    private string ShopDomain => HttpContext.Items["ShopDomain"]?.ToString() ?? "";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadThemeAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadThemeAsync();
            return Page();
        }

        try
        {
            var updateDto = new UpdateThemeSettingsDto(
                LogoUrl: Input.LogoUrl,
                FaviconUrl: Input.FaviconUrl,
                StoreName: Input.StoreName,
                PrimaryColor: Input.PrimaryColor,
                PrimaryHoverColor: Input.PrimaryHoverColor,
                SecondaryColor: Input.SecondaryColor,
                AccentColor: Input.AccentColor,
                BackgroundColor: Input.BackgroundColor,
                SurfaceColor: Input.SurfaceColor,
                TextColor: Input.TextColor,
                TextMutedColor: Input.TextMutedColor,
                BorderColor: Input.BorderColor,
                ErrorColor: Input.ErrorColor,
                SuccessColor: Input.SuccessColor,
                WarningColor: Input.WarningColor,
                DarkBackgroundColor: Input.DarkBackgroundColor,
                DarkSurfaceColor: Input.DarkSurfaceColor,
                DarkTextColor: Input.DarkTextColor,
                DarkTextMutedColor: Input.DarkTextMutedColor,
                DarkBorderColor: Input.DarkBorderColor,
                FontFamily: Input.FontFamily,
                HeadingFontFamily: Input.HeadingFontFamily,
                FontSizeBase: Input.FontSizeBase,
                ButtonStyle: Input.ButtonStyle,
                ButtonSize: Input.ButtonSize,
                CardStyle: Input.CardStyle,
                CardRadius: Input.CardRadius,
                InputStyle: Input.InputStyle,
                EnableDarkMode: Input.EnableDarkMode,
                EnableAnimations: Input.EnableAnimations,
                ShowPoweredBy: Input.ShowPoweredBy,
                CustomCss: Input.CustomCss,
                CustomHeadHtml: Input.CustomHeadHtml,
                CustomFooterHtml: Input.CustomFooterHtml
            );

            await _themeService.SaveThemeSettingsAsync(ShopDomain, updateDto);
            SuccessMessage = "Theme settings saved successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving theme: {ex.Message}";
        }

        await LoadThemeAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        try
        {
            await _themeService.ResetToDefaultAsync(ShopDomain);
            SuccessMessage = "Theme reset to defaults successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error resetting theme: {ex.Message}";
        }

        await LoadThemeAsync();
        return Page();
    }

    private async Task LoadThemeAsync()
    {
        Theme = await _themeService.GetThemeSettingsAsync(ShopDomain);

        if (Theme != null)
        {
            Input = new ThemeInputModel
            {
                LogoUrl = Theme.LogoUrl,
                FaviconUrl = Theme.FaviconUrl,
                StoreName = Theme.StoreName,
                PrimaryColor = Theme.PrimaryColor,
                PrimaryHoverColor = Theme.PrimaryHoverColor,
                SecondaryColor = Theme.SecondaryColor,
                AccentColor = Theme.AccentColor,
                BackgroundColor = Theme.BackgroundColor,
                SurfaceColor = Theme.SurfaceColor,
                TextColor = Theme.TextColor,
                TextMutedColor = Theme.TextMutedColor,
                BorderColor = Theme.BorderColor,
                ErrorColor = Theme.ErrorColor,
                SuccessColor = Theme.SuccessColor,
                WarningColor = Theme.WarningColor,
                DarkBackgroundColor = Theme.DarkBackgroundColor,
                DarkSurfaceColor = Theme.DarkSurfaceColor,
                DarkTextColor = Theme.DarkTextColor,
                DarkTextMutedColor = Theme.DarkTextMutedColor,
                DarkBorderColor = Theme.DarkBorderColor,
                FontFamily = Theme.FontFamily,
                HeadingFontFamily = Theme.HeadingFontFamily,
                FontSizeBase = Theme.FontSizeBase,
                ButtonStyle = Theme.ButtonStyle,
                ButtonSize = Theme.ButtonSize,
                CardStyle = Theme.CardStyle,
                CardRadius = Theme.CardRadius,
                InputStyle = Theme.InputStyle,
                EnableDarkMode = Theme.EnableDarkMode,
                EnableAnimations = Theme.EnableAnimations,
                ShowPoweredBy = Theme.ShowPoweredBy,
                CustomCss = Theme.CustomCss,
                CustomHeadHtml = Theme.CustomHeadHtml,
                CustomFooterHtml = Theme.CustomFooterHtml
            };
        }
    }

    public class ThemeInputModel
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
}
