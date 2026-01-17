namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// TikTok Ads connection settings stored per shop.
/// </summary>
public record TikTokAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AdvertiserId,
    string? AdvertiserName,
    string? BusinessCenterId,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving TikTok Ads connection.
/// </summary>
public record SaveTikTokAdsConnectionDto(
    string AccessToken,
    string AdvertiserId,
    string? AdvertiserName = null,
    string? BusinessCenterId = null
);

/// <summary>
/// Campaign data from TikTok Ads API.
/// </summary>
public record TikTokAdsCampaignDto(
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
/// Ad Group data from TikTok Ads API.
/// </summary>
public record TikTokAdsAdGroupDto(
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
/// Daily insights from TikTok Ads API.
/// </summary>
public record TikTokAdsDailyInsightDto(
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
/// Summary of TikTok Ads performance.
/// </summary>
public record TikTokAdsSummaryDto(
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
/// TikTok Ads advertiser account info.
/// </summary>
public record TikTokAdsAdvertiserDto(
    string AdvertiserId,
    string AdvertiserName,
    string Status,
    string Currency,
    string Timezone,
    string? BusinessCenterId
);

/// <summary>
/// Sync result information.
/// </summary>
public record TikTokAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from TikTok.
/// </summary>
public record TikTokOAuthTokenResponse(
    string AccessToken,
    long ExpiresIn,
    string TokenType,
    string? RefreshToken,
    long? RefreshTokenExpiresIn
);
