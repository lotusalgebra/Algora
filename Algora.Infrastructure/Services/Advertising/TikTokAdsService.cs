using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.Advertising;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Advertising;

/// <summary>
/// Service for integrating with TikTok Marketing API.
/// </summary>
public class TikTokAdsService : ITikTokAdsService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TikTokAdsService> _logger;
    private readonly HttpClient _httpClient;

    private const string ApiBaseUrl = "https://business-api.tiktok.com/open_api/v1.3";
    private const string OAuthAuthUrl = "https://business-api.tiktok.com/portal/auth";
    private const string OAuthTokenUrl = "https://business-api.tiktok.com/open_api/v1.3/oauth2/access_token/";

    public TikTokAdsService(
        AppDbContext db,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<TikTokAdsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("TikTokAds");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<TikTokAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<TikTokAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new TikTokAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdvertiserId,
            connection.AdvertiserName,
            connection.BusinessCenterId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<TikTokAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveTikTokAdsConnectionDto dto)
    {
        var connection = await _db.Set<TikTokAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new TikTokAdsConnection { ShopDomain = shopDomain };
            _db.Set<TikTokAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryption.Encrypt(dto.AccessToken);
        connection.AdvertiserId = dto.AdvertiserId;
        connection.AdvertiserName = dto.AdvertiserName;
        connection.BusinessCenterId = dto.BusinessCenterId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.LastSyncError = null;
        connection.TokenExpiresAt = DateTime.UtcNow.AddDays(1); // TikTok tokens expire in 24 hours
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("TikTok Ads connected for shop {ShopDomain}, Advertiser ID: {AdvertiserId}",
            shopDomain, dto.AdvertiserId);

        return new TikTokAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdvertiserId,
            connection.AdvertiserName,
            connection.BusinessCenterId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<bool> DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<TikTokAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return false;

        connection.AccessToken = null;
        connection.RefreshToken = null;
        connection.IsConnected = false;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("TikTok Ads disconnected for shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestConnectionAsync(string shopDomain)
    {
        try
        {
            var connection = await _db.Set<TikTokAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null) return false;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            // Test by fetching advertiser info
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/advertiser/info/?advertiser_ids=[\"{connection.AdvertiserId}\"]");
            request.Headers.Add("Access-Token", accessToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok Ads connection test failed for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<TikTokAdsAdvertiserDto>> GetAdvertisersAsync(string accessToken)
    {
        var advertisers = new List<TikTokAdsAdvertiserDto>();

        try
        {
            var appId = _configuration["TikTokAds:AppId"] ?? "";
            var secret = _configuration["TikTokAds:Secret"] ?? "";

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/oauth2/advertiser/get/?app_id={appId}&secret={secret}&access_token={accessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to fetch advertisers: {Error}", error);
                return advertisers;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("list", out var list))
            {
                foreach (var item in list.EnumerateArray())
                {
                    var advertiserId = item.TryGetProperty("advertiser_id", out var id)
                        ? id.GetString() ?? "" : "";
                    var advertiserName = item.TryGetProperty("advertiser_name", out var name)
                        ? name.GetString() ?? "" : "";
                    var status = item.TryGetProperty("status", out var s)
                        ? s.GetString() ?? "" : "";

                    advertisers.Add(new TikTokAdsAdvertiserDto(
                        advertiserId,
                        advertiserName,
                        status,
                        "USD",
                        "UTC",
                        null
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TikTok advertisers");
        }

        return advertisers;
    }

    public async Task<TikTokAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<TikTokAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdvertiserId == null)
            {
                return new TikTokAdsSyncResultDto(false, 0, 0, 0, "TikTok Ads not connected", DateTime.UtcNow);
            }

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            // Fetch campaign insights from TikTok Ads API
            var insights = await FetchCampaignInsightsAsync(accessToken, connection.AdvertiserId, start, end);
            campaignsProcessed = insights.Count;

            foreach (var insight in insights)
            {
                // Check if record exists for this campaign and date
                var existing = await _db.AdsSpends
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "TikTok" &&
                        a.CampaignId == insight.CampaignId &&
                        a.SpendDate.Date == insight.DateStart.Date);

                if (existing != null)
                {
                    // Update existing record
                    existing.Amount = insight.Spend;
                    existing.Impressions = (int)insight.Impressions;
                    existing.Clicks = (int)insight.Clicks;
                    existing.Conversions = insight.Conversions;
                    existing.Revenue = insight.ConversionValue;
                    existing.UpdatedAt = DateTime.UtcNow;
                    recordsUpdated++;
                }
                else
                {
                    // Create new record
                    var adsSpend = new AdsSpend
                    {
                        ShopDomain = shopDomain,
                        Platform = "TikTok",
                        CampaignId = insight.CampaignId,
                        CampaignName = insight.CampaignName,
                        SpendDate = insight.DateStart,
                        Amount = insight.Spend,
                        Currency = connection.Currency,
                        Impressions = (int)insight.Impressions,
                        Clicks = (int)insight.Clicks,
                        Conversions = insight.Conversions,
                        Revenue = insight.ConversionValue,
                        Source = "api_sync",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.AdsSpends.Add(adsSpend);
                    recordsCreated++;
                }
            }

            await _db.SaveChangesAsync();

            // Update last sync time
            connection.LastSyncedAt = DateTime.UtcNow;
            connection.LastSyncError = null;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "TikTok Ads sync completed for {ShopDomain}: {Campaigns} campaigns, {Created} created, {Updated} updated",
                shopDomain, campaignsProcessed, recordsCreated, recordsUpdated);

            return new TikTokAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok Ads sync failed for {ShopDomain}", shopDomain);

            // Update error status
            var connection = await _db.Set<TikTokAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
            if (connection != null)
            {
                connection.LastSyncError = ex.Message;
                await _db.SaveChangesAsync();
            }

            return new TikTokAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, DateTime.UtcNow);
        }
    }

    public async Task<List<TikTokAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<TikTokAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection?.AccessToken == null || connection.AdvertiserId == null)
            return new List<TikTokAdsCampaignDto>();

        var accessToken = _encryption.Decrypt(connection.AccessToken);
        return await FetchCampaignInsightsAsync(accessToken, connection.AdvertiserId, startDate, endDate);
    }

    public async Task<List<TikTokAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var adGroups = new List<TikTokAdsAdGroupDto>();

        try
        {
            var connection = await _db.Set<TikTokAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdvertiserId == null)
                return adGroups;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            var requestBody = new
            {
                advertiser_id = connection.AdvertiserId,
                report_type = "BASIC",
                data_level = "AUCTION_ADGROUP",
                dimensions = new[] { "adgroup_id", "stat_time_day" },
                metrics = new[] { "spend", "impressions", "clicks", "conversion", "ctr", "cpc" },
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                page_size = 100
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/report/integrated/get/");
            request.Headers.Add("Access-Token", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return adGroups;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("list", out var list))
            {
                foreach (var item in list.EnumerateArray())
                {
                    var dimensions = item.TryGetProperty("dimensions", out var dims) ? dims : default;
                    var metrics = item.TryGetProperty("metrics", out var m) ? m : default;

                    var adGroupId = dimensions.TryGetProperty("adgroup_id", out var agId)
                        ? agId.GetString() ?? "" : "";
                    var spend = GetDecimalValue(metrics, "spend");
                    var impressions = GetLongValue(metrics, "impressions");
                    var clicks = GetLongValue(metrics, "clicks");
                    var conversions = (int)GetDecimalValue(metrics, "conversion");

                    adGroups.Add(new TikTokAdsAdGroupDto(
                        adGroupId,
                        "", // Name not returned in report
                        "",
                        "",
                        "ACTIVE",
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                        clicks > 0 ? spend / clicks : 0
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TikTok ad groups for {ShopDomain}", shopDomain);
        }

        return adGroups;
    }

    public async Task<List<TikTokAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<TikTokAdsDailyInsightDto>();

        try
        {
            var connection = await _db.Set<TikTokAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdvertiserId == null)
                return insights;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            var requestBody = new
            {
                advertiser_id = connection.AdvertiserId,
                report_type = "BASIC",
                data_level = "AUCTION_ADVERTISER",
                dimensions = new[] { "stat_time_day" },
                metrics = new[] { "spend", "impressions", "clicks", "conversion", "total_complete_payment_rate" },
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                page_size = 100
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/report/integrated/get/");
            request.Headers.Add("Access-Token", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return insights;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("list", out var list))
            {
                foreach (var item in list.EnumerateArray())
                {
                    var dimensions = item.TryGetProperty("dimensions", out var dims) ? dims : default;
                    var metrics = item.TryGetProperty("metrics", out var m) ? m : default;

                    var dateStr = dimensions.TryGetProperty("stat_time_day", out var d)
                        ? d.GetString() : null;
                    if (!DateTime.TryParse(dateStr, out var date)) continue;

                    var spend = GetDecimalValue(metrics, "spend");
                    var impressions = GetLongValue(metrics, "impressions");
                    var clicks = GetLongValue(metrics, "clicks");
                    var conversions = (int)GetDecimalValue(metrics, "conversion");

                    insights.Add(new TikTokAdsDailyInsightDto(
                        date,
                        null,
                        null,
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        null,
                        impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                        clicks > 0 ? spend / clicks : 0
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TikTok daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<TikTokAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<TikTokAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        // Get aggregated data from local database
        var adsData = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "TikTok" &&
                        a.SpendDate >= startDate &&
                        a.SpendDate <= endDate)
            .ToListAsync();

        var totalSpend = adsData.Sum(a => a.Amount);
        var totalRevenue = adsData.Sum(a => a.Revenue ?? 0);
        var totalImpressions = adsData.Sum(a => a.Impressions ?? 0);
        var totalClicks = adsData.Sum(a => a.Clicks ?? 0);
        var totalConversions = adsData.Sum(a => a.Conversions ?? 0);

        var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
        var cpc = totalClicks > 0 ? totalSpend / totalClicks : 0;
        var cpa = totalConversions > 0 ? totalSpend / totalConversions : (decimal?)null;
        var roas = totalSpend > 0 ? totalRevenue / totalSpend : 0;

        var activeCampaigns = adsData
            .Where(a => a.SpendDate >= DateTime.UtcNow.AddDays(-7))
            .Select(a => a.CampaignId)
            .Distinct()
            .Count();

        return new TikTokAdsSummaryDto(
            totalSpend,
            totalRevenue,
            totalImpressions,
            totalClicks,
            totalConversions,
            ctr,
            cpc,
            cpa,
            roas,
            activeCampaigns,
            connection?.LastSyncedAt
        );
    }

    public async Task<TikTokOAuthTokenResponse?> ExchangeCodeAsync(string authCode, string redirectUri)
    {
        try
        {
            var appId = _configuration["TikTokAds:AppId"] ?? "";
            var secret = _configuration["TikTokAds:Secret"] ?? "";

            var requestBody = new
            {
                app_id = appId,
                secret = secret,
                auth_code = authCode
            };

            var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to exchange code: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                return new TikTokOAuthTokenResponse(
                    data.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                    data.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 86400,
                    "Bearer",
                    data.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                    data.TryGetProperty("refresh_token_expires_in", out var rtei) ? rtei.GetInt64() : null
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange TikTok OAuth code");
            return null;
        }
    }

    public async Task<TikTokOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var appId = _configuration["TikTokAds:AppId"] ?? "";
            var secret = _configuration["TikTokAds:Secret"] ?? "";

            var requestBody = new
            {
                app_id = appId,
                secret = secret,
                refresh_token = refreshToken,
                grant_type = "refresh_token"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/oauth2/refresh_token/");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to refresh token: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                return new TikTokOAuthTokenResponse(
                    data.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                    data.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 86400,
                    "Bearer",
                    data.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                    data.TryGetProperty("refresh_token_expires_in", out var rtei) ? rtei.GetInt64() : null
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TikTok access token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var appId = _configuration["TikTokAds:AppId"] ?? "";

        return $"{OAuthAuthUrl}?app_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}";
    }

    #region Private Methods

    private async Task<List<TikTokAdsCampaignDto>> FetchCampaignInsightsAsync(
        string accessToken, string advertiserId, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<TikTokAdsCampaignDto>();

        try
        {
            var requestBody = new
            {
                advertiser_id = advertiserId,
                report_type = "BASIC",
                data_level = "AUCTION_CAMPAIGN",
                dimensions = new[] { "campaign_id", "stat_time_day" },
                metrics = new[] { "campaign_name", "spend", "impressions", "clicks", "conversion", "total_complete_payment_rate", "complete_payment_roas" },
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                page_size = 500
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/report/integrated/get/");
            request.Headers.Add("Access-Token", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("TikTok API error: {Error}", error);
                return campaigns;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("list", out var list))
            {
                // Group by campaign ID to aggregate daily metrics
                var campaignGroups = new Dictionary<string, List<JsonElement>>();

                foreach (var item in list.EnumerateArray())
                {
                    var dimensions = item.TryGetProperty("dimensions", out var dims) ? dims : default;
                    var campaignId = dimensions.TryGetProperty("campaign_id", out var cId)
                        ? cId.GetString() ?? "" : "";

                    if (!campaignGroups.ContainsKey(campaignId))
                        campaignGroups[campaignId] = new List<JsonElement>();

                    campaignGroups[campaignId].Add(item.Clone());
                }

                foreach (var group in campaignGroups)
                {
                    var firstItem = group.Value[0];
                    var metrics = firstItem.TryGetProperty("metrics", out var m) ? m : default;
                    var campaignName = metrics.TryGetProperty("campaign_name", out var cn)
                        ? cn.GetString() ?? "" : "";

                    var totalSpend = group.Value.Sum(i =>
                    {
                        i.TryGetProperty("metrics", out var met);
                        return GetDecimalValue(met, "spend");
                    });

                    var totalImpressions = group.Value.Sum(i =>
                    {
                        i.TryGetProperty("metrics", out var met);
                        return GetLongValue(met, "impressions");
                    });

                    var totalClicks = group.Value.Sum(i =>
                    {
                        i.TryGetProperty("metrics", out var met);
                        return GetLongValue(met, "clicks");
                    });

                    var totalConversions = (int)group.Value.Sum(i =>
                    {
                        i.TryGetProperty("metrics", out var met);
                        return GetDecimalValue(met, "conversion");
                    });

                    var roas = GetDecimalValue(metrics, "complete_payment_roas");
                    var conversionValue = roas > 0 ? totalSpend * roas : (decimal?)null;

                    var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
                    var cpc = totalClicks > 0 ? totalSpend / totalClicks : 0;
                    var cpa = totalConversions > 0 ? totalSpend / totalConversions : (decimal?)null;

                    campaigns.Add(new TikTokAdsCampaignDto(
                        group.Key,
                        campaignName,
                        "ACTIVE",
                        "CONVERSION",
                        totalSpend,
                        totalImpressions,
                        totalClicks,
                        totalConversions,
                        conversionValue,
                        ctr,
                        cpc,
                        cpa,
                        roas > 0 ? roas : null,
                        startDate,
                        endDate
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TikTok campaign insights");
        }

        return campaigns;
    }

    private static decimal GetDecimalValue(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return 0;

        if (element.TryGetProperty(property, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var val))
                return val;
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();
        }
        return 0;
    }

    private static long GetLongValue(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return 0;

        if (element.TryGetProperty(property, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var val))
                return val;
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt64();
        }
        return 0;
    }

    #endregion
}
