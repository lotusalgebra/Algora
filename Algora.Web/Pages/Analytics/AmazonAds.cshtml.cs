using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class AmazonAdsModel : PageModel
{
    private readonly IAmazonAdsService _amazonAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AmazonAdsModel> _logger;

    public AmazonAdsModel(
        IAmazonAdsService amazonAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<AmazonAdsModel> logger)
    {
        _amazonAdsService = amazonAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public AmazonAdsConnectionDto? Connection { get; set; }
    public AmazonAdsSummaryDto? Summary { get; set; }
    public List<AmazonAdsCampaignDto> Campaigns { get; set; } = new();
    public List<AmazonAdsProfileDto> AvailableProfiles { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "30days";

    [BindProperty]
    public string? SelectedProfileId { get; set; }

    [BindProperty]
    public string? AccessToken { get; set; }

    [BindProperty]
    public string? RefreshToken { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OAuthUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Handle OAuth callback
        if (!string.IsNullOrEmpty(Code))
        {
            return await HandleOAuthCallbackAsync();
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostConnectAsync()
    {
        // Generate OAuth URL and redirect
        var redirectUri = GetRedirectUri();
        var state = Guid.NewGuid().ToString("N");

        // Store state in session for validation
        HttpContext.Session.SetString("AmazonAdsOAuthState", state);

        OAuthUrl = _amazonAdsService.GetOAuthUrl(redirectUri, state);
        return Redirect(OAuthUrl);
    }

    public async Task<IActionResult> OnPostSelectProfileAsync()
    {
        if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(SelectedProfileId))
        {
            ErrorMessage = "Please select an advertising profile.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Get profile details
            var profiles = await _amazonAdsService.GetProfilesAsync(AccessToken);
            var selectedProfile = profiles.FirstOrDefault(p => p.ProfileId == SelectedProfileId);

            var dto = new SaveAmazonAdsConnectionDto(
                AccessToken,
                RefreshToken ?? string.Empty,
                SelectedProfileId,
                selectedProfile?.AccountName,
                selectedProfile?.MarketplaceId,
                selectedProfile?.CountryCode
            );

            await _amazonAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _amazonAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Amazon Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Amazon Ads connection");
            ErrorMessage = "Failed to connect Amazon Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _amazonAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Amazon Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Amazon Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _amazonAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            if (result.Success)
            {
                SuccessMessage = $"Sync completed: {result.CampaignsProcessed} campaigns processed, " +
                                 $"{result.RecordsCreated} new records, {result.RecordsUpdated} updated.";
            }
            else
            {
                ErrorMessage = $"Sync failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Amazon Ads");
            ErrorMessage = "Sync failed: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task<IActionResult> HandleOAuthCallbackAsync()
    {
        try
        {
            // Validate state
            var expectedState = HttpContext.Session.GetString("AmazonAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _amazonAdsService.ExchangeCodeAsync(Code!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from Amazon. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available advertising profiles
            AvailableProfiles = await _amazonAdsService.GetProfilesAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableProfiles.Count == 0)
            {
                ErrorMessage = "No advertising profiles found. Please make sure you have an Amazon Advertising account.";
            }
            else if (AvailableProfiles.Count == 1)
            {
                // Auto-select if only one profile
                SelectedProfileId = AvailableProfiles[0].ProfileId;
                return await OnPostSelectProfileAsync();
            }

            // Show profile selection
            await LoadDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed");
            ErrorMessage = "Authentication failed: " + ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Connection = await _amazonAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _amazonAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _amazonAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Amazon Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/amazonads";
    }

    private (DateTime startDate, DateTime endDate) GetDateRange()
    {
        var endDate = DateTime.UtcNow;
        var startDate = Period switch
        {
            "7days" => endDate.AddDays(-7),
            "30days" => endDate.AddDays(-30),
            "90days" => endDate.AddDays(-90),
            _ => endDate.AddDays(-30)
        };
        return (startDate, endDate);
    }
}
