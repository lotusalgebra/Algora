namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Microsoft Advertising (Bing Ads) connection settings stored per shop.
/// </summary>
public record BingAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AccountId,
    string? AccountName,
    string? CustomerId,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Bing Ads connection.
/// </summary>
public record SaveBingAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string AccountId,
    string? AccountName = null,
    string? CustomerId = null
);

/// <summary>
/// Campaign data from Microsoft Advertising API.
/// </summary>
public record BingAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string CampaignType,  // Search, Shopping, Audience, DynamicSearchAds
    string Status,
    decimal Budget,
    string BudgetType,    // DailyBudgetStandard, DailyBudgetAccelerated
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
/// Ad Group data from Microsoft Advertising API.
/// </summary>
public record BingAdsAdGroupDto(
    string AdGroupId,
    string AdGroupName,
    string CampaignId,
    string Status,
    decimal? CpcBid,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Conversions,
    decimal? ConversionValue
);

/// <summary>
/// Daily performance data from Microsoft Advertising API.
/// </summary>
public record BingAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Conversions,
    decimal? ConversionValue,
    decimal Ctr,
    decimal Cpc,
    decimal AveragePosition
);

/// <summary>
/// Summary of Bing Ads performance.
/// </summary>
public record BingAdsSummaryDto(
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
    int SearchCampaigns,
    int ShoppingCampaigns,
    int AudienceCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Microsoft Advertising account info.
/// </summary>
public record BingAdsAccountDto(
    string AccountId,
    string AccountName,
    string AccountNumber,
    string CustomerId,
    string Currency,
    string TimeZone,
    string Status
);

/// <summary>
/// Sync result information.
/// </summary>
public record BingAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from Microsoft identity platform.
/// </summary>
public record BingOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    string Scope
);
