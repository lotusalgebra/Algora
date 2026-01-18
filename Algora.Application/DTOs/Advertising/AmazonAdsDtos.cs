namespace Algora.Application.DTOs.Advertising;

/// <summary>
/// Amazon Ads connection settings stored per shop.
/// </summary>
public record AmazonAdsConnectionDto(
    int Id,
    string ShopDomain,
    string? ProfileId,
    string? ProfileName,
    string? MarketplaceId,
    string? CountryCode,
    bool IsConnected,
    DateTime? ConnectedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError
);

/// <summary>
/// Input for saving Amazon Ads connection.
/// </summary>
public record SaveAmazonAdsConnectionDto(
    string AccessToken,
    string RefreshToken,
    string ProfileId,
    string? ProfileName = null,
    string? MarketplaceId = null,
    string? CountryCode = null
);

/// <summary>
/// Campaign data from Amazon Advertising API.
/// Covers Sponsored Products, Sponsored Brands, and Sponsored Display.
/// </summary>
public record AmazonAdsCampaignDto(
    string CampaignId,
    string CampaignName,
    string CampaignType,  // SPONSORED_PRODUCTS, SPONSORED_BRANDS, SPONSORED_DISPLAY
    string Status,
    string TargetingType, // MANUAL, AUTO
    decimal Budget,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Orders,
    decimal Sales,
    decimal Acos,         // Advertising Cost of Sales
    decimal Roas,
    decimal Ctr,
    decimal Cpc,
    DateTime DateStart,
    DateTime DateEnd
);

/// <summary>
/// Ad Group data from Amazon Advertising API.
/// </summary>
public record AmazonAdsAdGroupDto(
    string AdGroupId,
    string AdGroupName,
    string CampaignId,
    string Status,
    decimal DefaultBid,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Orders,
    decimal Sales
);

/// <summary>
/// Daily performance data from Amazon Advertising API.
/// </summary>
public record AmazonAdsDailyInsightDto(
    DateTime Date,
    string? CampaignId,
    string? CampaignName,
    string? CampaignType,
    decimal Spend,
    long Impressions,
    long Clicks,
    int Orders,
    decimal Sales,
    decimal Acos,
    decimal Ctr,
    decimal Cpc
);

/// <summary>
/// Summary of Amazon Ads performance.
/// </summary>
public record AmazonAdsSummaryDto(
    decimal TotalSpend,
    decimal TotalSales,
    long TotalImpressions,
    long TotalClicks,
    int TotalOrders,
    decimal Acos,
    decimal Roas,
    decimal Ctr,
    decimal Cpc,
    int ActiveCampaigns,
    int SponsoredProductsCampaigns,
    int SponsoredBrandsCampaigns,
    int SponsoredDisplayCampaigns,
    DateTime? LastSyncedAt
);

/// <summary>
/// Amazon Advertising profile info.
/// A profile represents an advertiser account in a specific marketplace.
/// </summary>
public record AmazonAdsProfileDto(
    string ProfileId,
    string CountryCode,
    string? MarketplaceId,
    string? AccountName,
    string AccountType,  // seller, vendor, agency
    string Timezone,
    string Currency
);

/// <summary>
/// Sync result information.
/// </summary>
public record AmazonAdsSyncResultDto(
    bool Success,
    int CampaignsProcessed,
    int RecordsCreated,
    int RecordsUpdated,
    string? ErrorMessage,
    DateTime SyncedAt
);

/// <summary>
/// OAuth token response from Login with Amazon (LWA).
/// </summary>
public record AmazonOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType
);
