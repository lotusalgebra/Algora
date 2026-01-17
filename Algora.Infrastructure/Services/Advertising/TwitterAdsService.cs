using System.Net.Http.Headers;
using System.Security.Cryptography;
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
/// Service for managing Twitter (X) Ads integration using the Twitter Ads API.
/// </summary>
public class TwitterAdsService : ITwitterAdsService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwitterAdsService> _logger;

    private const string AdsApiBaseUrl = "https://ads-api.twitter.com/12/";
    private const string OAuthAuthorizeUrl = "https://twitter.com/i/oauth2/authorize";
    private const string OAuthTokenUrl = "https://api.twitter.com/2/oauth2/token";

    public TwitterAdsService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<TwitterAdsService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["Twitter:ClientId"];
        var scopes = "ads.read ads.write offline.access";

        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var url = $"{OAuthAuthorizeUrl}?" +
                  $"response_type=code&" +
                  $"client_id={Uri.EscapeDataString(clientId ?? "")}&" +
                  $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                  $"scope={Uri.EscapeDataString(scopes)}&" +
                  $"state={Uri.EscapeDataString(state)}&" +
                  $"code_challenge={codeChallenge}&" +
                  $"code_challenge_method=S256";

        return url;
    }

    public async Task<TwitterOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, string codeVerifier)
    {
        var clientId = _configuration["Twitter:ClientId"];
        var clientSecret = _configuration["Twitter:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        });

        try
        {
            var response = await client.PostAsync(OAuthTokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Twitter OAuth token exchange failed: {Response}", json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new TwitterOAuthTokenResponse(
                root.GetProperty("access_token").GetString() ?? "",
                root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 7200,
                root.TryGetProperty("scope", out var sc) ? sc.GetString() ?? "" : "",
                root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "bearer" : "bearer"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging Twitter OAuth code");
            return null;
        }
    }

    public async Task<TwitterAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new TwitterAdsConnectionDto(
            connection.Id,
            connection.ShopDomain,
            connection.AdAccountId,
            connection.AdAccountName,
            connection.IsConnected,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.LastSyncError
        );
    }

    public async Task SaveConnectionAsync(string shopDomain, SaveTwitterAdsConnectionDto dto)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new TwitterAdsConnection
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<TwitterAdsConnection>().Add(connection);
        }

        connection.AccessToken = _encryptionService.Encrypt(dto.AccessToken);
        connection.RefreshToken = !string.IsNullOrEmpty(dto.RefreshToken)
            ? _encryptionService.Encrypt(dto.RefreshToken)
            : null;
        connection.AdAccountId = dto.AdAccountId;
        connection.AdAccountName = dto.AdAccountName;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = DateTime.UtcNow.AddHours(2); // Twitter tokens expire in 2 hours
        connection.LastSyncError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string shopDomain)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
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

    public async Task<List<TwitterAdsAccountDto>> GetAdAccountsAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient("TwitterAds");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.GetAsync($"{AdsApiBaseUrl}accounts");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Twitter ad accounts: {Response}", json);
                return new List<TwitterAdsAccountDto>();
            }

            using var doc = JsonDocument.Parse(json);
            var accounts = new List<TwitterAdsAccountDto>();

            if (doc.RootElement.TryGetProperty("data", out var dataArray))
            {
                foreach (var account in dataArray.EnumerateArray())
                {
                    accounts.Add(new TwitterAdsAccountDto(
                        account.GetProperty("id").GetString() ?? "",
                        account.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        account.TryGetProperty("currency", out var curr) ? curr.GetString() ?? "USD" : "USD",
                        account.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "" : "",
                        account.TryGetProperty("approval_status", out var status) ? status.GetString() ?? "" : "",
                        account.TryGetProperty("deleted", out var del) && del.GetBoolean()
                    ));
                }
            }

            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Twitter ad accounts");
            return new List<TwitterAdsAccountDto>();
        }
    }

    public async Task<TwitterAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection == null || string.IsNullOrEmpty(connection.AccessToken))
        {
            return new TwitterAdsSyncResultDto(false, 0, 0, 0, "Not connected to Twitter Ads", DateTime.UtcNow);
        }

        // Refresh token if needed
        if (connection.TokenExpiresAt <= DateTime.UtcNow)
        {
            var refreshed = await RefreshTokensAsync(shopDomain);
            if (!refreshed)
            {
                return new TwitterAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh access token", DateTime.UtcNow);
            }
            // Reload connection with new token
            connection = await _db.Set<TwitterAdsConnection>()
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);
        }

        var accessToken = _encryptionService.Decrypt(connection!.AccessToken!);
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        int created = 0, updated = 0, processed = 0;

        try
        {
            var client = _httpClientFactory.CreateClient("TwitterAds");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get campaigns
            var campaignsUrl = $"{AdsApiBaseUrl}accounts/{connection.AdAccountId}/campaigns";
            var campaignsResponse = await client.GetAsync(campaignsUrl);
            var campaignsJson = await campaignsResponse.Content.ReadAsStringAsync();

            if (!campaignsResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Twitter campaigns: {Response}", campaignsJson);
                connection.LastSyncError = "Failed to fetch campaigns";
                connection.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return new TwitterAdsSyncResultDto(false, 0, 0, 0, "Failed to fetch campaigns", DateTime.UtcNow);
            }

            using var campaignsDoc = JsonDocument.Parse(campaignsJson);
            var campaigns = new List<(string Id, string Name, string Status)>();

            if (campaignsDoc.RootElement.TryGetProperty("data", out var campaignArray))
            {
                foreach (var campaign in campaignArray.EnumerateArray())
                {
                    var id = campaign.GetProperty("id").GetString() ?? "";
                    var name = campaign.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var status = campaign.TryGetProperty("entity_status", out var s) ? s.GetString() ?? "" : "";
                    campaigns.Add((id, name, status));
                }
            }

            // Get analytics for each campaign
            foreach (var campaign in campaigns)
            {
                processed++;

                try
                {
                    var statsUrl = $"{AdsApiBaseUrl}stats/accounts/{connection.AdAccountId}?" +
                                   $"entity=CAMPAIGN&" +
                                   $"entity_ids={campaign.Id}&" +
                                   $"start_time={start:yyyy-MM-ddT00:00:00Z}&" +
                                   $"end_time={end:yyyy-MM-ddT23:59:59Z}&" +
                                   $"granularity=DAY&" +
                                   $"metric_groups=BILLING,ENGAGEMENT";

                    var statsResponse = await client.GetAsync(statsUrl);
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();

                    if (!statsResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to get stats for campaign {CampaignId}: {Response}", campaign.Id, statsJson);
                        continue;
                    }

                    using var statsDoc = JsonDocument.Parse(statsJson);

                    if (statsDoc.RootElement.TryGetProperty("data", out var statsData) &&
                        statsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var dayStat in statsData.EnumerateArray())
                        {
                            if (!dayStat.TryGetProperty("id_data", out var idData)) continue;

                            foreach (var metric in idData.EnumerateArray())
                            {
                                if (!metric.TryGetProperty("metrics", out var metrics)) continue;

                                // Parse metrics - Twitter returns arrays for time series
                                var spendMicros = GetMetricSum(metrics, "billed_charge_local_micro");
                                var impressions = GetMetricSum(metrics, "impressions");
                                var clicks = GetMetricSum(metrics, "clicks");
                                var engagements = GetMetricSum(metrics, "engagements");

                                var spend = spendMicros / 1_000_000m; // Convert micros to actual currency

                                // Get the date from time_series_range if available
                                var spendDate = start;
                                if (dayStat.TryGetProperty("time_series_range", out var tsRange))
                                {
                                    if (tsRange.TryGetProperty("start", out var startTs))
                                    {
                                        var dateStr = startTs.GetString();
                                        if (DateTime.TryParse(dateStr, out var parsedDate))
                                        {
                                            spendDate = parsedDate;
                                        }
                                    }
                                }

                                // Upsert to AdsSpends table
                                var existing = await _db.AdsSpends.FirstOrDefaultAsync(a =>
                                    a.ShopDomain == shopDomain &&
                                    a.Platform == "Twitter" &&
                                    a.CampaignId == campaign.Id &&
                                    a.SpendDate.Date == spendDate.Date);

                                if (existing == null)
                                {
                                    _db.AdsSpends.Add(new AdsSpend
                                    {
                                        ShopDomain = shopDomain,
                                        Platform = "Twitter",
                                        CampaignId = campaign.Id,
                                        CampaignName = campaign.Name,
                                        SpendDate = spendDate.Date,
                                        Amount = spend,
                                        Impressions = (int)impressions,
                                        Clicks = (int)clicks,
                                        Conversions = 0, // Twitter conversion tracking requires additional setup
                                        Revenue = 0,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                    created++;
                                }
                                else
                                {
                                    existing.CampaignName = campaign.Name;
                                    existing.Amount = spend;
                                    existing.Impressions = (int)impressions;
                                    existing.Clicks = (int)clicks;
                                    updated++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing campaign {CampaignId}", campaign.Id);
                }
            }

            await _db.SaveChangesAsync();

            // Update connection status
            connection.LastSyncedAt = DateTime.UtcNow;
            connection.LastSyncError = null;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new TwitterAdsSyncResultDto(true, processed, created, updated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Twitter Ads for {ShopDomain}", shopDomain);

            connection.LastSyncError = ex.Message;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new TwitterAdsSyncResultDto(false, processed, created, updated, ex.Message, DateTime.UtcNow);
        }
    }

    public async Task<List<TwitterAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var spends = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Twitter" &&
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
                Conversions = g.Sum(x => x.Conversions ?? 0),
                Revenue = g.Sum(x => x.Revenue ?? 0)
            })
            .ToListAsync();

        return spends.Select(s =>
        {
            var ctr = s.Impressions > 0 ? (decimal)s.Clicks / s.Impressions * 100 : 0;
            var cpc = s.Clicks > 0 ? s.Spend / s.Clicks : 0;
            var cpa = s.Conversions > 0 ? s.Spend / s.Conversions : (decimal?)null;
            var roas = s.Spend > 0 ? s.Revenue / s.Spend : (decimal?)null;

            return new TwitterAdsCampaignDto(
                s.CampaignId ?? "",
                s.CampaignName ?? "",
                "ACTIVE", // Status not stored in AdsSpends
                "",       // Objective
                "",       // Funding instrument
                s.Spend,
                s.Impressions,
                s.Clicks,
                0,        // Engagements - would need separate tracking
                s.Conversions,
                s.Revenue > 0 ? s.Revenue : null,
                ctr,
                cpc,
                cpa,
                roas,
                startDate,
                endDate
            );
        }).ToList();
    }

    public async Task<List<TwitterAdsLineItemDto>> GetLineItemsAsync(string shopDomain, string campaignId)
    {
        // Line items would require additional API calls and storage
        // For now, return empty list
        return new List<TwitterAdsLineItemDto>();
    }

    public async Task<TwitterAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        var stats = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                        a.Platform == "Twitter" &&
                        a.SpendDate >= startDate &&
                        a.SpendDate <= endDate)
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalSpend = g.Sum(x => x.Amount),
                TotalImpressions = g.Sum(x => x.Impressions ?? 0),
                TotalClicks = g.Sum(x => x.Clicks ?? 0),
                TotalConversions = g.Sum(x => x.Conversions ?? 0),
                TotalRevenue = g.Sum(x => x.Revenue ?? 0),
                ActiveCampaigns = g.Select(x => x.CampaignId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        if (stats == null)
        {
            return new TwitterAdsSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, null, 0, 0, connection?.LastSyncedAt);
        }

        var ctr = stats.TotalImpressions > 0 ? (decimal)stats.TotalClicks / stats.TotalImpressions * 100 : 0;
        var cpc = stats.TotalClicks > 0 ? stats.TotalSpend / stats.TotalClicks : 0;
        var cpa = stats.TotalConversions > 0 ? stats.TotalSpend / stats.TotalConversions : (decimal?)null;
        var roas = stats.TotalSpend > 0 ? stats.TotalRevenue / stats.TotalSpend : 0;

        return new TwitterAdsSummaryDto(
            stats.TotalSpend,
            stats.TotalRevenue,
            stats.TotalImpressions,
            stats.TotalClicks,
            0, // Total engagements
            stats.TotalConversions,
            ctr,
            cpc,
            cpa,
            roas,
            stats.ActiveCampaigns,
            connection?.LastSyncedAt
        );
    }

    public async Task<bool> RefreshTokensAsync(string shopDomain)
    {
        var connection = await _db.Set<TwitterAdsConnection>()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null || string.IsNullOrEmpty(connection.RefreshToken))
        {
            return false;
        }

        var clientId = _configuration["Twitter:ClientId"];
        var clientSecret = _configuration["Twitter:ClientSecret"];
        var refreshToken = _encryptionService.Decrypt(connection.RefreshToken);

        var client = _httpClientFactory.CreateClient();

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        try
        {
            var response = await client.PostAsync(OAuthTokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh Twitter token: {Response}", json);
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
                root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 7200);
            connection.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Twitter tokens");
            return false;
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static decimal GetMetricSum(JsonElement metrics, string metricName)
    {
        if (!metrics.TryGetProperty(metricName, out var metricArray))
            return 0;

        if (metricArray.ValueKind == JsonValueKind.Array)
        {
            decimal sum = 0;
            foreach (var val in metricArray.EnumerateArray())
            {
                if (val.ValueKind == JsonValueKind.Number)
                {
                    sum += val.GetDecimal();
                }
                else if (val.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(val.GetString(), out var parsed))
                {
                    sum += parsed;
                }
            }
            return sum;
        }

        if (metricArray.ValueKind == JsonValueKind.Number)
        {
            return metricArray.GetDecimal();
        }

        return 0;
    }
}
