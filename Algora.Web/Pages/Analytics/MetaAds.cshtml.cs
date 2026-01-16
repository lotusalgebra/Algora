using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class MetaAdsModel : PageModel
{
    private readonly IMetaAdsService _metaAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaAdsModel> _logger;

    public MetaAdsModel(
        IMetaAdsService metaAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<MetaAdsModel> logger)
    {
        _metaAdsService = metaAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public MetaAdsConnectionDto? Connection { get; set; }
    public MetaAdsSummaryDto? Summary { get; set; }
    public List<MetaAdsCampaignDto> Campaigns { get; set; } = new();
    public List<MetaAdAccountDto> AvailableAccounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "30days";

    [BindProperty]
    public string? SelectedAdAccountId { get; set; }

    [BindProperty]
    public string? AccessToken { get; set; }

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
        HttpContext.Session.SetString("MetaAdsOAuthState", state);

        OAuthUrl = _metaAdsService.GetOAuthUrl(redirectUri, state);
        return Redirect(OAuthUrl);
    }

    public async Task<IActionResult> OnPostSelectAccountAsync()
    {
        if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(SelectedAdAccountId))
        {
            ErrorMessage = "Please select an ad account.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Get account details
            var accounts = await _metaAdsService.GetAdAccountsAsync(AccessToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.Id == SelectedAdAccountId);

            var dto = new SaveMetaAdsConnectionDto(
                AccessToken,
                SelectedAdAccountId,
                selectedAccount?.Name,
                selectedAccount?.BusinessName
            );

            await _metaAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _metaAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Meta Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Meta Ads connection");
            ErrorMessage = "Failed to connect Meta Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _metaAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Meta Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Meta Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _metaAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync Meta Ads");
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
            var expectedState = HttpContext.Session.GetString("MetaAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for access token
            var appId = _configuration["Meta:AppId"];
            var appSecret = _configuration["Meta:AppSecret"];
            var redirectUri = GetRedirectUri();

            using var httpClient = new HttpClient();
            var tokenUrl = $"https://graph.facebook.com/v18.0/oauth/access_token" +
                           $"?client_id={appId}" +
                           $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                           $"&client_secret={appSecret}" +
                           $"&code={Code}";

            var response = await httpClient.GetAsync(tokenUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OAuth token exchange failed: {Response}", json);
                ErrorMessage = "Failed to authenticate with Meta. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(accessToken))
            {
                ErrorMessage = "Failed to get access token from Meta.";
                await LoadDataAsync();
                return Page();
            }

            // Get available ad accounts
            AvailableAccounts = await _metaAdsService.GetAdAccountsAsync(accessToken);
            AccessToken = accessToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No ad accounts found. Please make sure you have access to a Meta Ads account.";
            }
            else if (AvailableAccounts.Count == 1)
            {
                // Auto-select if only one account
                SelectedAdAccountId = AvailableAccounts[0].Id;
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
            Connection = await _metaAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _metaAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _metaAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }

            // Generate OAuth URL for connect button
            var redirectUri = GetRedirectUri();
            var state = Guid.NewGuid().ToString("N");
            OAuthUrl = _metaAdsService.GetOAuthUrl(redirectUri, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Meta Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/metaads";
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
