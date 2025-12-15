using Algora.Auth.Models;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Algora.Auth.Services;

public class ShopifyAuthService : IShopifyAuthService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly ShopifySettings _settings;
    private readonly IJwtService _jwtService;
    private readonly ILogger<ShopifyAuthService> _logger;

    public ShopifyAuthService(
        AppDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<ShopifySettings> settings,
        IJwtService jwtService,
        ILogger<ShopifyAuthService> logger)
    {
        _db = db;
        _http = httpFactory.CreateClient();
        _settings = settings.Value;
        _jwtService = jwtService;
        _logger = logger;
    }

    public Task<string> GetInstallUrlAsync(string shopDomain, string state)
    {
        var redirectUri = $"{_settings.AppUrl.TrimEnd('/')}/api/auth/shopify/callback";

        var url = $"https://{shopDomain}/admin/oauth/authorize" +
                  $"?client_id={_settings.ApiKey}" +
                  $"&scope={Uri.EscapeDataString(_settings.Scopes)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={state}";

        return Task.FromResult(url);
    }

    public async Task<ShopifyAuthResponse> HandleCallbackAsync(ShopifyCallbackRequest request, string savedState)
    {
        // Validate state
        if (request.State != savedState)
        {
            return new ShopifyAuthResponse { Success = false, Message = "Invalid state parameter" };
        }

        // Validate HMAC
        var queryParams = new Dictionary<string, string>
        {
            ["shop"] = request.Shop,
            ["code"] = request.Code,
            ["state"] = request.State,
            ["timestamp"] = request.Timestamp ?? ""
        };

        if (!await ValidateHmacAsync(request.Shop, queryParams, request.Hmac))
        {
            return new ShopifyAuthResponse { Success = false, Message = "Invalid HMAC signature" };
        }

        // Exchange code for access token
        var tokenUrl = $"https://{request.Shop}/admin/oauth/access_token";
        var tokenPayload = new
        {
            client_id = _settings.ApiKey,
            client_secret = _settings.ApiSecret,
            code = request.Code
        };

        try
        {
            var response = await _http.PostAsJsonAsync(tokenUrl, tokenPayload);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenResponse.GetProperty("access_token").GetString()!;
            var scope = tokenResponse.TryGetProperty("scope", out var scopeElement)
                ? scopeElement.GetString() : null;

            // Create or update shop and user
            return await CreateOrUpdateShopUserAsync(request.Shop, accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange code for token for shop {Shop}", request.Shop);
            return new ShopifyAuthResponse { Success = false, Message = "Failed to authenticate with Shopify" };
        }
    }

    public Task<bool> ValidateHmacAsync(string shopDomain, IDictionary<string, string> queryParams, string hmac)
    {
        var message = string.Join("&", queryParams
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));

        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.ApiSecret));
        var hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(message));
        var calculated = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return Task.FromResult(calculated == hmac.ToLowerInvariant());
    }

    public async Task<ShopifyAuthResponse> CreateOrUpdateShopUserAsync(string shopDomain, string accessToken)
    {
        // Get shop info from Shopify
        var shopInfo = await GetShopInfoAsync(shopDomain, accessToken);

        // Create or update shop
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
        if (shop is null)
        {
            shop = new Shop
            {
                Domain = shopDomain,
                OfflineAccessToken = accessToken,
                InstalledAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Shops.Add(shop);
            _logger.LogInformation("New shop installed: {Shop}", shopDomain);
        }
        else
        {
            shop.OfflineAccessToken = accessToken;
            shop.IsActive = true;
            shop.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Shop reinstalled: {Shop}", shopDomain);
        }

        // Update shop info
        if (shopInfo is not null)
        {
            shop.ShopName = shopInfo.Name;
            shop.Email = shopInfo.Email;
            shop.Currency = shopInfo.Currency;
            shop.Country = shopInfo.CountryCode;
            shop.Timezone = shopInfo.Timezone;
            shop.PlanName = shopInfo.PlanName;
            shop.LastSyncedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Create or get admin user for the shop
        var email = shopInfo?.Email ?? $"admin@{shopDomain}";
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.ShopDomain == shopDomain && u.Email == email);

        if (user is null)
        {
            user = new AppUser
            {
                ShopDomain = shopDomain,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                FirstName = shopInfo?.Name ?? "Shop",
                LastName = "Admin",
                Role = "Owner",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();
        }

        // Generate tokens
        var (jwtToken, refreshToken, expiresAt) = _jwtService.GenerateTokenPair(user);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ShopifyAuthResponse
        {
            Success = true,
            Message = "Shopify authentication successful",
            ShopDomain = shopDomain,
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ShopDomain = user.ShopDomain,
                Role = user.Role
            },
            RedirectUrl = "/dashboard"
        };
    }

    private async Task<ShopInfoResponse?> GetShopInfoAsync(string shopDomain, string accessToken)
    {
        try
        {
            var url = $"https://{shopDomain}/admin/api/2024-01/shop.json";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Shopify-Access-Token", accessToken);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var shopElement = json.GetProperty("shop");

            return new ShopInfoResponse
            {
                Name = shopElement.TryGetProperty("name", out var n) ? n.GetString() : null,
                Email = shopElement.TryGetProperty("email", out var e) ? e.GetString() : null,
                Currency = shopElement.TryGetProperty("currency", out var c) ? c.GetString() : null,
                CountryCode = shopElement.TryGetProperty("country_code", out var cc) ? cc.GetString() : null,
                Timezone = shopElement.TryGetProperty("timezone", out var t) ? t.GetString() : null,
                PlanName = shopElement.TryGetProperty("plan_name", out var p) ? p.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get shop info for {Shop}", shopDomain);
            return null;
        }
    }

    private record ShopInfoResponse
    {
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Currency { get; init; }
        public string? CountryCode { get; init; }
        public string? Timezone { get; init; }
        public string? PlanName { get; init; }
    }
}