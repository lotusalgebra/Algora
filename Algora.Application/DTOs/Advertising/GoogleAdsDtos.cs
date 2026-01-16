namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Google Ads connection settings stored per shop.
/// </summary>
public record GoogleAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? CustomerId,
    string? CustomerName,
    string? ManagerAccountId,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Google Ads connection.
/// </summary>
public record SaveGoogleAdsConnectionDto(
    string RefreshToken,
    string CustomerId,
    string? CustomerName = null,
    string? ManagerAccountId = null
);

/// <summary>
/// Campaign data from Google Ads API.
/// </summary>
public record GoogleAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string Status,
    string CampaignType,
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
/// Ad Group data from Google Ads API.
/// </summary>
public record GoogleAdsAdGroupDto(
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
/// Daily insights from Google Ads API.
/// </summary>
public record GoogleAdsDailyInsightDto(
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
/// Summary of Google Ads performance.
/// </summary>
public record GoogleAdsSummaryDto(
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
/// Google Ads customer account info.
/// </summary>
public record GoogleAdsCustomerDto(
    string Id,
    string DescriptiveName,
    string CurrencyCode,
    string? ManagerCustomerId,
    bool IsManager
);

/// <summary>
/// Sync result information.
/// </summary>
public record GoogleAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from Google.
/// </summary>
public record GoogleOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType
);
