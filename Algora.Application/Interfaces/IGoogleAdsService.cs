using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with Google Ads API.
/// </summary>
public interface IGoogleAdsService
{
    /// <summary>
    /// Gets the current Google Ads connection for a shop.
    /// </summary>
    Task<GoogleAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves Google Ads connection credentials after OAuth.
    /// </summary>
    Task<GoogleAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveGoogleAdsConnectionDto dto);

    /// <summary>
    /// Disconnects Google Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to Google Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets accessible customer accounts for the connected user.
    /// </summary>
    Task<List<GoogleAdsCustomerDto>> GetAccessibleCustomersAsync(string refreshToken);

    /// <summary>
    /// Syncs campaign data from Google Ads API to local database.
    /// </summary>
    Task<GoogleAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<GoogleAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets ad group performance data.
    /// </summary>
    Task<List<GoogleAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<GoogleAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of Google Ads performance.
    /// </summary>
    Task<GoogleAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges authorization code for tokens.
    /// </summary>
    Task<GoogleOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<string?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Generates the OAuth URL for Google Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
