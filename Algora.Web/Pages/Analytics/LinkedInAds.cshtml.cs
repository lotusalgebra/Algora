using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class LinkedInAdsModel : PageModel
{
    private readonly ILinkedInAdsService _linkedInAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LinkedInAdsModel> _logger;

    public LinkedInAdsModel(
        ILinkedInAdsService linkedInAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<LinkedInAdsModel> logger)
    {
        _linkedInAdsService = linkedInAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public LinkedInAdsConnectionDto? Connection { get; set; }
    public LinkedInAdsSummaryDto? Summary { get; set; }
    public List<LinkedInAdsCampaignDto> Campaigns { get; set; } = new();
    public List<LinkedInAdsAccountDto> AvailableAccounts { get; set; } = new();

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
        HttpContext.Session.SetString("LinkedInAdsOAuthState", state);

        OAuthUrl = _linkedInAdsService.GetOAuthUrl(redirectUri, state);
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
            var accounts = await _linkedInAdsService.GetAdAccountsAsync(AccessToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.Id == SelectedAdAccountId);

            var dto = new SaveLinkedInAdsConnectionDto(
                AccessToken,
                RefreshToken ?? string.Empty,
                SelectedAdAccountId,
                selectedAccount?.Name,
                selectedAccount?.OrganizationId
            );

            await _linkedInAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _linkedInAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "LinkedIn Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save LinkedIn Ads connection");
            ErrorMessage = "Failed to connect LinkedIn Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _linkedInAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "LinkedIn Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect LinkedIn Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _linkedInAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync LinkedIn Ads");
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
            var expectedState = HttpContext.Session.GetString("LinkedInAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _linkedInAdsService.ExchangeCodeAsync(Code!, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from LinkedIn. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available ad accounts
            AvailableAccounts = await _linkedInAdsService.GetAdAccountsAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No ad accounts found. Please make sure you have access to a LinkedIn Campaign Manager account.";
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
            Connection = await _linkedInAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _linkedInAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _linkedInAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }

            // Generate OAuth URL for connect button
            var redirectUri = GetRedirectUri();
            var state = Guid.NewGuid().ToString("N");
            OAuthUrl = _linkedInAdsService.GetOAuthUrl(redirectUri, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LinkedIn Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/linkedinads";
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
