using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with TikTok Ads API.
/// </summary>
public interface ITikTokAdsService
{
    /// <summary>
    /// Gets the current TikTok Ads connection for a shop.
    /// </summary>
    Task<TikTokAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves TikTok Ads connection credentials after OAuth.
    /// </summary>
    Task<TikTokAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveTikTokAdsConnectionDto dto);

    /// <summary>
    /// Disconnects TikTok Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to TikTok Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets accessible advertiser accounts for the connected user.
    /// </summary>
    Task<List<TikTokAdsAdvertiserDto>> GetAdvertisersAsync(string accessToken);

    /// <summary>
    /// Syncs campaign data from TikTok Ads API to local database.
    /// </summary>
    Task<TikTokAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<TikTokAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets ad group performance data.
    /// </summary>
    Task<List<TikTokAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<TikTokAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of TikTok Ads performance.
    /// </summary>
    Task<TikTokAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges authorization code for tokens.
    /// </summary>
    Task<TikTokOAuthTokenResponse?> ExchangeCodeAsync(string authCode, string redirectUri);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<TikTokOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Generates the OAuth URL for TikTok Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
