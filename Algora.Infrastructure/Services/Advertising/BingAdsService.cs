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
/// Service for managing Microsoft Advertising (Bing Ads) integration.
/// Uses Microsoft identity platform for OAuth and Bing Ads API for campaign data.
/// </summary>
public class BingAdsService : IBingAdsService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BingAdsService> _logger;

    private const string MicrosoftAuthUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string MicrosoftTokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private const string BingAdsApiUrl = "https://bingads.microsoft.com/Api/v13";
    private const string CustomerManagementUrl = "https://clientcenter.api.bingads.microsoft.com/Api/v13/CustomerManagementService.svc";

    public BingAdsService(
        AppDbContext db,
        HttpClient httpClient,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<BingAdsService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetOAuthUrl(string redirectUri, string state)
    {
        var clientId = _configuration["BingAds:ClientId"];
        var scopes = "https://ads.microsoft.com/msads.manage offline_access";

        var url = $"{MicrosoftAuthUrl}" +
                  $"?client_id={Uri.EscapeDataString(clientId ?? "")}" +
                  $"&response_type=code" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={Uri.EscapeDataString(scopes)}" +
                  $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    public async Task<BingOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri)
    {
        var clientId = _configuration["BingAds:ClientId"];
        var clientSecret = _configuration["BingAds:ClientSecret"];

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId ?? "",
            ["client_secret"] = clientSecret ?? "",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await _httpClient.PostAsync(MicrosoftTokenUrl, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to exchange Bing Ads code: {Response}", json);
            return null;
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
        return new BingOAuthTokenResponse(
            AccessToken: tokenData.GetProperty("access_token").GetString() ?? "",
            RefreshToken: tokenData.GetProperty("refresh_token").GetString() ?? "",
            ExpiresIn: tokenData.GetProperty("expires_in").GetInt32(),
            TokenType: tokenData.GetProperty("token_type").GetString() ?? "Bearer",
            Scope: tokenData.TryGetProperty("scope", out var scope) ? scope.GetString() ?? "" : ""
        );
    }

    public async Task<BingAdsConnectionDto?> GetConnectionAsync(string shopDomain)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        return new BingAdsConnectionDto(
            Id: connection.Id,
            ShopDomain: connection.ShopDomain,
            AccountId: connection.AccountId,
            AccountName: connection.AccountName,
            CustomerId: connection.CustomerId,
            IsConnected: connection.IsConnected,
            ConnectedAt: connection.ConnectedAt,
            LastSyncedAt: connection.LastSyncedAt,
            LastSyncError: connection.LastSyncError
        );
    }

    public async Task SaveConnectionAsync(string shopDomain, SaveBingAdsConnectionDto dto)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null)
        {
            connection = new BingAdsConnection
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.BingAdsConnections.Add(connection);
        }

        connection.AccessToken = _encryption.Encrypt(dto.AccessToken);
        connection.RefreshToken = _encryption.Encrypt(dto.RefreshToken);
        connection.AccountId = dto.AccountId;
        connection.AccountName = dto.AccountName;
        connection.CustomerId = dto.CustomerId;
        connection.IsConnected = true;
        connection.ConnectedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = DateTime.UtcNow.AddHours(1);
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string shopDomain)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection != null)
        {
            connection.IsConnected = false;
            connection.AccessToken = null;
            connection.RefreshToken = null;
            connection.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<BingAdsAccountDto>> GetAccountsAsync(string accessToken)
    {
        var accounts = new List<BingAdsAccountDto>();

        try
        {
            // Use SOAP API to get accounts - simplified for REST-like approach
            var request = new HttpRequestMessage(HttpMethod.Post, CustomerManagementUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("DeveloperToken", _configuration["BingAds:DeveloperToken"]);

            // Build SOAP request for GetAccountsInfo
            var soapBody = @"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Header>
    <AuthenticationToken xmlns=""https://bingads.microsoft.com/Customer/v13"">" + accessToken + @"</AuthenticationToken>
    <DeveloperToken xmlns=""https://bingads.microsoft.com/Customer/v13"">" + _configuration["BingAds:DeveloperToken"] + @"</DeveloperToken>
  </s:Header>
  <s:Body>
    <GetAccountsInfoRequest xmlns=""https://bingads.microsoft.com/Customer/v13"">
      <CustomerId xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" i:nil=""true""/>
      <OnlyParentAccounts>false</OnlyParentAccounts>
    </GetAccountsInfoRequest>
  </s:Body>
</s:Envelope>";

            request.Content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            request.Content.Headers.Add("SOAPAction", "GetAccountsInfo");

            var response = await _httpClient.SendAsync(request);
            var xml = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse SOAP response (simplified - in production use proper XML parsing)
                // For now, return mock data structure
                _logger.LogInformation("Got accounts response from Bing Ads API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Bing Ads accounts");
        }

        return accounts;
    }

    public async Task<BingAdsSyncResultDto> SyncCampaignsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.IsConnected);

        if (connection == null)
        {
            return new BingAdsSyncResultDto(false, 0, 0, 0, "Not connected", DateTime.UtcNow);
        }

        try
        {
            // Refresh tokens if needed
            if (connection.TokenExpiresAt <= DateTime.UtcNow)
            {
                var refreshed = await RefreshTokensAsync(shopDomain);
                if (!refreshed)
                {
                    return new BingAdsSyncResultDto(false, 0, 0, 0, "Failed to refresh tokens", DateTime.UtcNow);
                }
                // Reload connection with new tokens
                connection = await _db.BingAdsConnections.FirstAsync(c => c.ShopDomain == shopDomain);
            }

            var accessToken = _encryption.Decrypt(connection.AccessToken!);
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            int processed = 0, created = 0, updated = 0;

            // Fetch campaign performance data using Reporting API
            var campaigns = await FetchCampaignPerformanceAsync(accessToken, connection, start, end);

            foreach (var campaign in campaigns)
            {
                processed++;

                // Store in unified AdsSpends table
                var existing = await _db.AdsSpends.FirstOrDefaultAsync(a =>
                    a.ShopDomain == shopDomain &&
                    a.Platform == "BingAds" &&
                    a.CampaignId == campaign.CampaignId &&
                    a.SpendDate == campaign.DateStart);

                if (existing == null)
                {
                    _db.AdsSpends.Add(new AdsSpend
                    {
                        ShopDomain = shopDomain,
                        Platform = "BingAds",
                        CampaignId = campaign.CampaignId,
                        CampaignName = campaign.CampaignName,
                        SpendDate = campaign.DateStart,
                        Amount = campaign.Spend,
                        Impressions = (int)campaign.Impressions,
                        Clicks = (int)campaign.Clicks,
                        Conversions = campaign.Conversions,
                        Revenue = campaign.ConversionValue,
                        CreatedAt = DateTime.UtcNow
                    });
                    created++;
                }
                else
                {
                    existing.Amount = campaign.Spend;
                    existing.Impressions = (int)campaign.Impressions;
                    existing.Clicks = (int)campaign.Clicks;
                    existing.Conversions = campaign.Conversions;
                    existing.Revenue = campaign.ConversionValue;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
            }

            connection.LastSyncedAt = DateTime.UtcNow;
            connection.LastSyncError = null;
            await _db.SaveChangesAsync();

            return new BingAdsSyncResultDto(true, processed, created, updated, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Bing Ads campaigns for {ShopDomain}", shopDomain);
            connection.LastSyncError = ex.Message;
            await _db.SaveChangesAsync();
            return new BingAdsSyncResultDto(false, 0, 0, 0, ex.Message, DateTime.UtcNow);
        }
    }

    private async Task<List<BingAdsCampaignDto>> FetchCampaignPerformanceAsync(
        string accessToken,
        BingAdsConnection connection,
        DateTime startDate,
        DateTime endDate)
    {
        var campaigns = new List<BingAdsCampaignDto>();

        try
        {
            // Use Reporting API to get campaign performance
            var reportingUrl = "https://reporting.api.bingads.microsoft.com/Api/v13/Reporting/SubmitGenerateReport";

            var request = new HttpRequestMessage(HttpMethod.Post, reportingUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("DeveloperToken", _configuration["BingAds:DeveloperToken"]);
            request.Headers.Add("CustomerId", connection.CustomerId);
            request.Headers.Add("AccountId", connection.AccountId);

            // Build SOAP request for CampaignPerformanceReport
            var soapBody = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Header>
    <AuthenticationToken xmlns=""https://bingads.microsoft.com/Reporting/v13"">{accessToken}</AuthenticationToken>
    <DeveloperToken xmlns=""https://bingads.microsoft.com/Reporting/v13"">{_configuration["BingAds:DeveloperToken"]}</DeveloperToken>
    <CustomerId xmlns=""https://bingads.microsoft.com/Reporting/v13"">{connection.CustomerId}</CustomerId>
    <AccountId xmlns=""https://bingads.microsoft.com/Reporting/v13"">{connection.AccountId}</AccountId>
  </s:Header>
  <s:Body>
    <SubmitGenerateReportRequest xmlns=""https://bingads.microsoft.com/Reporting/v13"">
      <ReportRequest xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" i:type=""CampaignPerformanceReportRequest"">
        <ExcludeColumnHeaders>false</ExcludeColumnHeaders>
        <ExcludeReportFooter>true</ExcludeReportFooter>
        <ExcludeReportHeader>true</ExcludeReportHeader>
        <Format>Csv</Format>
        <ReportName>CampaignPerformance</ReportName>
        <ReturnOnlyCompleteData>false</ReturnOnlyCompleteData>
        <Aggregation>Daily</Aggregation>
        <Columns>
          <CampaignPerformanceReportColumn>CampaignId</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>CampaignName</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>CampaignType</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>CampaignStatus</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Spend</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Impressions</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Clicks</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Conversions</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Revenue</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>Ctr</CampaignPerformanceReportColumn>
          <CampaignPerformanceReportColumn>AverageCpc</CampaignPerformanceReportColumn>
        </Columns>
        <Scope>
          <AccountIds xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
            <a:long>{connection.AccountId}</a:long>
          </AccountIds>
        </Scope>
        <Time>
          <CustomDateRangeStart>
            <Day>{startDate.Day}</Day>
            <Month>{startDate.Month}</Month>
            <Year>{startDate.Year}</Year>
          </CustomDateRangeStart>
          <CustomDateRangeEnd>
            <Day>{endDate.Day}</Day>
            <Month>{endDate.Month}</Month>
            <Year>{endDate.Year}</Year>
          </CustomDateRangeEnd>
        </Time>
      </ReportRequest>
    </SubmitGenerateReportRequest>
  </s:Body>
</s:Envelope>";

            request.Content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            request.Content.Headers.Add("SOAPAction", "SubmitGenerateReport");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse report response and poll for completion
                // In production, implement proper report polling and CSV parsing
                _logger.LogInformation("Submitted Bing Ads report request");
            }
            else
            {
                _logger.LogWarning("Bing Ads report request failed: {Response}", responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Bing Ads campaign performance");
        }

        return campaigns;
    }

    public async Task<List<BingAdsCampaignDto>> GetCampaignsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var campaigns = new List<BingAdsCampaignDto>();

        var spends = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.Platform == "BingAds" &&
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

        foreach (var s in spends)
        {
            var ctr = s.Impressions > 0 ? (decimal)s.Clicks / s.Impressions * 100 : 0;
            var cpc = s.Clicks > 0 ? s.Spend / s.Clicks : 0;
            var cpa = s.Conversions > 0 ? s.Spend / s.Conversions : (decimal?)null;
            var roas = s.Spend > 0 ? s.Revenue / s.Spend : (decimal?)null;

            campaigns.Add(new BingAdsCampaignDto(
                CampaignId: s.CampaignId ?? "",
                CampaignName: s.CampaignName ?? "",
                CampaignType: "Search",
                Status: "Active",
                Budget: 0,
                BudgetType: "DailyBudgetStandard",
                Spend: s.Spend,
                Impressions: s.Impressions,
                Clicks: s.Clicks,
                Conversions: s.Conversions,
                ConversionValue: s.Revenue,
                Ctr: ctr,
                Cpc: cpc,
                Cpa: cpa,
                Roas: roas,
                DateStart: startDate,
                DateEnd: endDate
            ));
        }

        return campaigns;
    }

    public async Task<List<BingAdsAdGroupDto>> GetAdGroupsAsync(string shopDomain, string campaignId)
    {
        // Ad groups would be fetched similarly via the Reporting API
        return new List<BingAdsAdGroupDto>();
    }

    public async Task<BingAdsSummaryDto?> GetSummaryAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection == null) return null;

        var spends = await _db.AdsSpends
            .Where(a => a.ShopDomain == shopDomain &&
                       a.Platform == "BingAds" &&
                       a.SpendDate >= startDate &&
                       a.SpendDate <= endDate)
            .ToListAsync();

        var totalSpend = spends.Sum(s => s.Amount);
        var totalRevenue = spends.Sum(s => s.Revenue ?? 0);
        var totalImpressions = spends.Sum(s => s.Impressions ?? 0);
        var totalClicks = spends.Sum(s => s.Clicks ?? 0);
        var totalConversions = spends.Sum(s => s.Conversions ?? 0);

        var ctr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
        var cpc = totalClicks > 0 ? totalSpend / totalClicks : 0;
        var cpa = totalConversions > 0 ? totalSpend / totalConversions : (decimal?)null;
        var roas = totalSpend > 0 ? totalRevenue / totalSpend : 0;

        var activeCampaigns = spends.Select(s => s.CampaignId).Distinct().Count();

        return new BingAdsSummaryDto(
            TotalSpend: totalSpend,
            TotalConversionValue: totalRevenue,
            TotalImpressions: totalImpressions,
            TotalClicks: totalClicks,
            TotalConversions: totalConversions,
            Ctr: ctr,
            Cpc: cpc,
            Cpa: cpa,
            Roas: roas,
            ActiveCampaigns: activeCampaigns,
            SearchCampaigns: activeCampaigns, // Simplified
            ShoppingCampaigns: 0,
            AudienceCampaigns: 0,
            LastSyncedAt: connection.LastSyncedAt
        );
    }

    public async Task<bool> RefreshTokensAsync(string shopDomain)
    {
        var connection = await _db.BingAdsConnections
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain);

        if (connection?.RefreshToken == null) return false;

        try
        {
            var clientId = _configuration["BingAds:ClientId"];
            var clientSecret = _configuration["BingAds:ClientSecret"];
            var refreshToken = _encryption.Decrypt(connection.RefreshToken);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId ?? "",
                ["client_secret"] = clientSecret ?? "",
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            });

            var response = await _httpClient.PostAsync(MicrosoftTokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh Bing Ads tokens: {Response}", json);
                return false;
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            connection.AccessToken = _encryption.Encrypt(tokenData.GetProperty("access_token").GetString()!);
            if (tokenData.TryGetProperty("refresh_token", out var newRefresh))
            {
                connection.RefreshToken = _encryption.Encrypt(newRefresh.GetString()!);
            }
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32());
            connection.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Bing Ads tokens for {ShopDomain}", shopDomain);
            return false;
        }
    }
}
