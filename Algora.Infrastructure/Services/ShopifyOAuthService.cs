using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Algora.Infrastructure.Services;

public class ShopifyOAuthService : IShopifyOAuthService
{
    private readonly ShopifyOptions _defaultOptions;
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<ShopifyOAuthService> _logger;

    public ShopifyOAuthService(
        IOptions<ShopifyOptions> defaultOptions,
        AppDbContext db,
        IHttpClientFactory httpFactory,
        ILogger<ShopifyOAuthService> logger)
    {
        _defaultOptions = defaultOptions.Value;
        _db = db;
        _http = httpFactory.CreateClient();
        _logger = logger;
    }

    /// <summary>
    /// Gets effective credentials for a shop (custom or app default).
    /// </summary>
    private async Task<(string ApiKey, string ApiSecret, string Scopes, string AppUrl)> GetCredentialsAsync(string shopDomain)
    {
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Domain == shopDomain);

        if (shop is not null && shop.UseCustomCredentials &&
            !string.IsNullOrEmpty(shop.CustomApiKey) &&
            !string.IsNullOrEmpty(shop.CustomApiSecret))
        {
            return (
                shop.CustomApiKey,
                shop.CustomApiSecret,
                shop.CustomScopes ?? _defaultOptions.Scopes,
                shop.CustomAppUrl ?? _defaultOptions.AppUrl
            );
        }

        return (_defaultOptions.ApiKey, _defaultOptions.ApiSecret, _defaultOptions.Scopes, _defaultOptions.AppUrl);
    }

    public async Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code)
    {
        var (apiKey, apiSecret, _, _) = await GetCredentialsAsync(shopDomain);

        var url = $"https://{shopDomain}/admin/oauth/access_token";
        var payload = new { client_id = apiKey, client_secret = apiSecret, code };

        var resp = await _http.PostAsJsonAsync(url, payload);
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("access_token").GetString()!;

        // Also get associated scopes
        var scopes = doc.RootElement.TryGetProperty("scope", out var scopeElement)
            ? scopeElement.GetString() : null;

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        if (shop is null)
        {
            shop = new Shop
            {
                Domain = shopDomain,
                OfflineAccessToken = token,
                InstalledAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Shops.Add(shop);
            _logger.LogInformation("New shop installed: {ShopDomain}", shopDomain);
        }
        else
        {
            shop.OfflineAccessToken = token;
            shop.IsActive = true;
            shop.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Shop reinstalled/updated: {ShopDomain}", shopDomain);
        }

        await _db.SaveChangesAsync();

        // Sync shop info after install
        await SyncShopInfoAsync(shopDomain, token);

        return token;
    }

    public async Task<string?> GetAccessTokenAsync(string shopDomain)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain && s.IsActive);
        return shop?.OfflineAccessToken;
    }

    /// <summary>
    /// Gets the OAuth authorization URL for a shop.
    /// </summary>
    public async Task<string> GetAuthorizationUrlAsync(string shopDomain, string state)
    {
        var (apiKey, _, scopes, appUrl) = await GetCredentialsAsync(shopDomain);
        var redirectUri = appUrl.TrimEnd('/') + "/auth/callback";

        return $"https://{shopDomain}/admin/oauth/authorize" +
               $"?client_id={apiKey}" +
               $"&scope={Uri.EscapeDataString(scopes)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={state}";
    }

    /// <summary>
    /// Validates HMAC signature using shop-specific credentials.
    /// </summary>
    public async Task<bool> ValidateHmacAsync(string shopDomain, string message, string hmac)
    {
        var (_, apiSecret, _, _) = await GetCredentialsAsync(shopDomain);

        using var hasher = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(apiSecret));
        var hashBytes = hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
        var calculated = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return calculated == hmac;
    }

    private async Task SyncShopInfoAsync(string shopDomain, string accessToken)
    {
        try
        {
            var url = $"https://{shopDomain}/admin/api/2024-01/shop.json";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Shopify-Access-Token", accessToken);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var shopInfo = doc.RootElement.GetProperty("shop");

            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
            if (shop is null) return;

            shop.ShopName = shopInfo.TryGetProperty("name", out var name) ? name.GetString() : null;
            shop.Email = shopInfo.TryGetProperty("email", out var email) ? email.GetString() : null;
            shop.PrimaryLocale = shopInfo.TryGetProperty("primary_locale", out var locale) ? locale.GetString() : null;
            shop.Timezone = shopInfo.TryGetProperty("timezone", out var tz) ? tz.GetString() : null;
            shop.Currency = shopInfo.TryGetProperty("currency", out var currency) ? currency.GetString() : null;
            shop.Country = shopInfo.TryGetProperty("country_code", out var country) ? country.GetString() : null;
            shop.PlanName = shopInfo.TryGetProperty("plan_name", out var plan) ? plan.GetString() : null;
            shop.LastSyncedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Synced shop info for {ShopDomain}", shopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync shop info for {ShopDomain}", shopDomain);
        }
    }
}
