using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Analytics;

[Authorize]
public class TwitterAdsModel : PageModel
{
    private readonly ITwitterAdsService _twitterAdsService;
    private readonly IShopContext _shopContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwitterAdsModel> _logger;

    public TwitterAdsModel(
        ITwitterAdsService twitterAdsService,
        IShopContext shopContext,
        IConfiguration configuration,
        ILogger<TwitterAdsModel> logger)
    {
        _twitterAdsService = twitterAdsService;
        _shopContext = shopContext;
        _configuration = configuration;
        _logger = logger;
    }

    public TwitterAdsConnectionDto? Connection { get; set; }
    public TwitterAdsSummaryDto? Summary { get; set; }
    public List<TwitterAdsCampaignDto> Campaigns { get; set; } = new();
    public List<TwitterAdsAccountDto> AvailableAccounts { get; set; } = new();

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

    // PKCE code verifier stored in session
    private const string CodeVerifierSessionKey = "TwitterAdsCodeVerifier";

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
        // Generate PKCE code verifier
        var codeVerifier = GenerateCodeVerifier();
        HttpContext.Session.SetString(CodeVerifierSessionKey, codeVerifier);

        // Generate OAuth URL and redirect
        var redirectUri = GetRedirectUri();
        var state = Guid.NewGuid().ToString("N");

        // Store state in session for validation
        HttpContext.Session.SetString("TwitterAdsOAuthState", state);

        // Generate code challenge from verifier
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var clientId = _configuration["Twitter:ClientId"];
        var scopes = "ads.read ads.write offline.access";

        OAuthUrl = $"https://twitter.com/i/oauth2/authorize?" +
                   $"response_type=code&" +
                   $"client_id={Uri.EscapeDataString(clientId ?? "")}&" +
                   $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                   $"scope={Uri.EscapeDataString(scopes)}&" +
                   $"state={Uri.EscapeDataString(state)}&" +
                   $"code_challenge={codeChallenge}&" +
                   $"code_challenge_method=S256";

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
            var accounts = await _twitterAdsService.GetAdAccountsAsync(AccessToken);
            var selectedAccount = accounts.FirstOrDefault(a => a.Id == SelectedAdAccountId);

            var dto = new SaveTwitterAdsConnectionDto(
                AccessToken,
                RefreshToken ?? string.Empty,
                SelectedAdAccountId,
                selectedAccount?.Name
            );

            await _twitterAdsService.SaveConnectionAsync(_shopContext.ShopDomain, dto);

            // Trigger initial sync
            await _twitterAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

            SuccessMessage = "Twitter Ads connected successfully! Initial data sync complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Twitter Ads connection");
            ErrorMessage = "Failed to connect Twitter Ads: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        try
        {
            await _twitterAdsService.DisconnectAsync(_shopContext.ShopDomain);
            SuccessMessage = "Twitter Ads disconnected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Twitter Ads");
            ErrorMessage = "Failed to disconnect: " + ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        try
        {
            var result = await _twitterAdsService.SyncCampaignsAsync(_shopContext.ShopDomain);

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
            _logger.LogError(ex, "Failed to sync Twitter Ads");
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
            var expectedState = HttpContext.Session.GetString("TwitterAdsOAuthState");
            if (State != expectedState)
            {
                ErrorMessage = "Invalid OAuth state. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get code verifier from session
            var codeVerifier = HttpContext.Session.GetString(CodeVerifierSessionKey);
            if (string.IsNullOrEmpty(codeVerifier))
            {
                ErrorMessage = "OAuth session expired. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Exchange code for tokens
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _twitterAdsService.ExchangeCodeAsync(Code!, redirectUri, codeVerifier);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErrorMessage = "Failed to get access token from Twitter. Please try again.";
                await LoadDataAsync();
                return Page();
            }

            // Get available ad accounts
            AvailableAccounts = await _twitterAdsService.GetAdAccountsAsync(tokenResponse.AccessToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            if (AvailableAccounts.Count == 0)
            {
                ErrorMessage = "No ad accounts found. Please make sure you have access to a Twitter Ads account.";
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
            Connection = await _twitterAdsService.GetConnectionAsync(_shopContext.ShopDomain);

            if (Connection?.IsConnected == true)
            {
                var (startDate, endDate) = GetDateRange();
                Summary = await _twitterAdsService.GetSummaryAsync(_shopContext.ShopDomain, startDate, endDate);
                Campaigns = await _twitterAdsService.GetCampaignsAsync(_shopContext.ShopDomain, startDate, endDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Twitter Ads data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
    }

    private string GetRedirectUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/analytics/twitterads";
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

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
