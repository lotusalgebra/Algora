using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Microsoft Advertising (Bing Ads) integration.
/// </summary>
public interface IBingAdsService
{
    /// <summary>
    /// Generate the Microsoft identity platform OAuth authorization URL.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);

    /// <summary>
    /// Exchange authorization code for access and refresh tokens.
    /// </summary>
    Task<BingOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Get connection settings for a shop.
    /// </summary>
    Task<BingAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Save connection settings and tokens.
    /// </summary>
    Task SaveConnectionAsync(string shopDomain, SaveBingAdsConnectionDto dto);

    /// <summary>
    /// Disconnect Bing Ads from a shop.
    /// </summary>
    Task DisconnectAsync(string shopDomain);

    /// <summary>
    /// Get available advertising accounts for the authenticated user.
    /// </summary>
    Task<List<BingAdsAccountDto>> GetAccountsAsync(string accessToken);

    /// <summary>
    /// Sync campaign data from Microsoft Advertising to local database.
    /// </summary>
    Task<BingAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get campaign performance data for a date range.
    /// </summary>
    Task<List<BingAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get ad groups for a campaign.
    /// </summary>
    Task<List<BingAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, string campaignId);

    /// <summary>
    /// Get aggregated summary of Bing Ads performance.
    /// </summary>
    Task<BingAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Refresh OAuth tokens if expired.
    /// </summary>
    Task<bool> RefreshTokensAsync(string shopDomain);
}
