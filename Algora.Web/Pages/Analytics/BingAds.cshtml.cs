using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class BingAdsModel : PageModel
{
    private readonly IBingAdsService _bingAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BingAdsModel> _logger;

    public BingAdsModel(
        IBingAdsService bingAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<BingAdsModel> logger)
    {
        _bingAdsService = bingAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public BingAdsConnectionDto? Connection { get; set; }
    public BingAdsSummaryDto? Summary { get; set; }
    public List<BingAdsCampaignDto> Campaigns { get; set; } = new();
    public List<BingAdsAccountDto> AvailableAccounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "30days";

    [BindProperty]
    public string? SelectedAccountId { get; set; }

    [BindProperty]
    public string? SelectedCustomerId { get; set; }

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
        HttpContext.Session.SetString("BingAdsOAuthState", state);

        OAuthUrl = _bingAdsService.GetOAuthUrl(redirectUri, state);

        return Redirect(OAuthUrl);
    }

    public async Task<IActionResult> OnPostSelectAccountAsync()
    {
        if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(SelectedAccountId))
        {
            ErrorMessage = "Please select an advertising account.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Get account details
            var accounts = await _bingAdsService.GetAccountsAsync(AccessToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.AccountId == SelectedAccountId);

            var dto = new SaveBingAdsConnectionDto(
                AccessToken,
                RefreshToken ?? string.Empty,
                SelectedAccountId,
                selectedAccount?.AccountName,
                selectedAccount?.CustomerId ?? SelectedCustomerId
            );

            await _bingAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _bingAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Microsoft Advertising connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Bing Ads connection");
            ErrorMessage = "Failed to connect Microsoft Advertising: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _bingAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Microsoft Advertising disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Bing Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _bingAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync Bing Ads");
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
            var expectedState = HttpContext.Session.GetString("BingAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _bingAdsService.ExchangeCodeAsync(Code!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from Microsoft. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available ad accounts
            AvailableAccounts = await _bingAdsService.GetAccountsAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No advertising accounts found. Please make sure you have access to a Microsoft Advertising account.";
            }
            else if (AvailableAccounts.Count == 1)
            {
                // Auto-select if only one account
                SelectedAccountId = AvailableAccounts[0].AccountId;
                SelectedCustomerId = AvailableAccounts[0].CustomerId;
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
            Connection = await _bingAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _bingAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _bingAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Bing Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/bingads";
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
