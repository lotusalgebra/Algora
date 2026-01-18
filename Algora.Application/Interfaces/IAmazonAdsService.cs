using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Amazon Advertising integration.
/// Supports Sponsored Products, Sponsored Brands, and Sponsored Display campaigns.
/// </summary>
public interface IAmazonAdsService
{
    /// <summary>
    /// Generate the Login with Amazon (LWA) authorization URL.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);

    /// <summary>
    /// Exchange authorization code for access and refresh tokens.
    /// </summary>
    Task<AmazonOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Get connection settings for a shop.
    /// </summary>
    Task<AmazonAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Save connection settings and tokens.
    /// </summary>
    Task SaveConnectionAsync(string shopDomain, SaveAmazonAdsConnectionDto dto);

    /// <summary>
    /// Disconnect Amazon Ads from a shop.
    /// </summary>
    Task DisconnectAsync(string shopDomain);

    /// <summary>
    /// Get available advertising profiles for the authenticated user.
    /// </summary>
    Task<List<AmazonAdsProfileDto>> GetProfilesAsync(string accessToken);

    /// <summary>
    /// Sync campaign data from Amazon Advertising to local database.
    /// </summary>
    Task<AmazonAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get campaign performance data for a date range.
    /// </summary>
    Task<List<AmazonAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get ad groups for a campaign.
    /// </summary>
    Task<List<AmazonAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, string campaignId);

    /// <summary>
    /// Get aggregated summary of Amazon Ads performance.
    /// </summary>
    Task<AmazonAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Refresh OAuth tokens if expired.
    /// </summary>
    Task<bool> RefreshTokensAsync(string shopDomain);
}
