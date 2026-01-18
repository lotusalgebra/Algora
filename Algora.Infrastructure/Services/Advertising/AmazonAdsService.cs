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
/// Service for managing Amazon Advertising integration.
/// Supports Sponsored Products, Sponsored Brands, and Sponsored Display campaigns.
/// </summary>
public class AmazonAdsService : IAmazonAdsService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AmazonAdsService> _logger;

    // Amazon Advertising API endpoints (North America)
    private const string AdsApiBaseUrl = "https://advertising-api.amazon.com/";
    private const string OAuthAuthorizeUrl = "https://www.amazon.com/ap/oa";
    private const string OAuthTokenUrl = "https://api.amazon.com/auth/o2/token";

    public AmazonAdsService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<AmazonAdsService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["Amazon:ClientId"];
        var scopes = "advertising::campaign_management";

        var url = $"{OAuthAuthorizeUrl}?" +
                  $"client_id={Uri.EscapeDataString(clientId ?? "")}&" +
                  $"scope={Uri.EscapeDataString(scopes)}&" +
                  $"response_type=code&" +
                  $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                  $"state={Uri.EscapeDataString(state)}";

        return url;
    }

    public async Task<AmazonOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        var clientId = _configuration["Amazon:ClientId"];
        var clientSecret = _configuration["Amazon:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId ?? "",
            ["client_secret"] = clientSecret ?? ""
        });

        try
        {
            var response = await client.PostAsync(OAuthTokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Amazon OAuth token exchange failed: {Response}", json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AmazonOAuthTokenResponse(
                root.GetProperty("access_token").GetString() ?? "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600,
                root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "bearer" : "bearer"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging Amazon OAuth code");
            return null;
        }
    }

    public async Task<AmazonAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new AmazonAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.ProfileId,
            connection.ProfileName,
            connection.MarketplaceId,
            connection.CountryCode,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task SaveConnectionAsync(string shopDomain, SaveAmazonAdsConnectionDto dto)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new AmazonAdsConnection
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<AmazonAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryptionService.Encrypt(dto.AccessToken);
        connection.RefreshToken = !string.IsNullOrEmpty(dto.RefreshToken)
            ? _encryptionService.Encrypt(dto.RefreshToken)
            : null;
        connection.ProfileId = dto.ProfileId;
        connection.ProfileName = dto.ProfileName;
        connection.MarketplaceId = dto.MarketplaceId;
        connection.CountryCode = dto.CountryCode;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = DateTime.UtcNow.AddHours(1); // Amazon tokens expire in 1 hour
        connection.LastSyncError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection != null)
        {
            connection.AccessToken = null;
            connection.RefreshToken = null;
            connection.IsConnected = false;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<AmazonAdsProfileDto>> GetProfilesAsync(string accessToken)
    {
        var clientId = _configuration["Amazon:ClientId"];
        var client = _httpClientFactory.CreateClient("AmazonAds");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("Amazon-Advertising-API-ClientId", clientId);

        try
        {
            var response = await client.GetAsync($"{AdsApiBaseUrl}v2/profiles");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Amazon advertising profiles: {Response}", json);
                return new List<AmazonAdsProfileDto>();
            }

            using var doc = JsonDocument.Parse(json);
            var profiles = new List<AmazonAdsProfileDto>();

            foreach (var profile in doc.RootElement.EnumerateArray())
            {
                var accountInfo = profile.TryGetProperty("accountInfo", out var ai) ? ai : default;

                profiles.Add(new AmazonAdsProfileDto(
                    profile.GetProperty("profileId").GetInt64().ToString(),
                    profile.TryGetProperty("countryCode", out var cc) ? cc.GetString() ?? "" : "",
                    accountInfo.ValueKind != JsonValueKind.Undefined && accountInfo.TryGetProperty("marketplaceStringId", out var msi)
                        ? msi.GetString() : null,
                    accountInfo.ValueKind != JsonValueKind.Undefined && accountInfo.TryGetProperty("name", out var name)
                        ? name.GetString() : null,
                    accountInfo.ValueKind != JsonValueKind.Undefined && accountInfo.TryGetProperty("type", out var type)
                        ? type.GetString() ?? "" : "",
                    profile.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "" : "",
                    profile.TryGetProperty("currencyCode", out var curr) ? curr.GetString() ?? "USD" : "USD"
                ));
            }

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Amazon advertising profiles");
            return new List<AmazonAdsProfileDto>();
        }
    }

    public async Task<AmazonAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection == null || string.IsNullOrEmpty(connection.AccessToken))
        {
            return new AmazonAdsSyncResultDto(false, 0, 0, 0, "Not connected to Amazon Ads", DateTime.UtcNow);
        }

        // Refresh token if needed
        if (connection.TokenExpiresAt <= DateTime.UtcNow)
        {
            var refreshed = await RefreshTokensAsync(shopDomain);
            if (!refreshed)
            {
                return new AmazonAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh access token", DateTime.UtcNow);
            }
            connection = await _db.Set<AmazonAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
        }

        var accessToken = _encryptionService.Decrypt(connection!.AccessToken!);
        var clientId = _configuration["Amazon:ClientId"];
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        int created = 0, updated = 0, processed = 0;

        try
        {
            var client = _httpClientFactory.CreateClient("AmazonAds");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Amazon-Advertising-API-ClientId", clientId);
            client.DefaultRequestHeaders.Add("Amazon-Advertising-API-Scope", connection.ProfileId);

            // Sync Sponsored Products campaigns
            var spResult = await SyncCampaignTypeAsync(client, connection, "sp", "SPONSORED_PRODUCTS", start, end);
            processed += spResult.Processed;
            created += spResult.Created;
            updated += spResult.Updated;

            // Sync Sponsored Brands campaigns
            var sbResult = await SyncCampaignTypeAsync(client, connection, "sb", "SPONSORED_BRANDS", start, end);
            processed += sbResult.Processed;
            created += sbResult.Created;
            updated += sbResult.Updated;

            // Sync Sponsored Display campaigns
            var sdResult = await SyncCampaignTypeAsync(client, connection, "sd", "SPONSORED_DISPLAY", start, end);
            processed += sdResult.Processed;
            created += sdResult.Created;
            updated += sdResult.Updated;

            await _db.SaveChangesAsync();

            // Update connection status
            connection.LastSyncedAt = DateTime.UtcNow;
            connection.LastSyncError = null;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new AmazonAdsSyncResultDto(true, processed, created, updated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Amazon Ads for {ShopDomain}", shopDomain);

            connection.LastSyncError = ex.Message;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new AmazonAdsSyncResultDto(false, processed, created, updated, ex.Message, DateTime.UtcNow);
        }
    }

    private async Task<(int Processed, int Created, int Updated)> SyncCampaignTypeAsync(
        HttpClient client,
        AmazonAdsConnection connection,
        string apiPrefix,
        string campaignType,
        DateTime startDate,
        DateTime endDate)
    {
        int processed = 0, created = 0, updated = 0;

        try
        {
            // Get campaigns list
            var campaignsUrl = $"{AdsApiBaseUrl}{apiPrefix}/campaigns";
            var campaignsResponse = await client.GetAsync(campaignsUrl);
            var campaignsJson = await campaignsResponse.Content.ReadAsStringAsync();

            if (!campaignsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch {CampaignType} campaigns: {Response}", campaignType, campaignsJson);
                return (processed, created, updated);
            }

            using var campaignsDoc = JsonDocument.Parse(campaignsJson);
            var campaigns = new List<(string Id, string Name, string Status)>();

            foreach (var campaign in campaignsDoc.RootElement.EnumerateArray())
            {
                var id = campaign.GetProperty("campaignId").GetInt64().ToString();
                var name = campaign.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var status = campaign.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
                campaigns.Add((id, name, status));
            }

            // Request report for campaign performance
            var reportRequest = new
            {
                startDate = startDate.ToString("yyyyMMdd"),
                endDate = endDate.ToString("yyyyMMdd"),
                metrics = "impressions,clicks,cost,attributedSales14d,attributedConversions14d"
            };

            var reportRequestJson = JsonSerializer.Serialize(reportRequest);
            var reportContent = new StringContent(reportRequestJson, Encoding.UTF8, "application/json");

            var reportUrl = $"{AdsApiBaseUrl}{apiPrefix}/campaigns/report";
            var reportResponse = await client.PostAsync(reportUrl, reportContent);
            var reportJson = await reportResponse.Content.ReadAsStringAsync();

            if (reportResponse.IsSuccessStatusCode)
            {
                using var reportDoc = JsonDocument.Parse(reportJson);

                if (reportDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var record in reportDoc.RootElement.EnumerateArray())
                    {
                        processed++;

                        var campaignId = record.TryGetProperty("campaignId", out var cid)
                            ? cid.GetInt64().ToString() : "";
                        var campaignInfo = campaigns.FirstOrDefault(c => c.Id == campaignId);

                        var cost = record.TryGetProperty("cost", out var c) ? c.GetDecimal() : 0;
                        var impressions = record.TryGetProperty("impressions", out var imp) ? imp.GetInt64() : 0;
                        var clicks = record.TryGetProperty("clicks", out var clk) ? clk.GetInt64() : 0;
                        var sales = record.TryGetProperty("attributedSales14d", out var sal) ? sal.GetDecimal() : 0;
                        var orders = record.TryGetProperty("attributedConversions14d", out var ord) ? ord.GetInt32() : 0;

                        // Upsert to AdsSpends table
                        var existing = await _db.AdsSpends.FirstOrDefaultAsync(a =>
                            a.ShopDomain == connection.ShopDomain &&
                            a.Platform == "Amazon" &&
                            a.CampaignId == campaignId &&
                            a.SpendDate.Date == startDate.Date);

                        if (existing == null)
                        {
                            _db.AdsSpends.Add(new AdsSpend
                            {
                                ShopDomain = connection.ShopDomain,
                                Platform = "Amazon",
                                CampaignId = campaignId,
                                CampaignName = campaignInfo.Name ?? $"{campaignType} Campaign",
                                SpendDate = startDate.Date,
                                Amount = cost,
                                Impressions = (int)impressions,
                                Clicks = (int)clicks,
                                Conversions = orders,
                                Revenue = sales,
                                CreatedAt = DateTime.UtcNow
                            });
                            created++;
                        }
                        else
                        {
                            existing.CampaignName = campaignInfo.Name ?? existing.CampaignName;
                            existing.Amount = cost;
                            existing.Impressions = (int)impressions;
                            existing.Clicks = (int)clicks;
                            existing.Conversions = orders;
                            existing.Revenue = sales;
                            updated++;
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Failed to get {CampaignType} report: {Response}", campaignType, reportJson);

                // Fallback: just record campaign existence without metrics
                foreach (var campaign in campaigns)
                {
                    processed++;

                    var existing = await _db.AdsSpends.FirstOrDefaultAsync(a =>
                        a.ShopDomain == connection.ShopDomain &&
                        a.Platform == "Amazon" &&
                        a.CampaignId == campaign.Id &&
                        a.SpendDate.Date == startDate.Date);

                    if (existing == null)
                    {
                        _db.AdsSpends.Add(new AdsSpend
                        {
                            ShopDomain = connection.ShopDomain,
                            Platform = "Amazon",
                            CampaignId = campaign.Id,
                            CampaignName = campaign.Name,
                            SpendDate = startDate.Date,
                            Amount = 0,
                            Impressions = 0,
                            Clicks = 0,
                            Conversions = 0,
                            Revenue = 0,
                            CreatedAt = DateTime.UtcNow
                        });
                        created++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error syncing {CampaignType} campaigns", campaignType);
        }

        return (processed, created, updated);
    }

    public async Task<List<AmazonAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var spends = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Amazon" &&
                        a.SpendDate >= startDate &&
                        a.SpendDate <= endDate)
            .GroupBy(a => new { a.CampaignId, a.CampaignName })
            .Select(g => new
            {
                g.Key.CampaignId,
                g.Key.CampaignName,
                Spend = g.Sum(x => x.Amount),
                Impressions = g.Sum(x => x.Impressions ?? 0),
                Clicks = g.Sum(x => x.Clicks ?? 0),
                Orders = g.Sum(x => x.Conversions ?? 0),
                Sales = g.Sum(x => x.Revenue ?? 0)
            })
            .ToListAsync();

        return spends.Select(s =>
        {
            var ctr = s.Impressions > 0 ? (decimal)s.Clicks / s.Impressions * 100 : 0;
            var cpc = s.Clicks > 0 ? s.Spend / s.Clicks : 0;
            var acos = s.Sales > 0 ? s.Spend / s.Sales * 100 : 0;
            var roas = s.Spend > 0 ? s.Sales / s.Spend : 0;

            return new AmazonAdsCampaignDto(
                s.CampaignId ?? "",
                s.CampaignName ?? "",
                "SPONSORED_PRODUCTS", // Type not stored
                "ENABLED",
                "AUTO",
                0, // Budget not stored
                s.Spend,
                s.Impressions,
                s.Clicks,
                s.Orders,
                s.Sales,
                acos,
                roas,
                ctr,
                cpc,
                startDate,
                endDate
            );
        }).ToList();
    }

    public async Task<List<AmazonAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, string campaignId)
    {
        // Ad groups would require additional API calls and storage
        return new List<AmazonAdsAdGroupDto>();
    }

    public async Task<AmazonAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        var stats = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Amazon" &&
                        a.SpendDate >= startDate &&
                        a.SpendDate <= endDate)
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalSpend = g.Sum(x => x.Amount),
                TotalImpressions = g.Sum(x => x.Impressions ?? 0),
                TotalClicks = g.Sum(x => x.Clicks ?? 0),
                TotalOrders = g.Sum(x => x.Conversions ?? 0),
                TotalSales = g.Sum(x => x.Revenue ?? 0),
                ActiveCampaigns = g.Select(x => x.CampaignId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        if (stats == null)
        {
            return new AmazonAdsSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, connection?.LastSyncedAt);
        }

        var ctr = stats.TotalImpressions > 0 ? (decimal)stats.TotalClicks / stats.TotalImpressions * 100 : 0;
        var cpc = stats.TotalClicks > 0 ? stats.TotalSpend / stats.TotalClicks : 0;
        var acos = stats.TotalSales > 0 ? stats.TotalSpend / stats.TotalSales * 100 : 0;
        var roas = stats.TotalSpend > 0 ? stats.TotalSales / stats.TotalSpend : 0;

        return new AmazonAdsSummaryDto(
            stats.TotalSpend,
            stats.TotalSales,
            stats.TotalImpressions,
            stats.TotalClicks,
            stats.TotalOrders,
            acos,
            roas,
            ctr,
            cpc,
            stats.ActiveCampaigns,
            0, // SP campaigns count - would need separate tracking
            0, // SB campaigns count
            0, // SD campaigns count
            connection?.LastSyncedAt
        );
    }

    public async Task<bool> RefreshTokensAsync(string shopDomain)
    {
        var connection = await _db.Set<AmazonAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null || string.IsNullOrEmpty(connection.RefreshToken))
        {
            return false;
        }

        var clientId = _configuration["Amazon:ClientId"];
        var clientSecret = _configuration["Amazon:ClientSecret"];
        var refreshToken = _encryptionService.Decrypt(connection.RefreshToken);

        var client = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId ?? "",
            ["client_secret"] = clientSecret ?? ""
        });

        try
        {
            var response = await client.PostAsync(OAuthTokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh Amazon token: {Response}", json);
                return false;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            connection.AccessToken = _encryptionService.Encrypt(root.GetProperty("access_token").GetString() ?? "");

            if (root.TryGetProperty("refresh_token", out var newRefreshToken))
            {
                connection.RefreshToken = _encryptionService.Encrypt(newRefreshToken.GetString() ?? "");
            }

            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(
                root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600);
            connection.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Amazon tokens");
            return false;
        }
    }
}
