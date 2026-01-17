namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Pinterest Ads connection settings stored per shop.
/// </summary>
public record PinterestAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AdAccountId,
    string? AdAccountName,
    string? BusinessId,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Pinterest Ads connection.
/// </summary>
public record SavePinterestAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string AdAccountId,
    string? AdAccountName = null,
    string? BusinessId = null
);

/// <summary>
/// Campaign data from Pinterest Ads API.
/// </summary>
public record PinterestAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string Status,
    string ObjectiveType,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Conversions,
    decimal? ConversionValue,
    decimal Ctr,
    decimal Cpc,
    decimal? Cpa,
    decimal? Roas,
    DateTime DateStart,
    DateTime DateEnd
);

/// <summary>
/// Ad Group data from Pinterest Ads API.
/// </summary>
public record PinterestAdsAdGroupDto(
    string AdGroupId,
    string AdGroupName,
    string CampaignId,
    string CampaignName,
    string Status,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Conversions,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Daily insights from Pinterest Ads API.
/// </summary>
public record PinterestAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Conversions,
    decimal? ConversionValue,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Summary of Pinterest Ads performance.
/// </summary>
public record PinterestAdsSummaryDto(
    decimal TotalSpend,
    decimal TotalConversionValue,
    long TotalImpressions,
    long TotalClicks,
    int TotalConversions,
    decimal Ctr,
    decimal Cpc,
    decimal? Cpa,
    decimal Roas,
    int ActiveCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Pinterest Ads ad account info.
/// </summary>
public record PinterestAdsAccountDto(
    string Id,
    string Name,
    string Currency,
    string? BusinessId,
    string Country
);

/// <summary>
/// Sync result information.
/// </summary>
public record PinterestAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from Pinterest.
/// </summary>
public record PinterestOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    string Scope
);
