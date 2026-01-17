using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with Snapchat Ads API.
/// </summary>
public interface ISnapchatAdsService
{
    /// <summary>
    /// Gets the current Snapchat Ads connection for a shop.
    /// </summary>
    Task<SnapchatAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves Snapchat Ads connection credentials after OAuth.
    /// </summary>
    Task<SnapchatAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveSnapchatAdsConnectionDto dto);

    /// <summary>
    /// Disconnects Snapchat Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to Snapchat Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets accessible ad accounts for the connected user.
    /// </summary>
    Task<List<SnapchatAdsAccountDto>> GetAdAccountsAsync(string accessToken);

    /// <summary>
    /// Syncs campaign data from Snapchat Ads API to local database.
    /// </summary>
    Task<SnapchatAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<SnapchatAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets ad squad performance data.
    /// </summary>
    Task<List<SnapchatAdsAdSquadDto>> GetAdSquadsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<SnapchatAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of Snapchat Ads performance.
    /// </summary>
    Task<SnapchatAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges authorization code for tokens.
    /// </summary>
    Task<SnapchatOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<SnapchatOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Generates the OAuth URL for Snapchat Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
