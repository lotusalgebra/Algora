using System.Net.Http.Headers;
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
/// Service for integrating with Meta (Facebook/Instagram) Marketing API.
/// </summary>
public class MetaAdsService : IMetaAdsService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaAdsService> _logger;
    private readonly HttpClient _httpClient;

    private const string GraphApiBaseUrl = "https://graph.facebook.com/v18.0";
    private const string OAuthBaseUrl = "https://www.facebook.com/v18.0/dialog/oauth";

    public MetaAdsService(
        AppDbContext db,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<MetaAdsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MetaAds");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<MetaAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<MetaAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new MetaAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.BusinessName,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<MetaAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveMetaAdsConnectionDto dto)
    {
        var connection = await _db.Set<MetaAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new MetaAdsConnection { ShopDomain = shopDomain };
            _db.Set<MetaAdsConnection>().Add(connection);
        }

        // Exchange for long-lived token
        var longLivedToken = await ExchangeTokenAsync(dto.AccessToken);

        connection.AccessToken = _encryption.Encrypt(longLivedToken ?? dto.AccessToken);
        connection.AdAccountId = dto.AdAccountId;
        connection.AdAccountName = dto.AdAccountName;
        connection.BusinessName = dto.BusinessName;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.LastSyncError = null;
        connection.TokenExpiresAt = longLivedToken != null ? DateTime.UtcNow.AddDays(60) : DateTime.UtcNow.AddHours(1);
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Meta Ads connected for shop {ShopDomain}, Ad Account: {AdAccountId}",
            shopDomain, dto.AdAccountId);

        return new MetaAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.BusinessName,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<bool> DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<MetaAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return false;

        connection.AccessToken = null;
        connection.IsConnected = false;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Meta Ads disconnected for shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestConnectionAsync(string shopDomain)
    {
        try
        {
            var connection = await _db.Set<MetaAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null) return false;

            var accessToken = _encryption.Decrypt(connection.AccessToken);
            var url = $"{GraphApiBaseUrl}/me?access_token={accessToken}";

            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta Ads connection test failed for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<MetaAdAccountDto>> GetAdAccountsAsync(string accessToken)
    {
        var accounts = new List<MetaAdAccountDto>();

        try
        {
            var url = $"{GraphApiBaseUrl}/me/adaccounts?fields=id,name,account_status,currency,business&access_token={accessToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch ad accounts: {StatusCode}", response.StatusCode);
                return accounts;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var account in data.EnumerateArray())
                {
                    var id = account.GetProperty("id").GetString() ?? "";
                    var name = account.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var status = account.TryGetProperty("account_status", out var s) ? s.GetInt32().ToString() : "unknown";
                    var currency = account.TryGetProperty("currency", out var c) ? c.GetString() ?? "USD" : "USD";
                    var businessName = account.TryGetProperty("business", out var b) && b.TryGetProperty("name", out var bn)
                        ? bn.GetString() : null;

                    accounts.Add(new MetaAdAccountDto(id, name, status, currency, businessName));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Meta ad accounts");
        }

        return accounts;
    }

    public async Task<MetaAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<MetaAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
            {
                return new MetaAdsSyncResultDto(false, 0, 0, 0, "Meta Ads not connected", DateTime.UtcNow);
            }

            var accessToken = _encryption.Decrypt(connection.AccessToken);
            var adAccountId = connection.AdAccountId;

            // Fetch campaign insights from Meta API
            var insights = await FetchCampaignInsightsAsync(accessToken, adAccountId, start, end);
            campaignsProcessed = insights.Count;

            foreach (var insight in insights)
            {
                // Check if record exists for this campaign and date
                var existing = await _db.AdsSpends
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "Meta" &&
                        a.CampaignId == insight.CampaignId &&
                        a.SpendDate.Date == insight.DateStart.Date);

                if (existing != null)
                {
                    // Update existing record
                    existing.Amount = insight.Spend;
                    existing.Impressions = insight.Impressions;
                    existing.Clicks = insight.Clicks;
                    existing.Conversions = insight.Conversions;
                    existing.Revenue = insight.Revenue;
                    existing.UpdatedAt = DateTime.UtcNow;
                    recordsUpdated++;
                }
                else
                {
                    // Create new record
                    var adsSpend = new AdsSpend
                    {
                        ShopDomain = shopDomain,
                        Platform = "Meta",
                        CampaignId = insight.CampaignId,
                        CampaignName = insight.CampaignName,
                        SpendDate = insight.DateStart,
                        Amount = insight.Spend,
                        Currency = connection.Currency,
                        Impressions = insight.Impressions,
                        Clicks = insight.Clicks,
                        Conversions = insight.Conversions,
                        Revenue = insight.Revenue,
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
                "Meta Ads sync completed for {ShopDomain}: {Campaigns} campaigns, {Created} created, {Updated} updated",
                shopDomain, campaignsProcessed, recordsCreated, recordsUpdated);

            return new MetaAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta Ads sync failed for {ShopDomain}", shopDomain);

            // Update error status
            var connection = await _db.Set<MetaAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
            if (connection != null)
            {
                connection.LastSyncError = ex.Message;
                await _db.SaveChangesAsync();
            }

            return new MetaAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, DateTime.UtcNow);
        }
    }

    public async Task<List<MetaAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<MetaAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection?.AccessToken == null || connection.AdAccountId == null)
            return new List<MetaAdsCampaignDto>();

        var accessToken = _encryption.Decrypt(connection.AccessToken);
        return await FetchCampaignInsightsAsync(accessToken, connection.AdAccountId, startDate, endDate);
    }

    public async Task<List<MetaAdsAdSetDto>> GetAdSetsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var adSets = new List<MetaAdsAdSetDto>();

        try
        {
            var connection = await _db.Set<MetaAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
                return adSets;

            var accessToken = _encryption.Decrypt(connection.AccessToken);
            var dateRange = $"{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd}";

            var url = $"{GraphApiBaseUrl}/{connection.AdAccountId}/adsets" +
                      $"?fields=id,name,campaign_id,campaign{{name}},status,insights.time_range({{{dateRange}}}){{spend,impressions,clicks,conversions}}" +
                      $"&access_token={accessToken}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return adSets;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var adSet in data.EnumerateArray())
                {
                    var insights = adSet.TryGetProperty("insights", out var ins) &&
                                   ins.TryGetProperty("data", out var insData) &&
                                   insData.GetArrayLength() > 0
                        ? insData[0]
                        : (JsonElement?)null;

                    if (insights == null) continue;

                    var spend = GetDecimalValue(insights.Value, "spend");
                    var impressions = GetIntValue(insights.Value, "impressions");
                    var clicks = GetIntValue(insights.Value, "clicks");
                    var conversions = GetIntValue(insights.Value, "conversions");

                    adSets.Add(new MetaAdsAdSetDto(
                        adSet.GetProperty("id").GetString() ?? "",
                        adSet.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        adSet.TryGetProperty("campaign_id", out var cid) ? cid.GetString() ?? "" : "",
                        adSet.TryGetProperty("campaign", out var c) && c.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "",
                        adSet.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
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
            _logger.LogError(ex, "Error fetching Meta ad sets for {ShopDomain}", shopDomain);
        }

        return adSets;
    }

    public async Task<List<MetaAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<MetaAdsDailyInsightDto>();

        try
        {
            var connection = await _db.Set<MetaAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
                return insights;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            var url = $"{GraphApiBaseUrl}/{connection.AdAccountId}/insights" +
                      $"?fields=spend,impressions,clicks,conversions,purchase_roas" +
                      $"&time_range={{'since':'{startDate:yyyy-MM-dd}','until':'{endDate:yyyy-MM-dd}'}}" +
                      $"&time_increment=1" +
                      $"&access_token={accessToken}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return insights;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var day in data.EnumerateArray())
                {
                    var dateStr = day.TryGetProperty("date_start", out var ds) ? ds.GetString() : null;
                    if (dateStr == null || !DateTime.TryParse(dateStr, out var date)) continue;

                    var spend = GetDecimalValue(day, "spend");
                    var impressions = GetIntValue(day, "impressions");
                    var clicks = GetIntValue(day, "clicks");
                    var conversions = GetIntValue(day, "conversions");
                    var roas = GetRoasValue(day);

                    insights.Add(new MetaAdsDailyInsightDto(
                        date,
                        null,
                        null,
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        roas > 0 ? spend * roas : null,
                        impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                        clicks > 0 ? spend / clicks : 0
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Meta daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<MetaAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<MetaAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        // Get aggregated data from local database
        var adsData = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Meta" &&
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

        return new MetaAdsSummaryDto(
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

    public async Task<string?> ExchangeTokenAsync(string shortLivedToken)
    {
        try
        {
            var appId = _configuration["Meta:AppId"];
            var appSecret = _configuration["Meta:AppSecret"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                _logger.LogWarning("Meta App credentials not configured, using short-lived token");
                return null;
            }

            var url = $"{GraphApiBaseUrl}/oauth/access_token" +
                      $"?grant_type=fb_exchange_token" +
                      $"&client_id={appId}" +
                      $"&client_secret={appSecret}" +
                      $"&fb_exchange_token={shortLivedToken}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("access_token", out var token)
                ? token.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Meta token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var appId = _configuration["Meta:AppId"] ?? "";
        var scopes = "ads_read,ads_management,business_management";

        return $"{OAuthBaseUrl}?client_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}&scope={scopes}&response_type=code";
    }

    #region Private Methods

    private async Task<List<MetaAdsCampaignDto>> FetchCampaignInsightsAsync(
        string accessToken, string adAccountId, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<MetaAdsCampaignDto>();

        try
        {
            var url = $"{GraphApiBaseUrl}/{adAccountId}/campaigns" +
                      $"?fields=id,name,status,objective,insights.time_range({{'since':'{startDate:yyyy-MM-dd}','until':'{endDate:yyyy-MM-dd}'}}){{spend,impressions,clicks,conversions,purchase_roas,date_start,date_stop}}" +
                      $"&access_token={accessToken}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Meta API error: {Error}", error);
                return campaigns;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var campaign in data.EnumerateArray())
                {
                    var insights = campaign.TryGetProperty("insights", out var ins) &&
                                   ins.TryGetProperty("data", out var insData) &&
                                   insData.GetArrayLength() > 0
                        ? insData[0]
                        : (JsonElement?)null;

                    if (insights == null) continue;

                    var spend = GetDecimalValue(insights.Value, "spend");
                    var impressions = GetIntValue(insights.Value, "impressions");
                    var clicks = GetIntValue(insights.Value, "clicks");
                    var conversions = GetIntValue(insights.Value, "conversions");
                    var roas = GetRoasValue(insights.Value);
                    var revenue = roas > 0 ? spend * roas : (decimal?)null;

                    var dateStartStr = insights.Value.TryGetProperty("date_start", out var ds) ? ds.GetString() : null;
                    var dateEndStr = insights.Value.TryGetProperty("date_stop", out var de) ? de.GetString() : null;

                    campaigns.Add(new MetaAdsCampaignDto(
                        campaign.GetProperty("id").GetString() ?? "",
                        campaign.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        campaign.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                        campaign.TryGetProperty("objective", out var o) ? o.GetString() ?? "" : "",
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        revenue,
                        impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                        clicks > 0 ? spend / clicks : 0,
                        conversions > 0 ? spend / conversions : (decimal?)null,
                        roas > 0 ? roas : (decimal?)null,
                        DateTime.TryParse(dateStartStr, out var dStart) ? dStart : startDate,
                        DateTime.TryParse(dateEndStr, out var dEnd) ? dEnd : endDate
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Meta campaign insights");
        }

        return campaigns;
    }

    private static decimal GetDecimalValue(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var val))
                return val;
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();
        }
        return 0;
    }

    private static int GetIntValue(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var val))
                return val;
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();
        }
        return 0;
    }

    private static decimal GetRoasValue(JsonElement element)
    {
        if (element.TryGetProperty("purchase_roas", out var roas) && roas.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in roas.EnumerateArray())
            {
                if (item.TryGetProperty("value", out var val))
                {
                    if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var roasVal))
                        return roasVal;
                    if (val.ValueKind == JsonValueKind.Number)
                        return val.GetDecimal();
                }
            }
        }
        return 0;
    }

    #endregion
}
