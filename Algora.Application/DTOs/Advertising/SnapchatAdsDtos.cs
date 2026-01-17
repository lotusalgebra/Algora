namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Snapchat Ads connection settings stored per shop.
/// </summary>
public record SnapchatAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AdAccountId,
    string? AdAccountName,
    string? OrganizationId,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Snapchat Ads connection.
/// </summary>
public record SaveSnapchatAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string AdAccountId,
    string? AdAccountName = null,
    string? OrganizationId = null
);

/// <summary>
/// Campaign data from Snapchat Ads API.
/// </summary>
public record SnapchatAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string Status,
    string ObjectiveType,
    decimal Spend,
    long Impressions,
    long Swipes,
    int Conversions,
    decimal? ConversionValue,
    decimal SwipeRate,
    decimal CostPerSwipe,
    decimal? Cpa,
    decimal? Roas,
    DateTime DateStart,
    DateTime DateEnd
);

/// <summary>
/// Ad Squad (Ad Group) data from Snapchat Ads API.
/// </summary>
public record SnapchatAdsAdSquadDto(
    string AdSquadId,
    string AdSquadName,
    string CampaignId,
    string CampaignName,
    string Status,
    decimal Spend,
    long Impressions,
    long Swipes,
    int Conversions,
    decimal SwipeRate,
    decimal CostPerSwipe
);

/// <summary>
/// Daily insights from Snapchat Ads API.
/// </summary>
public record SnapchatAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    decimal Spend,
    long Impressions,
    long Swipes,
    int Conversions,
    decimal? ConversionValue,
    decimal SwipeRate,
    decimal CostPerSwipe
);

/// <summary>
/// Summary of Snapchat Ads performance.
/// </summary>
public record SnapchatAdsSummaryDto(
    decimal TotalSpend,
    decimal TotalConversionValue,
    long TotalImpressions,
    long TotalSwipes,
    int TotalConversions,
    decimal SwipeRate,
    decimal CostPerSwipe,
    decimal? Cpa,
    decimal Roas,
    int ActiveCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Snapchat Ads ad account info.
/// </summary>
public record SnapchatAdsAccountDto(
    string Id,
    string Name,
    string Currency,
    string? OrganizationId,
    string Status,
    string Type
);

/// <summary>
/// Sync result information.
/// </summary>
public record SnapchatAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from Snapchat.
/// </summary>
public record SnapchatOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    string Scope
);
