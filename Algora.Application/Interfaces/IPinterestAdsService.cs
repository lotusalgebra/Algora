using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with Pinterest Ads API.
/// </summary>
public interface IPinterestAdsService
{
    /// <summary>
    /// Gets the current Pinterest Ads connection for a shop.
    /// </summary>
    Task<PinterestAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves Pinterest Ads connection credentials after OAuth.
    /// </summary>
    Task<PinterestAdsConnectionDto> SaveConnectionAsync(string shopDomain, SavePinterestAdsConnectionDto dto);

    /// <summary>
    /// Disconnects Pinterest Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to Pinterest Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets accessible ad accounts for the connected user.
    /// </summary>
    Task<List<PinterestAdsAccountDto>> GetAdAccountsAsync(string accessToken);

    /// <summary>
    /// Syncs campaign data from Pinterest Ads API to local database.
    /// </summary>
    Task<PinterestAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<PinterestAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets ad group performance data.
    /// </summary>
    Task<List<PinterestAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<PinterestAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of Pinterest Ads performance.
    /// </summary>
    Task<PinterestAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges authorization code for tokens.
    /// </summary>
    Task<PinterestOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<PinterestOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Generates the OAuth URL for Pinterest Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
