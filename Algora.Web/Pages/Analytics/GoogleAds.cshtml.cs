using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class GoogleAdsModel : PageModel
{
    private readonly IGoogleAdsService _googleAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAdsModel> _logger;

    public GoogleAdsModel(
        IGoogleAdsService googleAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<GoogleAdsModel> logger)
    {
        _googleAdsService = googleAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public GoogleAdsConnectionDto? Connection { get; set; }
    public GoogleAdsSummaryDto? Summary { get; set; }
    public List<GoogleAdsCampaignDto> Campaigns { get; set; } = new();
    public List<GoogleAdsCustomerDto> AvailableAccounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "30days";

    [BindProperty]
    public string? SelectedCustomerId { get; set; }

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
        HttpContext.Session.SetString("GoogleAdsOAuthState", state);

        OAuthUrl = _googleAdsService.GetOAuthUrl(redirectUri, state);
        return Redirect(OAuthUrl);
    }

    public async Task<IActionResult> OnPostSelectAccountAsync()
    {
        if (string.IsNullOrEmpty(RefreshToken) || string.IsNullOrEmpty(SelectedCustomerId))
        {
            ErrorMessage = "Please select a customer account.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Get account details
            var accounts = await _googleAdsService.GetAccessibleCustomersAsync(RefreshToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.Id == SelectedCustomerId);

            var dto = new SaveGoogleAdsConnectionDto(
                RefreshToken,
                SelectedCustomerId,
                selectedAccount?.DescriptiveName,
                selectedAccount?.ManagerCustomerId
            );

            await _googleAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _googleAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Google Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Google Ads connection");
            ErrorMessage = "Failed to connect Google Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _googleAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Google Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Google Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _googleAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync Google Ads");
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
            var expectedState = HttpContext.Session.GetString("GoogleAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _googleAdsService.ExchangeCodeAsync(Code!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                ErrorMessage = "Failed to get refresh token from Google. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available customer accounts
            AvailableAccounts = await _googleAdsService.GetAccessibleCustomersAsync(tokenResponse.RefreshToken);
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No Google Ads accounts found. Please make sure you have access to a Google Ads account.";
            }
            else if (AvailableAccounts.Count == 1)
            {
                // Auto-select if only one account
                SelectedCustomerId = AvailableAccounts[0].Id;
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
            Connection = await _googleAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _googleAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _googleAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }

            // Generate OAuth URL for connect button
            var redirectUri = GetRedirectUri();
            var state = Guid.NewGuid().ToString("N");
            OAuthUrl = _googleAdsService.GetOAuthUrl(redirectUri, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Google Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/googleads";
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
