using Algora.Application.DTOs.Admin;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Admin;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IGlobalSettingsService _settingsService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(IGlobalSettingsService settingsService, ILogger<SettingsModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public GlobalSettingsDto Settings { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "ai";

    // Form properties for AI settings
    [BindProperty]
    public string? DefaultTextProvider { get; set; }

    [BindProperty]
    public string? DefaultImageProvider { get; set; }

    // OpenAI
    [BindProperty]
    public string? OpenAiApiKey { get; set; }

    [BindProperty]
    public string? OpenAiTextModel { get; set; }

    [BindProperty]
    public string? OpenAiImageModel { get; set; }

    [BindProperty]
    public double OpenAiTemperature { get; set; }

    [BindProperty]
    public int OpenAiMaxTokens { get; set; }

    // Anthropic
    [BindProperty]
    public string? AnthropicApiKey { get; set; }

    [BindProperty]
    public string? AnthropicModel { get; set; }

    [BindProperty]
    public double AnthropicTemperature { get; set; }

    [BindProperty]
    public int AnthropicMaxTokens { get; set; }

    // Gemini
    [BindProperty]
    public string? GeminiApiKey { get; set; }

    [BindProperty]
    public string? GeminiModel { get; set; }

    [BindProperty]
    public double GeminiTemperature { get; set; }

    [BindProperty]
    public int GeminiMaxOutputTokens { get; set; }

    // StabilityAI
    [BindProperty]
    public string? StabilityAiApiKey { get; set; }

    [BindProperty]
    public string? StabilityAiEngine { get; set; }

    [BindProperty]
    public int StabilityAiSteps { get; set; }

    [BindProperty]
    public double StabilityAiCfgScale { get; set; }

    // ScraperAPI
    [BindProperty]
    public string? ScraperApiKey { get; set; }

    [BindProperty]
    public string? ScraperApiProvider { get; set; }

    [BindProperty]
    public bool ScraperApiEnabled { get; set; }

    [BindProperty]
    public bool ScraperApiRenderJs { get; set; }

    [BindProperty]
    public string? ScraperApiCountryCode { get; set; }

    [BindProperty]
    public int ScraperApiTimeoutSeconds { get; set; }

    public async Task OnGetAsync()
    {
        await LoadSettingsAsync();
    }

    public async Task<IActionResult> OnPostSaveAiSettingsAsync()
    {
        try
        {
            var updateDto = new UpdateGlobalSettingsDto
            {
                DefaultTextProvider = DefaultTextProvider,
                DefaultImageProvider = DefaultImageProvider,
                OpenAi = new UpdateOpenAiSettingsDto
                {
                    ApiKey = string.IsNullOrWhiteSpace(OpenAiApiKey) ? null : OpenAiApiKey,
                    TextModel = OpenAiTextModel,
                    ImageModel = OpenAiImageModel,
                    Temperature = OpenAiTemperature,
                    MaxTokens = OpenAiMaxTokens
                },
                Anthropic = new UpdateAnthropicSettingsDto
                {
                    ApiKey = string.IsNullOrWhiteSpace(AnthropicApiKey) ? null : AnthropicApiKey,
                    Model = AnthropicModel,
                    Temperature = AnthropicTemperature,
                    MaxTokens = AnthropicMaxTokens
                },
                Gemini = new UpdateGeminiSettingsDto
                {
                    ApiKey = string.IsNullOrWhiteSpace(GeminiApiKey) ? null : GeminiApiKey,
                    Model = GeminiModel,
                    Temperature = GeminiTemperature,
                    MaxOutputTokens = GeminiMaxOutputTokens
                },
                StabilityAi = new UpdateStabilityAiSettingsDto
                {
                    ApiKey = string.IsNullOrWhiteSpace(StabilityAiApiKey) ? null : StabilityAiApiKey,
                    Engine = StabilityAiEngine,
                    Steps = StabilityAiSteps,
                    CfgScale = StabilityAiCfgScale
                }
            };

            await _settingsService.SaveGlobalSettingsAsync(updateDto);
            SuccessMessage = "AI settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AI settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        await LoadSettingsAsync();
        ActiveTab = "ai";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveScraperSettingsAsync()
    {
        try
        {
            var updateDto = new UpdateGlobalSettingsDto
            {
                ScraperApi = new UpdateScraperApiSettingsDto
                {
                    ApiKey = string.IsNullOrWhiteSpace(ScraperApiKey) ? null : ScraperApiKey,
                    Provider = ScraperApiProvider,
                    Enabled = ScraperApiEnabled,
                    RenderJs = ScraperApiRenderJs,
                    CountryCode = ScraperApiCountryCode,
                    TimeoutSeconds = ScraperApiTimeoutSeconds
                }
            };

            await _settingsService.SaveGlobalSettingsAsync(updateDto);
            SuccessMessage = "Scraper settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save scraper settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        await LoadSettingsAsync();
        ActiveTab = "scraper";
        return Page();
    }

    public async Task<IActionResult> OnPostTestOpenAiAsync()
    {
        var result = await _settingsService.TestOpenAiConnectionAsync();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostTestAnthropicAsync()
    {
        var result = await _settingsService.TestAnthropicConnectionAsync();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostTestGeminiAsync()
    {
        var result = await _settingsService.TestGeminiConnectionAsync();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostTestStabilityAiAsync()
    {
        var result = await _settingsService.TestStabilityAiConnectionAsync();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostTestScraperApiAsync()
    {
        var result = await _settingsService.TestScraperApiConnectionAsync();
        return new JsonResult(result);
    }

    private async Task LoadSettingsAsync()
    {
        Settings = await _settingsService.GetGlobalSettingsAsync();

        // Populate form properties
        DefaultTextProvider = Settings.DefaultTextProvider;
        DefaultImageProvider = Settings.DefaultImageProvider;

        // OpenAI
        OpenAiTextModel = Settings.OpenAi.TextModel;
        OpenAiImageModel = Settings.OpenAi.ImageModel;
        OpenAiTemperature = Settings.OpenAi.Temperature;
        OpenAiMaxTokens = Settings.OpenAi.MaxTokens;

        // Anthropic
        AnthropicModel = Settings.Anthropic.Model;
        AnthropicTemperature = Settings.Anthropic.Temperature;
        AnthropicMaxTokens = Settings.Anthropic.MaxTokens;

        // Gemini
        GeminiModel = Settings.Gemini.Model;
        GeminiTemperature = Settings.Gemini.Temperature;
        GeminiMaxOutputTokens = Settings.Gemini.MaxOutputTokens;

        // StabilityAI
        StabilityAiEngine = Settings.StabilityAi.Engine;
        StabilityAiSteps = Settings.StabilityAi.Steps;
        StabilityAiCfgScale = Settings.StabilityAi.CfgScale;

        // ScraperAPI
        ScraperApiProvider = Settings.ScraperApi.Provider;
        ScraperApiEnabled = Settings.ScraperApi.Enabled;
        ScraperApiRenderJs = Settings.ScraperApi.RenderJs;
        ScraperApiCountryCode = Settings.ScraperApi.CountryCode;
        ScraperApiTimeoutSeconds = Settings.ScraperApi.TimeoutSeconds;
    }
}
