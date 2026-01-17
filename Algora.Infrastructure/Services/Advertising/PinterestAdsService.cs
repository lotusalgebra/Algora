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
/// Service for integrating with Pinterest Ads API v5.
/// </summary>
public class PinterestAdsService : IPinterestAdsService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinterestAdsService> _logger;
    private readonly HttpClient _httpClient;

    private const string ApiBaseUrl = "https://api.pinterest.com/v5";
    private const string OAuthAuthUrl = "https://www.pinterest.com/oauth/";
    private const string OAuthTokenUrl = "https://api.pinterest.com/v5/oauth/token";

    public PinterestAdsService(
        AppDbContext db,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<PinterestAdsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PinterestAds");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<PinterestAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<PinterestAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new PinterestAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.BusinessId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<PinterestAdsConnectionDto> SaveConnectionAsync(string shopDomain, SavePinterestAdsConnectionDto dto)
    {
        var connection = await _db.Set<PinterestAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new PinterestAdsConnection { ShopDomain = shopDomain };
            _db.Set<PinterestAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryption.Encrypt(dto.AccessToken);
        connection.RefreshToken = _encryption.Encrypt(dto.RefreshToken);
        connection.AdAccountId = dto.AdAccountId;
        connection.AdAccountName = dto.AdAccountName;
        connection.BusinessId = dto.BusinessId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.LastSyncError = null;
        connection.TokenExpiresAt = DateTime.UtcNow.AddDays(30); // Pinterest tokens expire in ~30 days
        connection.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(365); // Refresh tokens last ~1 year
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Pinterest Ads connected for shop {ShopDomain}, Ad Account: {AdAccountId}",
            shopDomain, dto.AdAccountId);

        return new PinterestAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.BusinessId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<bool> DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<PinterestAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return false;

        connection.AccessToken = null;
        connection.RefreshToken = null;
        connection.IsConnected = false;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Pinterest Ads disconnected for shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestConnectionAsync(string shopDomain)
    {
        try
        {
            var connection = await _db.Set<PinterestAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null) return false;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/user_account");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pinterest Ads connection test failed for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<PinterestAdsAccountDto>> GetAdAccountsAsync(string accessToken)
    {
        var accounts = new List<PinterestAdsAccountDto>();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ad_accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to fetch ad accounts: {Error}", error);
                return accounts;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                    var currency = item.TryGetProperty("currency", out var currProp) ? currProp.GetString() ?? "USD" : "USD";
                    var country = item.TryGetProperty("country", out var countryProp) ? countryProp.GetString() ?? "US" : "US";

                    accounts.Add(new PinterestAdsAccountDto(id, name, currency, null, country));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pinterest ad accounts");
        }

        return accounts;
    }

    public async Task<PinterestAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<PinterestAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
            {
                return new PinterestAdsSyncResultDto(false, 0, 0, 0, "Pinterest Ads not connected", DateTime.UtcNow);
            }

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            // Check if token needs refresh
            if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value < DateTime.UtcNow.AddDays(1))
            {
                var refreshToken = _encryption.Decrypt(connection.RefreshToken!);
                var newTokens = await RefreshAccessTokenAsync(refreshToken);
                if (newTokens != null)
                {
                    connection.AccessToken = _encryption.Encrypt(newTokens.AccessToken);
                    connection.RefreshToken = _encryption.Encrypt(newTokens.RefreshToken);
                    connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
                    accessToken = newTokens.AccessToken;
                    await _db.SaveChangesAsync();
                }
            }

            // Fetch campaign insights from Pinterest Ads API
            var insights = await FetchCampaignInsightsAsync(accessToken, connection.AdAccountId, start, end);
            campaignsProcessed = insights.Count;

            foreach (var insight in insights)
            {
                // Check if record exists for this campaign and date
                var existing = await _db.AdsSpends
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "Pinterest" &&
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
                        Platform = "Pinterest",
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
                "Pinterest Ads sync completed for {ShopDomain}: {Campaigns} campaigns, {Created} created, {Updated} updated",
                shopDomain, campaignsProcessed, recordsCreated, recordsUpdated);

            return new PinterestAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pinterest Ads sync failed for {ShopDomain}", shopDomain);

            // Update error status
            var connection = await _db.Set<PinterestAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
            if (connection != null)
            {
                connection.LastSyncError = ex.Message;
                await _db.SaveChangesAsync();
            }

            return new PinterestAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, DateTime.UtcNow);
        }
    }

    public async Task<List<PinterestAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<PinterestAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection?.AccessToken == null || connection.AdAccountId == null)
            return new List<PinterestAdsCampaignDto>();

        var accessToken = _encryption.Decrypt(connection.AccessToken);
        return await FetchCampaignInsightsAsync(accessToken, connection.AdAccountId, startDate, endDate);
    }

    public async Task<List<PinterestAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var adGroups = new List<PinterestAdsAdGroupDto>();

        try
        {
            var connection = await _db.Set<PinterestAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
                return adGroups;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            // Get ad groups with analytics
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/ad_accounts/{connection.AdAccountId}/ad_groups/analytics" +
                $"?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
                $"&columns=SPEND_IN_MICRO_DOLLAR,IMPRESSION,CLICKTHROUGH,TOTAL_CONVERSIONS" +
                $"&granularity=TOTAL");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return adGroups;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var adGroupId = item.TryGetProperty("ad_group_id", out var agId) ? agId.GetString() ?? "" : "";
                    var spendMicro = item.TryGetProperty("SPEND_IN_MICRO_DOLLAR", out var sp) ? sp.GetInt64() : 0;
                    var spend = spendMicro / 1_000_000m;
                    var impressions = item.TryGetProperty("IMPRESSION", out var imp) ? imp.GetInt64() : 0;
                    var clicks = item.TryGetProperty("CLICKTHROUGH", out var clk) ? clk.GetInt64() : 0;
                    var conversions = item.TryGetProperty("TOTAL_CONVERSIONS", out var conv) ? conv.GetInt32() : 0;

                    adGroups.Add(new PinterestAdsAdGroupDto(
                        adGroupId,
                        "",
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
            _logger.LogError(ex, "Error fetching Pinterest ad groups for {ShopDomain}", shopDomain);
        }

        return adGroups;
    }

    public async Task<List<PinterestAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<PinterestAdsDailyInsightDto>();

        try
        {
            var connection = await _db.Set<PinterestAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null || connection.AdAccountId == null)
                return insights;

            var accessToken = _encryption.Decrypt(connection.AccessToken);

            // Get account-level daily analytics
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/ad_accounts/{connection.AdAccountId}/analytics" +
                $"?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
                $"&columns=SPEND_IN_MICRO_DOLLAR,IMPRESSION,CLICKTHROUGH,TOTAL_CONVERSIONS,TOTAL_CONVERSIONS_VALUE_IN_MICRO_DOLLAR" +
                $"&granularity=DAY");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return insights;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var dateStr = item.TryGetProperty("DATE", out var d) ? d.GetString() : null;
                    if (!DateTime.TryParse(dateStr, out var date)) continue;

                    var spendMicro = item.TryGetProperty("SPEND_IN_MICRO_DOLLAR", out var sp) ? sp.GetInt64() : 0;
                    var spend = spendMicro / 1_000_000m;
                    var impressions = item.TryGetProperty("IMPRESSION", out var imp) ? imp.GetInt64() : 0;
                    var clicks = item.TryGetProperty("CLICKTHROUGH", out var clk) ? clk.GetInt64() : 0;
                    var conversions = item.TryGetProperty("TOTAL_CONVERSIONS", out var conv) ? conv.GetInt32() : 0;
                    var convValueMicro = item.TryGetProperty("TOTAL_CONVERSIONS_VALUE_IN_MICRO_DOLLAR", out var cvMicro) ? cvMicro.GetInt64() : 0;
                    var conversionValue = convValueMicro > 0 ? convValueMicro / 1_000_000m : (decimal?)null;

                    insights.Add(new PinterestAdsDailyInsightDto(
                        date,
                        null,
                        null,
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        conversionValue,
                        impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                        clicks > 0 ? spend / clicks : 0
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pinterest daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<PinterestAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<PinterestAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        // Get aggregated data from local database
        var adsData = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Pinterest" &&
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

        return new PinterestAdsSummaryDto(
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

    public async Task<PinterestOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        try
        {
            var appId = _configuration["PinterestAds:AppId"] ?? "";
            var appSecret = _configuration["PinterestAds:AppSecret"] ?? "";

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appId}:{appSecret}"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to exchange code: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return new PinterestOAuthTokenResponse(
                doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 2592000,
                doc.RootElement.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "bearer" : "bearer",
                doc.RootElement.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Pinterest OAuth code");
            return null;
        }
    }

    public async Task<PinterestOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var appId = _configuration["PinterestAds:AppId"] ?? "";
            var appSecret = _configuration["PinterestAds:AppSecret"] ?? "";

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appId}:{appSecret}"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to refresh token: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return new PinterestOAuthTokenResponse(
                doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 2592000,
                doc.RootElement.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "bearer" : "bearer",
                doc.RootElement.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Pinterest access token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var appId = _configuration["PinterestAds:AppId"] ?? "";
        var scopes = "ads:read,user_accounts:read";

        return $"{OAuthAuthUrl}?client_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}&state={state}";
    }

    #region Private Methods

    private async Task<List<PinterestAdsCampaignDto>> FetchCampaignInsightsAsync(
        string accessToken, string adAccountId, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<PinterestAdsCampaignDto>();

        try
        {
            // First get list of campaigns
            var campaignsRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/ad_accounts/{adAccountId}/campaigns");
            campaignsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var campaignsResponse = await _httpClient.SendAsync(campaignsRequest);
            if (!campaignsResponse.IsSuccessStatusCode)
            {
                var error = await campaignsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Pinterest API error getting campaigns: {Error}", error);
                return campaigns;
            }

            var campaignsJson = await campaignsResponse.Content.ReadAsStringAsync();
            using var campaignsDoc = JsonDocument.Parse(campaignsJson);

            var campaignIds = new List<(string Id, string Name, string Status, string Objective)>();
            if (campaignsDoc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                    var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "" : "";
                    var objective = item.TryGetProperty("objective_type", out var objProp) ? objProp.GetString() ?? "" : "";
                    campaignIds.Add((id, name, status, objective));
                }
            }

            if (campaignIds.Count == 0) return campaigns;

            // Get analytics for campaigns
            var analyticsRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/ad_accounts/{adAccountId}/campaigns/analytics" +
                $"?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
                $"&campaign_ids={string.Join(",", campaignIds.Select(c => c.Id))}" +
                $"&columns=SPEND_IN_MICRO_DOLLAR,IMPRESSION,CLICKTHROUGH,TOTAL_CONVERSIONS,TOTAL_CONVERSIONS_VALUE_IN_MICRO_DOLLAR" +
                $"&granularity=TOTAL");
            analyticsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var analyticsResponse = await _httpClient.SendAsync(analyticsRequest);
            if (!analyticsResponse.IsSuccessStatusCode)
            {
                var error = await analyticsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Pinterest API error getting analytics: {Error}", error);
                return campaigns;
            }

            var analyticsJson = await analyticsResponse.Content.ReadAsStringAsync();
            using var analyticsDoc = JsonDocument.Parse(analyticsJson);

            if (analyticsDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in analyticsDoc.RootElement.EnumerateArray())
                {
                    var campaignId = item.TryGetProperty("CAMPAIGN_ID", out var cid) ? cid.GetString() ?? "" : "";
                    var campaignInfo = campaignIds.FirstOrDefault(c => c.Id == campaignId);

                    var spendMicro = item.TryGetProperty("SPEND_IN_MICRO_DOLLAR", out var sp) ? sp.GetInt64() : 0;
                    var spend = spendMicro / 1_000_000m;
                    var impressions = item.TryGetProperty("IMPRESSION", out var imp) ? imp.GetInt64() : 0;
                    var clicks = item.TryGetProperty("CLICKTHROUGH", out var clk) ? clk.GetInt64() : 0;
                    var conversions = item.TryGetProperty("TOTAL_CONVERSIONS", out var conv) ? conv.GetInt32() : 0;
                    var convValueMicro = item.TryGetProperty("TOTAL_CONVERSIONS_VALUE_IN_MICRO_DOLLAR", out var cvMicro) ? cvMicro.GetInt64() : 0;
                    var conversionValue = convValueMicro > 0 ? convValueMicro / 1_000_000m : (decimal?)null;

                    var ctr = impressions > 0 ? (decimal)clicks / impressions * 100 : 0;
                    var cpc = clicks > 0 ? spend / clicks : 0;
                    var cpa = conversions > 0 ? spend / conversions : (decimal?)null;
                    var roas = spend > 0 && conversionValue.HasValue ? conversionValue.Value / spend : (decimal?)null;

                    campaigns.Add(new PinterestAdsCampaignDto(
                        campaignId,
                        campaignInfo.Name ?? "",
                        campaignInfo.Status ?? "ACTIVE",
                        campaignInfo.Objective ?? "",
                        spend,
                        impressions,
                        clicks,
                        conversions,
                        conversionValue,
                        ctr,
                        cpc,
                        cpa,
                        roas,
                        startDate,
                        endDate
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pinterest campaign insights");
        }

        return campaigns;
    }

    #endregion
}
