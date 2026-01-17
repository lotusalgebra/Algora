namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// LinkedIn Ads connection settings stored per shop.
/// </summary>
public record LinkedInAdsConnectionDto(
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
/// Input for saving LinkedIn Ads connection.
/// </summary>
public record SaveLinkedInAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string AdAccountId,
    string? AdAccountName = null,
    string? OrganizationId = null
);

/// <summary>
/// Campaign data from LinkedIn Ads API.
/// </summary>
public record LinkedInAdsCampaignDto(
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
/// Campaign Group data from LinkedIn Ads API.
/// </summary>
public record LinkedInAdsCampaignGroupDto(
    string CampaignGroupId,
    string CampaignGroupName,
    string Status,
    decimal TotalBudget,
    int CampaignCount,
    decimal Spend,
    long Impressions,
    long Clicks
);

/// <summary>
/// Daily insights from LinkedIn Ads API.
/// </summary>
public record LinkedInAdsDailyInsightDto(
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
/// Summary of LinkedIn Ads performance.
/// </summary>
public record LinkedInAdsSummaryDto(
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
/// LinkedIn Ads ad account info.
/// </summary>
public record LinkedInAdsAccountDto(
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
public record LinkedInAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from LinkedIn.
/// </summary>
public record LinkedInOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    int RefreshTokenExpiresIn,
    string Scope
);
