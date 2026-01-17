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
/// Service for integrating with LinkedIn Marketing API.
/// </summary>
public class LinkedInAdsService : ILinkedInAdsService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LinkedInAdsService> _logger;

    private const string LinkedInApiBaseUrl = "https://api.linkedin.com/rest";
    private const string LinkedInAuthUrl = "https://www.linkedin.com/oauth/v2/authorization";
    private const string LinkedInTokenUrl = "https://www.linkedin.com/oauth/v2/accessToken";
    private const string LinkedInApiVersion = "202401"; // January 2024 version

    public LinkedInAdsService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<LinkedInAdsService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LinkedInAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<LinkedInAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new LinkedInAdsConnectionDto(
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

    public async Task<LinkedInAdsConnectionDto> SaveConnectionAsync(string shopDomain, SaveLinkedInAdsConnectionDto dto)
    {
        var connection = await _db.Set<LinkedInAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new LinkedInAdsConnection
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<LinkedInAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryptionService.Encrypt(dto.AccessToken);
        connection.RefreshToken = _encryptionService.Encrypt(dto.RefreshToken);
        connection.AdAccountId = dto.AdAccountId;
        connection.AdAccountName = dto.AdAccountName;
        connection.OrganizationId = dto.OrganizationId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = DateTime.UtcNow.AddDays(60); // LinkedIn access tokens expire in 60 days
        connection.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(365); // Refresh tokens expire in 1 year
        connection.LastSyncError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new LinkedInAdsConnectionDto(
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
        var connection = await _db.Set<LinkedInAdsConnection>()
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
            var connection = await _db.Set<LinkedInAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null) return false;

            var accessToken = await GetValidAccessTokenAsync(connection);
            if (string.IsNullOrEmpty(accessToken)) return false;

            var client = CreateLinkedInClient(accessToken);
            var response = await client.GetAsync($"{LinkedInApiBaseUrl}/me");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test LinkedIn Ads connection for {ShopDomain}", shopDomain);
            return false;
        }
    }

    public async Task<List<LinkedInAdsAccountDto>> GetAdAccountsAsync(string accessToken)
    {
        var accounts = new List<LinkedInAdsAccountDto>();

        try
        {
            var client = CreateLinkedInClient(accessToken);

            // Get ad accounts (sponsored accounts)
            var response = await client.GetAsync($"{LinkedInApiBaseUrl}/adAccounts?q=search");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get LinkedIn ad accounts: {Status}", response.StatusCode);
                return accounts;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (!json.RootElement.TryGetProperty("elements", out var elements))
                return accounts;

            foreach (var account in elements.EnumerateArray())
            {
                var id = account.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var name = account.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                var currency = account.TryGetProperty("currency", out var currProp) ? currProp.GetString() : "USD";
                var status = account.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "UNKNOWN";
                var type = account.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "BUSINESS";

                // Get organization ID from reference
                string? orgId = null;
                if (account.TryGetProperty("reference", out var refProp))
                {
                    var refStr = refProp.GetString();
                    if (refStr?.Contains("organization") == true)
                    {
                        orgId = refStr.Split(':').LastOrDefault();
                    }
                }

                if (!string.IsNullOrEmpty(id))
                {
                    accounts.Add(new LinkedInAdsAccountDto(id, name ?? id, currency ?? "USD", orgId, status ?? "UNKNOWN", type ?? "BUSINESS"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LinkedIn ad accounts");
        }

        return accounts;
    }

    public async Task<LinkedInAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var syncedAt = DateTime.UtcNow;
        var recordsCreated = 0;
        var recordsUpdated = 0;
        var campaignsProcessed = 0;

        try
        {
            var connection = await _db.Set<LinkedInAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

            if (connection?.AccessToken == null)
            {
                return new LinkedInAdsSyncResultDto(false, 0, 0, 0, "No active LinkedIn Ads connection", syncedAt);
            }

            var accessToken = await GetValidAccessTokenAsync(connection);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new LinkedInAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh access token", syncedAt);
            }

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var client = CreateLinkedInClient(accessToken);

            // Get campaigns for this ad account
            var campaignsResponse = await client.GetAsync(
                $"{LinkedInApiBaseUrl}/adCampaigns?q=search&search=(account:(values:List(urn%3Ali%3AsponsoredAccount%3A{connection.AdAccountId})))");

            if (!campaignsResponse.IsSuccessStatusCode)
            {
                var error = await campaignsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get LinkedIn campaigns: {Error}", error);
                return new LinkedInAdsSyncResultDto(false, 0, 0, 0, $"API error: {campaignsResponse.StatusCode}", syncedAt);
            }

            var campaignsContent = await campaignsResponse.Content.ReadAsStringAsync();
            var campaignsJson = JsonDocument.Parse(campaignsContent);

            if (!campaignsJson.RootElement.TryGetProperty("elements", out var campaignsArray))
            {
                return new LinkedInAdsSyncResultDto(true, 0, 0, 0, null, syncedAt);
            }

            foreach (var campaign in campaignsArray.EnumerateArray())
            {
                var campaignId = campaign.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var campaignName = campaign.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown";

                if (string.IsNullOrEmpty(campaignId)) continue;
                campaignsProcessed++;

                // Get campaign analytics
                var analyticsUrl = $"{LinkedInApiBaseUrl}/adAnalytics?q=analytics" +
                                  $"&pivot=CAMPAIGN" +
                                  $"&dateRange=(start:(year:{start.Year},month:{start.Month},day:{start.Day})," +
                                  $"end:(year:{end.Year},month:{end.Month},day:{end.Day}))" +
                                  $"&campaigns=List(urn%3Ali%3AsponsoredCampaign%3A{campaignId})" +
                                  $"&fields=impressions,clicks,costInLocalCurrency,externalWebsiteConversions,conversionValueInLocalCurrency";

                var analyticsResponse = await client.GetAsync(analyticsUrl);
                if (!analyticsResponse.IsSuccessStatusCode) continue;

                var analyticsContent = await analyticsResponse.Content.ReadAsStringAsync();
                var analyticsJson = JsonDocument.Parse(analyticsContent);

                decimal spend = 0, conversionValue = 0;
                long impressions = 0, clicks = 0;
                int conversions = 0;

                if (analyticsJson.RootElement.TryGetProperty("elements", out var analyticsElements) &&
                    analyticsElements.EnumerateArray().Any())
                {
                    var stats = analyticsElements.EnumerateArray().First();
                    spend = stats.TryGetProperty("costInLocalCurrency", out var costProp) ? costProp.GetDecimal() : 0;
                    impressions = stats.TryGetProperty("impressions", out var impProp) ? impProp.GetInt64() : 0;
                    clicks = stats.TryGetProperty("clicks", out var clicksProp) ? clicksProp.GetInt64() : 0;
                    conversions = stats.TryGetProperty("externalWebsiteConversions", out var convProp) ? convProp.GetInt32() : 0;
                    conversionValue = stats.TryGetProperty("conversionValueInLocalCurrency", out var convValueProp) ? convValueProp.GetDecimal() : 0;
                }

                // Store in AdsSpends table
                var existingSpend = await _db.Set<AdsSpend>()
                    .FirstOrDefaultAsync(a =>
                        a.ShopDomain == shopDomain &&
                        a.Platform == "LinkedIn" &&
                        a.CampaignId == campaignId &&
                        a.SpendDate.Date == start.Date);

                if (existingSpend != null)
                {
                    existingSpend.Amount = spend;
                    existingSpend.Impressions = (int)Math.Min(impressions, int.MaxValue);
                    existingSpend.Clicks = (int)Math.Min(clicks, int.MaxValue);
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
                        Platform = "LinkedIn",
                        CampaignId = campaignId,
                        CampaignName = campaignName,
                        SpendDate = start.Date,
                        Amount = spend,
                        Impressions = (int)Math.Min(impressions, int.MaxValue),
                        Clicks = (int)Math.Min(clicks, int.MaxValue),
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

            return new LinkedInAdsSyncResultDto(true, campaignsProcessed, recordsCreated, recordsUpdated, null, syncedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync LinkedIn Ads for {ShopDomain}", shopDomain);
            return new LinkedInAdsSyncResultDto(false, campaignsProcessed, recordsCreated, recordsUpdated, ex.Message, syncedAt);
        }
    }

    public async Task<List<LinkedInAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<LinkedInAdsCampaignDto>();

        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "LinkedIn" &&
                           a.SpendDate >= startDate.Date &&
                           a.SpendDate <= endDate.Date)
                .GroupBy(a => new { a.CampaignId, a.CampaignName })
                .Select(g => new
                {
                    g.Key.CampaignId,
                    g.Key.CampaignName,
                    Spend = g.Sum(x => x.Amount),
                    Impressions = g.Sum(x => x.Impressions ?? 0),
                    Clicks = g.Sum(x => x.Clicks ?? 0),
                    Conversions = g.Sum(x => x.Conversions ?? 0),
                    ConversionValue = g.Sum(x => x.Revenue ?? 0)
                })
                .ToListAsync();

            foreach (var spend in spends)
            {
                var ctr = spend.Impressions > 0 ? (decimal)spend.Clicks / spend.Impressions * 100 : 0;
                var cpc = spend.Clicks > 0 ? spend.Spend / spend.Clicks : 0;
                var cpa = spend.Conversions > 0 ? spend.Spend / spend.Conversions : (decimal?)null;
                var roas = spend.Spend > 0 ? spend.ConversionValue / spend.Spend : (decimal?)null;

                campaigns.Add(new LinkedInAdsCampaignDto(
                    spend.CampaignId ?? "unknown",
                    spend.CampaignName ?? "Unknown Campaign",
                    "ACTIVE",
                    "UNKNOWN",
                    spend.Spend,
                    spend.Impressions,
                    spend.Clicks,
                    spend.Conversions,
                    spend.ConversionValue,
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
            _logger.LogError(ex, "Failed to get LinkedIn campaigns for {ShopDomain}", shopDomain);
        }

        return campaigns;
    }

    public async Task<List<LinkedInAdsCampaignGroupDto>> GetCampaignGroupsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        // Campaign groups would require additional API calls and storage
        // For now, return empty list - can be extended later
        return new List<LinkedInAdsCampaignGroupDto>();
    }

    public async Task<List<LinkedInAdsDailyInsightDto>> GetDailyInsightsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var insights = new List<LinkedInAdsDailyInsightDto>();

        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "LinkedIn" &&
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
                    Clicks = g.Sum(x => x.Clicks ?? 0),
                    Conversions = g.Sum(x => x.Conversions ?? 0),
                    ConversionValue = g.Sum(x => x.Revenue ?? 0)
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            foreach (var spend in spends)
            {
                var ctr = spend.Impressions > 0 ? (decimal)spend.Clicks / spend.Impressions * 100 : 0;
                var cpc = spend.Clicks > 0 ? spend.Spend / spend.Clicks : 0;

                insights.Add(new LinkedInAdsDailyInsightDto(
                    spend.Date,
                    spend.CampaignId,
                    spend.CampaignName,
                    spend.Spend,
                    spend.Impressions,
                    spend.Clicks,
                    spend.Conversions,
                    spend.ConversionValue,
                    ctr,
                    cpc
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LinkedIn daily insights for {ShopDomain}", shopDomain);
        }

        return insights;
    }

    public async Task<LinkedInAdsSummaryDto> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        try
        {
            var spends = await _db.Set<AdsSpend>()
                .Where(a => a.ShopDomain == shopDomain &&
                           a.Platform == "LinkedIn" &&
                           a.SpendDate >= startDate.Date &&
                           a.SpendDate <= endDate.Date)
                .ToListAsync();

            var connection = await _db.Set<LinkedInAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

            var totalSpend = spends.Sum(s => s.Amount);
            var totalConversionValue = spends.Sum(s => s.Revenue ?? 0);
            var totalImpressions = spends.Sum(s => s.Impressions ?? 0);
            var totalClicks = spends.Sum(s => s.Clicks ?? 0);
            var totalConversions = spends.Sum(s => s.Conversions ?? 0);
            var activeCampaigns = spends.Select(s => s.CampaignId).Distinct().Count();

            var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
            var cpc = totalClicks > 0 ? totalSpend / totalClicks : 0;
            var cpa = totalConversions > 0 ? totalSpend / totalConversions : (decimal?)null;
            var roas = totalSpend > 0 ? totalConversionValue / totalSpend : 0;

            return new LinkedInAdsSummaryDto(
                totalSpend,
                totalConversionValue,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LinkedIn Ads summary for {ShopDomain}", shopDomain);
            return new LinkedInAdsSummaryDto(0, 0, 0, 0, 0, 0, 0, null, 0, 0, null);
        }
    }

    public async Task<LinkedInOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        try
        {
            var clientId = _configuration["LinkedInAds:ClientId"];
            var clientSecret = _configuration["LinkedInAds:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("LinkedIn Ads client credentials not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient("LinkedInAds");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await client.PostAsync(LinkedInTokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange LinkedIn code: {Response}", responseContent);
                return null;
            }

            var tokenData = JsonDocument.Parse(responseContent);
            var root = tokenData.RootElement;

            return new LinkedInOAuthTokenResponse(
                root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 5184000, // 60 days default
                root.TryGetProperty("refresh_token_expires_in", out var rtei) ? rtei.GetInt32() : 31536000, // 1 year default
                root.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange LinkedIn authorization code");
            return null;
        }
    }

    public async Task<LinkedInOAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var clientId = _configuration["LinkedInAds:ClientId"];
            var clientSecret = _configuration["LinkedInAds:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("LinkedIn Ads client credentials not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient("LinkedInAds");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await client.PostAsync(LinkedInTokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh LinkedIn token: {Response}", responseContent);
                return null;
            }

            var tokenData = JsonDocument.Parse(responseContent);
            var root = tokenData.RootElement;

            return new LinkedInOAuthTokenResponse(
                root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 5184000,
                root.TryGetProperty("refresh_token_expires_in", out var rtei) ? rtei.GetInt32() : 31536000,
                root.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh LinkedIn access token");
            return null;
        }
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["LinkedInAds:ClientId"];
        var scopes = "r_ads,r_ads_reporting,r_organization_admin";

        return $"{LinkedInAuthUrl}?" +
               $"response_type=code" +
               $"&client_id={Uri.EscapeDataString(clientId ?? "")}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope={Uri.EscapeDataString(scopes)}";
    }

    private HttpClient CreateLinkedInClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient("LinkedInAds");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("LinkedIn-Version", LinkedInApiVersion);
        client.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");
        return client;
    }

    private async Task<string?> GetValidAccessTokenAsync(LinkedInAdsConnection connection)
    {
        if (connection.AccessToken == null) return null;

        var accessToken = _encryptionService.Decrypt(connection.AccessToken);

        // Check if token is expired or about to expire
        if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.AddDays(1))
        {
            if (connection.RefreshToken == null)
            {
                _logger.LogWarning("LinkedIn access token expired and no refresh token available for {ShopDomain}", connection.ShopDomain);
                return null;
            }

            var refreshToken = _encryptionService.Decrypt(connection.RefreshToken);
            var newTokens = await RefreshAccessTokenAsync(refreshToken);

            if (newTokens == null)
            {
                _logger.LogWarning("Failed to refresh LinkedIn access token for {ShopDomain}", connection.ShopDomain);
                return null;
            }

            connection.AccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
            if (!string.IsNullOrEmpty(newTokens.RefreshToken))
            {
                connection.RefreshToken = _encryptionService.Encrypt(newTokens.RefreshToken);
            }
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn);
            connection.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            accessToken = newTokens.AccessToken;
        }

        return accessToken;
    }
}
