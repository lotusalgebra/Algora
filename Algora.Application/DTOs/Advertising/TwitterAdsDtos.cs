namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Twitter Ads connection settings stored per shop.
/// </summary>
public record TwitterAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AdAccountId,
    string? AdAccountName,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Twitter Ads connection.
/// </summary>
public record SaveTwitterAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string AdAccountId,
    string? AdAccountName = null
);

/// <summary>
/// Campaign data from Twitter Ads API.
/// </summary>
public record TwitterAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string Status,
    string Objective,
    string FundingInstrumentId,
    decimal Spend,
    long Impressions,
    long Clicks,
    long Engagements,
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
/// Line Item (Ad Group) data from Twitter Ads API.
/// </summary>
public record TwitterAdsLineItemDto(
    string LineItemId,
    string LineItemName,
    string CampaignId,
    string Status,
    string BidType,
    decimal? BidAmount,
    decimal Spend,
    long Impressions,
    long Clicks,
    long Engagements
);

/// <summary>
/// Daily analytics from Twitter Ads API.
/// </summary>
public record TwitterAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    decimal Spend,
    long Impressions,
    long Clicks,
    long Engagements,
    int Conversions,
    decimal? ConversionValue,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Summary of Twitter Ads performance.
/// </summary>
public record TwitterAdsSummaryDto(
    decimal TotalSpend,
    decimal TotalConversionValue,
    long TotalImpressions,
    long TotalClicks,
    long TotalEngagements,
    int TotalConversions,
    decimal Ctr,
    decimal Cpc,
    decimal? Cpa,
    decimal Roas,
    int ActiveCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Twitter Ads ad account info.
/// </summary>
public record TwitterAdsAccountDto(
    string Id,
    string Name,
    string Currency,
    string Timezone,
    string Status,
    bool Deleted
);

/// <summary>
/// Sync result information.
/// </summary>
public record TwitterAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth 2.0 token response from Twitter.
/// </summary>
public record TwitterOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string Scope,
    string TokenType
);
