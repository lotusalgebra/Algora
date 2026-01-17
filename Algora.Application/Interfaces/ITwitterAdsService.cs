using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Twitter Ads integration.
/// </summary>
public interface ITwitterAdsService
{
    /// <summary>
    /// Generate the OAuth 2.0 authorization URL for Twitter Ads.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);

    /// <summary>
    /// Exchange authorization code for access and refresh tokens.
    /// </summary>
    Task<TwitterOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, string codeVerifier);

    /// <summary>
    /// Get connection settings for a shop.
    /// </summary>
    Task<TwitterAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Save connection settings and tokens.
    /// </summary>
    Task SaveConnectionAsync(string shopDomain, SaveTwitterAdsConnectionDto dto);

    /// <summary>
    /// Disconnect Twitter Ads from a shop.
    /// </summary>
    Task DisconnectAsync(string shopDomain);

    /// <summary>
    /// Get available ad accounts for the authenticated user.
    /// </summary>
    Task<List<TwitterAdsAccountDto>> GetAdAccountsAsync(string accessToken);

    /// <summary>
    /// Sync campaign data from Twitter Ads to local database.
    /// </summary>
    Task<TwitterAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get campaign performance data for a date range.
    /// </summary>
    Task<List<TwitterAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get line items (ad groups) for a campaign.
    /// </summary>
    Task<List<TwitterAdsLineItemDto>> GetLineItemsAsync(string shopDomain, string campaignId);

    /// <summary>
    /// Get aggregated summary of Twitter Ads performance.
    /// </summary>
    Task<TwitterAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Refresh OAuth tokens if expired.
    /// </summary>
    Task<bool> RefreshTokensAsync(string shopDomain);
}
