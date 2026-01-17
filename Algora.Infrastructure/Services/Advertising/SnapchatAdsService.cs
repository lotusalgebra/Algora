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
/// Service for integrating with Snapchat Marketing API.
/// </summary>
public class SnapchatAdsService : ISnapchatAdsService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SnapchatAdsService> _logger;

    private const string SnapchatApiBaseUrl = "https://adsapi.snapchat.com/v1";
    private const string SnapchatAuthUrl = "https://accounts.snapchat.com/login/oauth2/authorize";
    private const string SnapchatTokenUrl = "https://accounts.snapchat.com/login/oauth2/access_token";

    public SnapchatAdsService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<SnapchatAdsService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SnapchatAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<SnapchatAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new SnapchatAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.OrganizationId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<SnapchatAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveSnapchatAdsConnectionDto dto)
    {
        var connection = await _db.Set<SnapchatAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new SnapchatAdsConnection
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<SnapchatAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryptionService.Encrypt(dto.AccessToken);
        connection.RefreshToken = _encryptionService.Encrypt(dto.RefreshToken);
        connection.AdAccountId = dto.AdAccountId;
        connection.AdAccountName = dto.AdAccountName;
        connection.OrganizationId = dto.OrganizationId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = DateTime.UtcNow.AddMinutes(30); // Snapchat tokens expire in 30 minutes
        connection.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(365); // Refresh tokens are long-lived
        connection.LastSyncError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new SnapchatAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.OrganizationId,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task<bool> DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<SnapchatAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return false;

        connection.IsConnected = false;
        connection.AccessToken = null;
        connection.RefreshToken = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TestConnectionAsync(string shopDomain)
    {
        try
        {
            var connection = await _db.Set<SnapchatAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null) return false;

            var accessToken = await GetValidAccessTokenAsync(connection);
            if (string.IsNullOrEmpty(accessToken)) return false;

            var client = _httpClientFactory.CreateClient("SnapchatAds");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"{SnapchatApiBaseUrl}/me");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Snapchat Ads connection for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<SnapchatAdsAccountDto>> GetAdAccountsAsync(string accessToken)
    {
        var accounts = new List<SnapchatAdsAccountDto>();

        try
        {
            var client = _httpClientFactory.CreateClient("SnapchatAds");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // First get organizations
            var orgResponse = await client.GetAsync($"{SnapchatApiBaseUrl}/me/organizations");
            if (!orgResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Snapchat organizations: {Status}", orgResponse.StatusCode);
                return accounts;
            }

            var orgContent = await orgResponse.Content.ReadAsStringAsync();
            var orgJson = JsonDocument.Parse(orgContent);

            if (!orgJson.RootElement.TryGetProperty("organizations", out var orgsArray))
                return accounts;

            foreach (var org in orgsArray.EnumerateArray())
            {
                var orgId = org.TryGetProperty("organization", out var orgData) &&
                           orgData.TryGetProperty("id", out var orgIdProp)
                    ? orgIdProp.GetString() : null;

                if (string.IsNullOrEmpty(orgId)) continue;

                // Get ad accounts for this organization
                var adAccountsResponse = await client.GetAsync($"{SnapchatApiBaseUrl}/organizations/{orgId}/adaccounts");
                if (!adAccountsResponse.IsSuccessStatusCode) continue;

                var adAccountsContent = await adAccountsResponse.Content.ReadAsStringAsync();
                var adAccountsJson = JsonDocument.Parse(adAccountsContent);

                if (!adAccountsJson.RootElement.TryGetProperty("adaccounts", out var adAccountsArray))
                    continue;

                foreach (var account in adAccountsArray.EnumerateArray())
                {
                    if (!account.TryGetProperty("adaccount", out var adAccount)) continue;

                    var id = adAccount.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var name = adAccount.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var currency = adAccount.TryGetProperty("currency", out var currencyProp) ? currencyProp.GetString() : "USD";
                    var status = adAccount.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "UNKNOWN";
                    var type = adAccount.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "UNKNOWN";

                    if (!string.IsNullOrEmpty(id))
                    {
                        accounts.Add(new SnapchatAdsAccountDto(id, name ?? id, currency ?? "USD", orgId, status ?? "UNKNOWN", type ?? "UNKNOWN"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapchat ad accounts");
        }

        return accounts;
    }

    public async Task<SnapchatAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var syncedAt = DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<SnapchatAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null)
            {
                return new SnapchatAdsSyncResultDto(false, 0, 0, 0, "No active Snapchat Ads connection", syncedAt);
            }

            var accessToken = await GetValidAccessTokenAsync(connection);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new SnapchatAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh access token", syncedAt);
            }

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var client = _httpClientFactory.CreateClient("SnapchatAds");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get campaigns
            var campaignsResponse = await client.GetAsync(
                $"{SnapchatApiBaseUrl}/adaccounts/{connection.AdAccountId}/campaigns");

            if (!campaignsResponse.IsSuccessStatusCode)
            {
                var error = await campaignsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get Snapchat campaigns: {Error}", error);
                return new SnapchatAdsSyncResultDto(false, 0, 0, 0, $"API error: {campaignsResponse.StatusCode}", syncedAt);
            }

            var campaignsContent = await campaignsResponse.Content.ReadAsStringAsync();
            var campaignsJson = JsonDocument.Parse(campaignsContent);

            if (!campaignsJson.RootElement.TryGetProperty("campaigns", out var campaignsArray))
            {
                return new SnapchatAdsSyncResultDto(true, 0, 0, 0, null, syncedAt);
            }

            foreach (var campaignWrapper in campaignsArray.EnumerateArray())
            {
                if (!campaignWrapper.TryGetProperty("campaign", out var campaign)) continue;

                var campaignId = campaign.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var campaignName = campaign.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown";
                var status = campaign.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "UNKNOWN";

                if (string.IsNullOrEmpty(campaignId)) continue;
                campaignsProcessed++;

                // Get campaign stats
                var statsUrl = $"{SnapchatApiBaseUrl}/campaigns/{campaignId}/stats" +
                              $"?granularity=TOTAL" +
                              $"&start_time={start:yyyy-MM-dd}T00:00:00.000-00:00" +
                              $"&end_time={end:yyyy-MM-dd}T23:59:59.999-00:00";

                var statsResponse = await client.GetAsync(statsUrl);
                if (!statsResponse.IsSuccessStatusCode) continue;

                var statsContent = await statsResponse.Content.ReadAsStringAsync();
                var statsJson = JsonDocument.Parse(statsContent);

                decimal spend = 0, conversionValue = 0;
                long impressions = 0, swipes = 0;
                int conversions = 0;

                if (statsJson.RootElement.TryGetProperty("total_stats", out var totalStats) &&
                    totalStats.EnumerateArray().Any())
                {
                    var stats = totalStats.EnumerateArray().First();
                    if (stats.TryGetProperty("total_stat", out var stat))
                    {
                        // Snapchat returns spend in micro-currency (millionths)
                        var spendMicro = stat.TryGetProperty("spend", out var spendProp) ? spendProp.GetInt64() : 0;
                        spend = spendMicro / 1_000_000m;
                        impressions = stat.TryGetProperty("impressions", out var impProp) ? impProp.GetInt64() : 0;
                        swipes = stat.TryGetProperty("swipes", out var swipeProp) ? swipeProp.GetInt64() : 0;
                        conversions = stat.TryGetProperty("conversion_purchases", out var convProp) ? convProp.GetInt32() : 0;
                        var convValueMicro = stat.TryGetProperty("conversion_purchases_value", out var convValueProp) ? convValueProp.GetInt64() : 0;
                        conversionValue = convValueMicro / 1_000_000m;
                    }
                }

                // Store in AdsSpends table
                var existingSpend = await _db.Set<AdsSpend>()
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "Snapchat" &&
                        a.CampaignId == campaignId &&
                        a.SpendDate.Date == start.Date);

                if (existingSpend != null)
                {
                    existingSpend.Amount = spend;
                    existingSpend.Impressions = (int)Math.Min(impressions, int.MaxValue);
                    existingSpend.Clicks = (int)Math.Min(swipes, int.MaxValue);
                    existingSpend.Conversions = conversions;
                    existingSpend.Revenue = conversionValue;
                    existingSpend.CampaignName = campaignName;
                    existingSpend.UpdatedAt = DateTime.UtcNow;
                    recordsUpdated++;
                }
                else
                {
                    _db.Set<AdsSpend>().Add(new AdsSpend
                    {
                        ShopDomain = shopDomain,
                        Platform = "Snapchat",
                        CampaignId = campaignId,
                        CampaignName = campaignName,
                        SpendDate = start.Date,
                        Amount = spend,
                        Impressions = (int)Math.Min(impressions, int.MaxValue),
                        Clicks = (int)Math.Min(swipes, int.MaxValue),
                        Conversions = conversions,
                        Revenue = conversionValue,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    recordsCreated++;
                }
            }

            // Update connection sync status
            connection.LastSyncedAt = syncedAt;
            connection.LastSyncError = null;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new SnapchatAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, syncedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Snapchat Ads for {ShopDomain}", shopDomain);
            return new SnapchatAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, syncedAt);
        }
    }

    public async Task<List<SnapchatAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<SnapchatAdsCampaignDto>();

        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "Snapchat" &&
                           a.SpendDate >= startDate.Date &&
                           a.SpendDate <= endDate.Date)
                .GroupBy(a => new { a.CampaignId, a.CampaignName })
                .Select(g => new
                {
                    g.Key.CampaignId,
                    g.Key.CampaignName,
                    Spend = g.Sum(x => x.Amount),
                    Impressions = g.Sum(x => x.Impressions ?? 0),
                    Swipes = g.Sum(x => x.Clicks ?? 0),
                    Conversions = g.Sum(x => x.Conversions ?? 0),
                    ConversionValue = g.Sum(x => x.Revenue ?? 0)
                })
                .ToListAsync();

            foreach (var spend in spends)
            {
                var swipeRate = spend.Impressions > 0 ? (decimal)spend.Swipes / spend.Impressions * 100 : 0;
                var costPerSwipe = spend.Swipes > 0 ? spend.Spend / spend.Swipes : 0;
                var cpa = spend.Conversions > 0 ? spend.Spend / spend.Conversions : (decimal?)null;
                var roas = spend.Spend > 0 ? spend.ConversionValue / spend.Spend : (decimal?)null;

                campaigns.Add(new SnapchatAdsCampaignDto(
                    spend.CampaignId ?? "unknown",
                    spend.CampaignName ?? "Unknown Campaign",
                    "ACTIVE",
                    "UNKNOWN",
                    spend.Spend,
                    spend.Impressions,
                    spend.Swipes,
                    spend.Conversions,
                    spend.ConversionValue,
                    swipeRate,
                    costPerSwipe,
                    cpa,
                    roas,
                    startDate,
                    endDate
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapchat campaigns for {ShopDomain}", shopDomain);
        }

        return campaigns;
    }

    public async Task<List<SnapchatAdsAdSquadDto>> GetAdSquadsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        // Ad squad data would require additional API calls and storage
        // For now, return empty list - can be extended later
        return new List<SnapchatAdsAdSquadDto>();
    }

    public async Task<List<SnapchatAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<SnapchatAdsDailyInsightDto>();

        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "Snapchat" &&
                           a.SpendDate >= startDate.Date &&
                           a.SpendDate <= endDate.Date)
                .GroupBy(a => new { a.SpendDate, a.CampaignId, a.CampaignName })
                .Select(g => new
                {
                    Date = g.Key.SpendDate,
                    g.Key.CampaignId,
                    g.Key.CampaignName,
                    Spend = g.Sum(x => x.Amount),
                    Impressions = g.Sum(x => x.Impressions ?? 0),
                    Swipes = g.Sum(x => x.Clicks ?? 0),
                    Conversions = g.Sum(x => x.Conversions ?? 0),
                    ConversionValue = g.Sum(x => x.Revenue ?? 0)
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            foreach (var spend in spends)
            {
                var swipeRate = spend.Impressions > 0 ? (decimal)spend.Swipes / spend.Impressions * 100 : 0;
                var costPerSwipe = spend.Swipes > 0 ? spend.Spend / spend.Swipes : 0;

                insights.Add(new SnapchatAdsDailyInsightDto(
                    spend.Date,
                    spend.CampaignId,
                    spend.CampaignName,
                    spend.Spend,
                    spend.Impressions,
                    spend.Swipes,
                    spend.Conversions,
                    spend.ConversionValue,
                    swipeRate,
                    costPerSwipe
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapchat daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<SnapchatAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "Snapchat" &&
                           a.SpendDate >= startDate.Date &&
                           a.SpendDate <= endDate.Date)
                .ToListAsync();

            var connection = await _db.Set<SnapchatAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

            var totalSpend = spends.Sum(s => s.Amount);
            var totalConversionValue = spends.Sum(s => s.Revenue ?? 0);
            var totalImpressions = spends.Sum(s => s.Impressions ?? 0);
            var totalSwipes = spends.Sum(s => s.Clicks ?? 0);
            var totalConversions = spends.Sum(s => s.Conversions ?? 0);
            var activeCampaigns = spends.Select(s => s.CampaignId).Distinct().Count();

            var swipeRate = totalImpressions > 0 ? (decimal)totalSwipes / totalImpressions * 100 : 0;
            var costPerSwipe = totalSwipes > 0 ? totalSpend / totalSwipes : 0;
            var cpa = totalConversions > 0 ? totalSpend / totalConversions : (decimal?)null;
            var roas = totalSpend > 0 ? totalConversionValue / totalSpend : 0;

            return new SnapchatAdsSummaryDto(
                totalSpend,
                totalConversionValue,
                totalImpressions,
                totalSwipes,
                totalConversions,
                swipeRate,
                costPerSwipe,
                cpa,
                roas,
                activeCampaigns,
                connection?.LastSyncedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Snapchat Ads summary for {ShopDomain}", shopDomain);
            return new SnapchatAdsSummaryDto(0, 0, 0, 0, 0, 0, 0, null, 0, 0, null);
        }
    }

    public async Task<SnapchatOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        try
        {
            var clientId = _configuration["SnapchatAds:ClientId"];
            var clientSecret = _configuration["SnapchatAds:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Snapchat Ads client credentials not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient("SnapchatAds");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await client.PostAsync(SnapchatTokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange Snapchat code: {Response}", responseContent);
                return null;
            }

            var tokenData = JsonDocument.Parse(responseContent);
            var root = tokenData.RootElement;

            return new SnapchatOAuthTokenResponse(
                root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 1800,
                root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer",
                root.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Snapchat authorization code");
            return null;
        }
    }

    public async Task<SnapchatOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var clientId = _configuration["SnapchatAds:ClientId"];
            var clientSecret = _configuration["SnapchatAds:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Snapchat Ads client credentials not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient("SnapchatAds");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await client.PostAsync(SnapchatTokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh Snapchat token: {Response}", responseContent);
                return null;
            }

            var tokenData = JsonDocument.Parse(responseContent);
            var root = tokenData.RootElement;

            return new SnapchatOAuthTokenResponse(
                root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 1800,
                root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer",
                root.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Snapchat access token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["SnapchatAds:ClientId"];
        var scopes = "snapchat-marketing-api";

        return $"{SnapchatAuthUrl}?" +
               $"client_id={Uri.EscapeDataString(clientId ?? "")}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scopes)}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    private async Task<string?> GetValidAccessTokenAsync(SnapchatAdsConnection connection)
    {
        if (connection.AccessToken == null) return null;

        var accessToken = _encryptionService.Decrypt(connection.AccessToken);

        // Check if token is expired or about to expire
        if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            if (connection.RefreshToken == null)
            {
                _logger.LogWarning("Snapchat access token expired and no refresh token available for {ShopDomain}", connection.ShopDomain);
                return null;
            }

            var refreshToken = _encryptionService.Decrypt(connection.RefreshToken);
            var newTokens = await RefreshAccessTokenAsync(refreshToken);

            if (newTokens == null)
            {
                _logger.LogWarning("Failed to refresh Snapchat access token for {ShopDomain}", connection.ShopDomain);
                return null;
            }

            connection.AccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
            connection.RefreshToken = _encryptionService.Encrypt(newTokens.RefreshToken);
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
            connection.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            accessToken = newTokens.AccessToken;
        }

        return accessToken;
    }
}
