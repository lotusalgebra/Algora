using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with LinkedIn Marketing API.
/// </summary>
public interface ILinkedInAdsService
{
    /// <summary>
    /// Gets the current LinkedIn Ads connection for a shop.
    /// </summary>
    Task<LinkedInAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves LinkedIn Ads connection credentials after OAuth.
    /// </summary>
    Task<LinkedInAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveLinkedInAdsConnectionDto dto);

    /// <summary>
    /// Disconnects LinkedIn Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to LinkedIn Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets accessible ad accounts for the connected user.
    /// </summary>
    Task<List<LinkedInAdsAccountDto>> GetAdAccountsAsync(string accessToken);

    /// <summary>
    /// Syncs campaign data from LinkedIn Ads API to local database.
    /// </summary>
    Task<LinkedInAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<LinkedInAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets campaign group data.
    /// </summary>
    Task<List<LinkedInAdsCampaignGroupDto>> GetCampaignGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<LinkedInAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of LinkedIn Ads performance.
    /// </summary>
    Task<LinkedInAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges authorization code for tokens.
    /// </summary>
    Task<LinkedInOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<LinkedInOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Generates the OAuth URL for LinkedIn Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
