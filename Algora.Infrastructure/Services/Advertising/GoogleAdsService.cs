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
/// Service for integrating with Google Ads API.
/// </summary>
public class GoogleAdsService : IGoogleAdsService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAdsService> _logger;
    private readonly HttpClient _httpClient;

    private const string OAuthTokenUrl = "https://oauth2.googleapis.com/token";
    private const string OAuthAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleAdsApiBaseUrl = "https://googleads.googleapis.com/v18";

    public GoogleAdsService(
        AppDbContext db,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<GoogleAdsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GoogleAds");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<GoogleAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<GoogleAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new GoogleAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.CustomerId,
            connection.CustomerName,
            connection.ManagerAccountId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<GoogleAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveGoogleAdsConnectionDto dto)
    {
        var connection = await _db.Set<GoogleAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new GoogleAdsConnection { ShopDomain = shopDomain };
            _db.Set<GoogleAdsConnection>().Add(connection);
        }

        connection.RefreshToken = _encryption.Encrypt(dto.RefreshToken);
        connection.CustomerId = dto.CustomerId;
        connection.CustomerName = dto.CustomerName;
        connection.ManagerAccountId = dto.ManagerAccountId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.LastSyncError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Google Ads connected for shop {ShopDomain}, Customer ID: {CustomerId}",
            shopDomain, dto.CustomerId);

        return new GoogleAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.CustomerId,
            connection.CustomerName,
            connection.ManagerAccountId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<bool> DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<GoogleAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return false;

        connection.RefreshToken = null;
        connection.IsConnected = false;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Google Ads disconnected for shop {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestConnectionAsync(string shopDomain)
    {
        try
        {
            var connection = await _db.Set<GoogleAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.RefreshToken == null) return false;

            var refreshToken = _encryption.Decrypt(connection.RefreshToken);
            var accessToken = await RefreshAccessTokenAsync(refreshToken);

            return !string.IsNullOrEmpty(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Ads connection test failed for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<GoogleAdsCustomerDto>> GetAccessibleCustomersAsync(string refreshToken)
    {
        var customers = new List<GoogleAdsCustomerDto>();

        try
        {
            var accessToken = await RefreshAccessTokenAsync(refreshToken);
            if (string.IsNullOrEmpty(accessToken)) return customers;

            var developerToken = _configuration["GoogleAds:DeveloperToken"] ?? "";

            // List accessible customers
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{GoogleAdsApiBaseUrl}/customers:listAccessibleCustomers");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("developer-token", developerToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to list accessible customers: {Error}", error);
                return customers;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("resourceNames", out var resourceNames))
            {
                foreach (var resourceName in resourceNames.EnumerateArray())
                {
                    var customerId = resourceName.GetString()?.Replace("customers/", "");
                    if (string.IsNullOrEmpty(customerId)) continue;

                    // Get customer details
                    var customerDetails = await GetCustomerDetailsAsync(accessToken, developerToken, customerId);
                    if (customerDetails != null)
                    {
                        customers.Add(customerDetails);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google Ads accessible customers");
        }

        return customers;
    }

    public async Task<GoogleAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<GoogleAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.RefreshToken == null || connection.CustomerId == null)
            {
                return new GoogleAdsSyncResultDto(false, 0, 0, 0, "Google Ads not connected", DateTime.UtcNow);
            }

            var refreshToken = _encryption.Decrypt(connection.RefreshToken);
            var accessToken = await RefreshAccessTokenAsync(refreshToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                return new GoogleAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh access token", DateTime.UtcNow);
            }

            // Fetch campaign insights from Google Ads API
            var insights = await FetchCampaignInsightsAsync(accessToken, connection.CustomerId, connection.ManagerAccountId, start, end);
            campaignsProcessed = insights.Count;

            foreach (var insight in insights)
            {
                // Check if record exists for this campaign and date
                var existing = await _db.AdsSpends
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "Google" &&
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
                        Platform = "Google",
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
                "Google Ads sync completed for {ShopDomain}: {Campaigns} campaigns, {Created} created, {Updated} updated",
                shopDomain, campaignsProcessed, recordsCreated, recordsUpdated);

            return new GoogleAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Ads sync failed for {ShopDomain}", shopDomain);

            // Update error status
            var connection = await _db.Set<GoogleAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
            if (connection != null)
            {
                connection.LastSyncError = ex.Message;
                await _db.SaveChangesAsync();
            }

            return new GoogleAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, DateTime.UtcNow);
        }
    }

    public async Task<List<GoogleAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<GoogleAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection?.RefreshToken == null || connection.CustomerId == null)
            return new List<GoogleAdsCampaignDto>();

        var refreshToken = _encryption.Decrypt(connection.RefreshToken);
        var accessToken = await RefreshAccessTokenAsync(refreshToken);

        if (string.IsNullOrEmpty(accessToken))
            return new List<GoogleAdsCampaignDto>();

        return await FetchCampaignInsightsAsync(accessToken, connection.CustomerId, connection.ManagerAccountId, startDate, endDate);
    }

    public async Task<List<GoogleAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var adGroups = new List<GoogleAdsAdGroupDto>();

        try
        {
            var connection = await _db.Set<GoogleAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.RefreshToken == null || connection.CustomerId == null)
                return adGroups;

            var refreshToken = _encryption.Decrypt(connection.RefreshToken);
            var accessToken = await RefreshAccessTokenAsync(refreshToken);

            if (string.IsNullOrEmpty(accessToken))
                return adGroups;

            var developerToken = _configuration["GoogleAds:DeveloperToken"] ?? "";

            // GAQL query for ad group performance
            var query = $@"
                SELECT
                    ad_group.id,
                    ad_group.name,
                    ad_group.status,
                    campaign.id,
                    campaign.name,
                    metrics.cost_micros,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.conversions
                FROM ad_group
                WHERE segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'";

            var results = await ExecuteGaqlQueryAsync(accessToken, developerToken, connection.CustomerId, connection.ManagerAccountId, query);

            foreach (var row in results)
            {
                if (!row.TryGetProperty("adGroup", out var adGroup)) continue;

                var costMicros = row.TryGetProperty("metrics", out var metrics) &&
                                 metrics.TryGetProperty("costMicros", out var cm)
                    ? long.Parse(cm.GetString() ?? "0")
                    : 0L;

                var spend = costMicros / 1_000_000m;
                var impressions = metrics.TryGetProperty("impressions", out var imp)
                    ? long.Parse(imp.GetString() ?? "0")
                    : 0L;
                var clicks = metrics.TryGetProperty("clicks", out var clk)
                    ? long.Parse(clk.GetString() ?? "0")
                    : 0L;
                var conversions = metrics.TryGetProperty("conversions", out var conv)
                    ? (int)double.Parse(conv.GetString() ?? "0")
                    : 0;

                adGroups.Add(new GoogleAdsAdGroupDto(
                    adGroup.TryGetProperty("id", out var agId) ? agId.GetString() ?? "" : "",
                    adGroup.TryGetProperty("name", out var agName) ? agName.GetString() ?? "" : "",
                    row.TryGetProperty("campaign", out var camp) && camp.TryGetProperty("id", out var campId)
                        ? campId.GetString() ?? ""
                        : "",
                    camp.TryGetProperty("name", out var campName) ? campName.GetString() ?? "" : "",
                    adGroup.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "",
                    spend,
                    impressions,
                    clicks,
                    conversions,
                    impressions > 0 ? (decimal)clicks / impressions * 100 : 0,
                    clicks > 0 ? spend / clicks : 0
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google Ads ad groups for {ShopDomain}", shopDomain);
        }

        return adGroups;
    }

    public async Task<List<GoogleAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<GoogleAdsDailyInsightDto>();

        try
        {
            var connection = await _db.Set<GoogleAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.RefreshToken == null || connection.CustomerId == null)
                return insights;

            var refreshToken = _encryption.Decrypt(connection.RefreshToken);
            var accessToken = await RefreshAccessTokenAsync(refreshToken);

            if (string.IsNullOrEmpty(accessToken))
                return insights;

            var developerToken = _configuration["GoogleAds:DeveloperToken"] ?? "";

            // GAQL query for daily metrics
            var query = $@"
                SELECT
                    segments.date,
                    metrics.cost_micros,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.conversions,
                    metrics.conversions_value
                FROM customer
                WHERE segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'";

            var results = await ExecuteGaqlQueryAsync(accessToken, developerToken, connection.CustomerId, connection.ManagerAccountId, query);

            foreach (var row in results)
            {
                if (!row.TryGetProperty("segments", out var segments) ||
                    !segments.TryGetProperty("date", out var dateEl))
                    continue;

                var dateStr = dateEl.GetString();
                if (!DateTime.TryParse(dateStr, out var date)) continue;

                var costMicros = row.TryGetProperty("metrics", out var metrics) &&
                                 metrics.TryGetProperty("costMicros", out var cm)
                    ? long.Parse(cm.GetString() ?? "0")
                    : 0L;

                var spend = costMicros / 1_000_000m;
                var impressions = metrics.TryGetProperty("impressions", out var imp)
                    ? long.Parse(imp.GetString() ?? "0")
                    : 0L;
                var clicks = metrics.TryGetProperty("clicks", out var clk)
                    ? long.Parse(clk.GetString() ?? "0")
                    : 0L;
                var conversions = metrics.TryGetProperty("conversions", out var conv)
                    ? (int)double.Parse(conv.GetString() ?? "0")
                    : 0;
                var conversionValue = metrics.TryGetProperty("conversionsValue", out var convVal)
                    ? decimal.Parse(convVal.GetString() ?? "0")
                    : (decimal?)null;

                insights.Add(new GoogleAdsDailyInsightDto(
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google Ads daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<GoogleAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<GoogleAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        // Get aggregated data from local database
        var adsData = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Google" &&
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

        return new GoogleAdsSummaryDto(
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

    public async Task<GoogleOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        try
        {
            var clientId = _configuration["GoogleAds:ClientId"] ?? "";
            var clientSecret = _configuration["GoogleAds:ClientSecret"] ?? "";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            });

            var response = await _httpClient.PostAsync(OAuthTokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to exchange code: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return new GoogleOAuthTokenResponse(
                doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                doc.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600,
                doc.RootElement.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Google OAuth code");
            return null;
        }
    }

    public async Task<string?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var clientId = _configuration["GoogleAds:ClientId"] ?? "";
            var clientSecret = _configuration["GoogleAds:ClientSecret"] ?? "";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "refresh_token" }
            });

            var response = await _httpClient.PostAsync(OAuthTokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to refresh token: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Google access token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["GoogleAds:ClientId"] ?? "";
        var scopes = Uri.EscapeDataString("https://www.googleapis.com/auth/adwords");

        return $"{OAuthAuthUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}&scope={scopes}&response_type=code&access_type=offline&prompt=consent";
    }

    #region Private Methods

    private async Task<GoogleAdsCustomerDto?> GetCustomerDetailsAsync(string accessToken, string developerToken, string customerId)
    {
        try
        {
            var query = @"
                SELECT
                    customer.id,
                    customer.descriptive_name,
                    customer.currency_code,
                    customer.manager
                FROM customer
                LIMIT 1";

            var results = await ExecuteGaqlQueryAsync(accessToken, developerToken, customerId, null, query);
            if (results.Count == 0) return null;

            var row = results[0];
            if (!row.TryGetProperty("customer", out var customer)) return null;

            return new GoogleAdsCustomerDto(
                customer.TryGetProperty("id", out var id) ? id.GetString() ?? customerId : customerId,
                customer.TryGetProperty("descriptiveName", out var name) ? name.GetString() ?? "" : "",
                customer.TryGetProperty("currencyCode", out var currency) ? currency.GetString() ?? "USD" : "USD",
                null,
                customer.TryGetProperty("manager", out var isManager) && isManager.GetBoolean()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer details for {CustomerId}", customerId);
            return null;
        }
    }

    private async Task<List<GoogleAdsCampaignDto>> FetchCampaignInsightsAsync(
        string accessToken, string customerId, string? managerAccountId, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<GoogleAdsCampaignDto>();

        try
        {
            var developerToken = _configuration["GoogleAds:DeveloperToken"] ?? "";

            // GAQL query for campaign performance
            var query = $@"
                SELECT
                    campaign.id,
                    campaign.name,
                    campaign.status,
                    campaign.advertising_channel_type,
                    segments.date,
                    metrics.cost_micros,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.conversions,
                    metrics.conversions_value
                FROM campaign
                WHERE segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'
                  AND campaign.status != 'REMOVED'";

            var results = await ExecuteGaqlQueryAsync(accessToken, developerToken, customerId, managerAccountId, query);

            // Group by campaign ID to aggregate daily metrics
            var campaignGroups = results
                .Where(r => r.TryGetProperty("campaign", out _))
                .GroupBy(r =>
                {
                    r.TryGetProperty("campaign", out var c);
                    c.TryGetProperty("id", out var id);
                    return id.GetString() ?? "";
                });

            foreach (var group in campaignGroups)
            {
                var firstRow = group.First();
                firstRow.TryGetProperty("campaign", out var campaign);

                var totalCostMicros = group.Sum(r =>
                {
                    r.TryGetProperty("metrics", out var m);
                    m.TryGetProperty("costMicros", out var cm);
                    return long.TryParse(cm.GetString(), out var v) ? v : 0;
                });

                var totalImpressions = group.Sum(r =>
                {
                    r.TryGetProperty("metrics", out var m);
                    m.TryGetProperty("impressions", out var imp);
                    return long.TryParse(imp.GetString(), out var v) ? v : 0;
                });

                var totalClicks = group.Sum(r =>
                {
                    r.TryGetProperty("metrics", out var m);
                    m.TryGetProperty("clicks", out var clk);
                    return long.TryParse(clk.GetString(), out var v) ? v : 0;
                });

                var totalConversions = group.Sum(r =>
                {
                    r.TryGetProperty("metrics", out var m);
                    m.TryGetProperty("conversions", out var conv);
                    return double.TryParse(conv.GetString(), out var v) ? (int)v : 0;
                });

                var totalConversionValue = group.Sum(r =>
                {
                    r.TryGetProperty("metrics", out var m);
                    m.TryGetProperty("conversionsValue", out var convVal);
                    return decimal.TryParse(convVal.GetString(), out var v) ? v : 0;
                });

                var spend = totalCostMicros / 1_000_000m;
                var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
                var cpc = totalClicks > 0 ? spend / totalClicks : 0;
                var cpa = totalConversions > 0 ? spend / totalConversions : (decimal?)null;
                var roas = spend > 0 ? totalConversionValue / spend : (decimal?)null;

                campaigns.Add(new GoogleAdsCampaignDto(
                    campaign.TryGetProperty("id", out var cId) ? cId.GetString() ?? "" : "",
                    campaign.TryGetProperty("name", out var cName) ? cName.GetString() ?? "" : "",
                    campaign.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "",
                    campaign.TryGetProperty("advertisingChannelType", out var channelType) ? channelType.GetString() ?? "" : "",
                    spend,
                    totalImpressions,
                    totalClicks,
                    totalConversions,
                    totalConversionValue > 0 ? totalConversionValue : null,
                    ctr,
                    cpc,
                    cpa,
                    roas,
                    startDate,
                    endDate
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google Ads campaign insights");
        }

        return campaigns;
    }

    private async Task<List<JsonElement>> ExecuteGaqlQueryAsync(
        string accessToken, string developerToken, string customerId, string? loginCustomerId, string query)
    {
        var results = new List<JsonElement>();

        try
        {
            var url = $"{GoogleAdsApiBaseUrl}/customers/{customerId}/googleAds:search";

            var requestBody = new { query };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("developer-token", developerToken);

            if (!string.IsNullOrEmpty(loginCustomerId))
            {
                request.Headers.Add("login-customer-id", loginCustomerId);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Google Ads API error: {Error}", error);
                return results;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var resultsArray))
            {
                foreach (var item in resultsArray.EnumerateArray())
                {
                    results.Add(item.Clone());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GAQL query");
        }

        return results;
    }

    #endregion
}
