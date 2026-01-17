using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class SnapchatAdsModel : PageModel
{
    private readonly ISnapchatAdsService _snapchatAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SnapchatAdsModel> _logger;

    public SnapchatAdsModel(
        ISnapchatAdsService snapchatAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<SnapchatAdsModel> logger)
    {
        _snapchatAdsService = snapchatAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public SnapchatAdsConnectionDto? Connection { get; set; }
    public SnapchatAdsSummaryDto? Summary { get; set; }
    public List<SnapchatAdsCampaignDto> Campaigns { get; set; } = new();
    public List<SnapchatAdsAccountDto> AvailableAccounts { get; set; } = new();

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
        HttpContext.Session.SetString("SnapchatAdsOAuthState", state);

        OAuthUrl = _snapchatAdsService.GetOAuthUrl(redirectUri, state);
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
            var accounts = await _snapchatAdsService.GetAdAccountsAsync(AccessToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.Id == SelectedAdAccountId);

            var dto = new SaveSnapchatAdsConnectionDto(
                AccessToken,
                RefreshToken ?? string.Empty,
                SelectedAdAccountId,
                selectedAccount?.Name,
                selectedAccount?.OrganizationId
            );

            await _snapchatAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _snapchatAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Snapchat Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Snapchat Ads connection");
            ErrorMessage = "Failed to connect Snapchat Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _snapchatAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Snapchat Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Snapchat Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _snapchatAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync Snapchat Ads");
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
            var expectedState = HttpContext.Session.GetString("SnapchatAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _snapchatAdsService.ExchangeCodeAsync(Code!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from Snapchat. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available ad accounts
            AvailableAccounts = await _snapchatAdsService.GetAdAccountsAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No ad accounts found. Please make sure you have access to a Snapchat Ads account.";
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
            Connection = await _snapchatAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _snapchatAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _snapchatAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }

            // Generate OAuth URL for connect button
            var redirectUri = GetRedirectUri();
            var state = Guid.NewGuid().ToString("N");
            OAuthUrl = _snapchatAdsService.GetOAuthUrl(redirectUri, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Snapchat Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/snapchatads";
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
