using Algora.Application.DTOs.Advertising;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for integrating with Meta (Facebook/Instagram) Ads API.
/// </summary>
public interface IMetaAdsService
{
    /// <summary>
    /// Gets the current Meta Ads connection for a shop.
    /// </summary>
    Task<MetaAdsConnectionDto?> GetConnectionAsync(string shopDomain);

    /// <summary>
    /// Saves Meta Ads connection credentials after OAuth.
    /// </summary>
    Task<MetaAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveMetaAdsConnectionDto dto);

    /// <summary>
    /// Disconnects Meta Ads integration.
    /// </summary>
    Task<bool> DisconnectAsync(string shopDomain);

    /// <summary>
    /// Tests the connection to Meta Ads API.
    /// </summary>
    Task<bool> TestConnectionAsync(string shopDomain);

    /// <summary>
    /// Gets available ad accounts for the connected user.
    /// </summary>
    Task<List<MetaAdAccountDto>> GetAdAccountsAsync(string accessToken);

    /// <summary>
    /// Syncs campaign data from Meta Ads API to local database.
    /// </summary>
    Task<MetaAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets campaign performance data.
    /// </summary>
    Task<List<MetaAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets ad set performance data.
    /// </summary>
    Task<List<MetaAdsAdSetDto>> GetAdSetsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily insights data.
    /// </summary>
    Task<List<MetaAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets summary of Meta Ads performance.
    /// </summary>
    Task<MetaAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Exchanges short-lived token for long-lived token.
    /// </summary>
    Task<string?> ExchangeTokenAsync(string shortLivedToken);

    /// <summary>
    /// Generates the OAuth URL for Meta Ads login.
    /// </summary>
    string GetOAuthUrl(string redirectUri, string state);
}
