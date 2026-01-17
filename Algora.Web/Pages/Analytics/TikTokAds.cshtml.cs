using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class TikTokAdsModel : PageModel
{
    private readonly ITikTokAdsService _tikTokAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TikTokAdsModel> _logger;

    public TikTokAdsModel(
        ITikTokAdsService tikTokAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<TikTokAdsModel> logger)
    {
        _tikTokAdsService = tikTokAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public TikTokAdsConnectionDto? Connection { get; set; }
    public TikTokAdsSummaryDto? Summary { get; set; }
    public List<TikTokAdsCampaignDto> Campaigns { get; set; } = new();
    public List<TikTokAdsAdvertiserDto> AvailableAdvertisers { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? AuthCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "30days";

    [BindProperty]
    public string? SelectedAdvertiserId { get; set; }

    [BindProperty]
    public string? AccessToken { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OAuthUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Handle OAuth callback
        if (!string.IsNullOrEmpty(AuthCode))
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
        HttpContext.Session.SetString("TikTokAdsOAuthState", state);

        OAuthUrl = _tikTokAdsService.GetOAuthUrl(redirectUri, state);
        return Redirect(OAuthUrl);
    }

    public async Task<IActionResult> OnPostSelectAccountAsync()
    {
        if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(SelectedAdvertiserId))
        {
            ErrorMessage = "Please select an advertiser account.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Get account details
            var advertisers = await _tikTokAdsService.GetAdvertisersAsync(AccessToken);
            var selectedAdvertiser = advertisers.FirstOrDefault(a => a.AdvertiserId == SelectedAdvertiserId);

            var dto = new SaveTikTokAdsConnectionDto(
                AccessToken,
                SelectedAdvertiserId,
                selectedAdvertiser?.AdvertiserName,
                selectedAdvertiser?.BusinessCenterId
            );

            await _tikTokAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _tikTokAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "TikTok Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save TikTok Ads connection");
            ErrorMessage = "Failed to connect TikTok Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _tikTokAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "TikTok Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect TikTok Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _tikTokAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync TikTok Ads");
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
            var expectedState = HttpContext.Session.GetString("TikTokAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _tikTokAdsService.ExchangeCodeAsync(AuthCode!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from TikTok. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available advertiser accounts
            AvailableAdvertisers = await _tikTokAdsService.GetAdvertisersAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;

            if (AvailableAdvertisers.Count == 0)
            {
                ErrorMessage = "No advertiser accounts found. Please make sure you have access to a TikTok Ads account.";
            }
            else if (AvailableAdvertisers.Count == 1)
            {
                // Auto-select if only one account
                SelectedAdvertiserId = AvailableAdvertisers[0].AdvertiserId;
                return await OnPostSelectAccountAsync();
            }

            // Show account selection
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
            Connection = await _tikTokAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _tikTokAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _tikTokAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }

            // Generate OAuth URL for connect button
            var redirectUri = GetRedirectUri();
            var state = Guid.NewGuid().ToString("N");
            OAuthUrl = _tikTokAdsService.GetOAuthUrl(redirectUri, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load TikTok Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/tiktokads";
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
