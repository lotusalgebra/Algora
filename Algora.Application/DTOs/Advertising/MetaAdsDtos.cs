namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Meta Ads connection settings stored per shop.
/// </summary>
public record MetaAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? AdAccountId,
    string? AdAccountName,
    string? BusinessName,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Meta Ads connection.
/// </summary>
public record SaveMetaAdsConnectionDto(
    string AccessToken,
    string AdAccountId,
    string? AdAccountName = null,
    string? BusinessName = null
);

/// <summary>
/// Campaign data from Meta Ads API.
/// </summary>
public record MetaAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string Status,
    string Objective,
    decimal Spend,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal? Revenue,
    decimal Ctr,
    decimal Cpc,
    decimal? Cpa,
    decimal? Roas,
    DateTime DateStart,
    DateTime DateEnd
);

/// <summary>
/// Ad Set data from Meta Ads API.
/// </summary>
public record MetaAdsAdSetDto(
    string AdSetId,
    string AdSetName,
    string CampaignId,
    string CampaignName,
    string Status,
    decimal Spend,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Daily insights from Meta Ads API.
/// </summary>
public record MetaAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    decimal Spend,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal? Revenue,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Summary of Meta Ads performance.
/// </summary>
public record MetaAdsSummaryDto(
    decimal TotalSpend,
    decimal TotalRevenue,
    int TotalImpressions,
    int TotalClicks,
    int TotalConversions,
    decimal Ctr,
    decimal Cpc,
    decimal? Cpa,
    decimal Roas,
    int ActiveCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Ad Account info from Meta API.
/// </summary>
public record MetaAdAccountDto(
    string Id,
    string Name,
    string AccountStatus,
    string Currency,
    string? BusinessName
);

/// <summary>
/// Sync result information.
/// </summary>
public record MetaAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);
