using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure;

public class HttpShopContext : IShopContext
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<HttpShopContext> _logger;
    private string? _cachedAccessToken;

    public HttpShopContext(IHttpContextAccessor http, IConfiguration config, AppDbContext db, ILogger<HttpShopContext> logger)
    {
        _http = http;
        _config = config;
        _db = db;
        _logger = logger;
    }

    public string ShopDomain
    {
        get
        {
            var ctx = _http.HttpContext;

            // 1. Query string ?shop=
            var q = ctx?.Request.Query["shop"].ToString();
            if (!string.IsNullOrWhiteSpace(q)) return q;

            // 2. Header X-Shop-Domain
            var h = ctx?.Request.Headers["X-Shop-Domain"].ToString();
            if (!string.IsNullOrWhiteSpace(h)) return h;

            // 3. User claims (from authenticated user)
            var shopClaim = ctx?.User?.FindFirst("shop_domain")?.Value;
            if (!string.IsNullOrWhiteSpace(shopClaim)) return shopClaim;

            // 4. Fallback to config
            return _config["Shopify:ShopDomain"] ?? "your-shop.myshopify.com";
        }
    }

    public string AccessToken
    {
        get
        {
            var ctx = _http.HttpContext;

            // 1. Header X-Shopify-Access-Token
            var h = ctx?.Request.Headers["X-Shopify-Access-Token"].ToString();
            if (!string.IsNullOrWhiteSpace(h))
            {
                _logger.LogDebug("Using access token from header");
                return h;
            }

            // 2. Try to get from database based on ShopDomain
            if (_cachedAccessToken is null)
            {
                var shopDomain = ShopDomain;
                _logger.LogDebug("Looking up access token for shop: {ShopDomain}", shopDomain);

                var shop = _db.Shops
                    .AsNoTracking()
                    .FirstOrDefault(s => s.Domain == shopDomain);

                if (shop != null)
                {
                    _logger.LogDebug("Found shop in database: {ShopDomain}, has token: {HasToken}",
                        shop.Domain, !string.IsNullOrEmpty(shop.OfflineAccessToken));
                }
                else
                {
                    _logger.LogWarning("Shop not found in database: {ShopDomain}", shopDomain);
                }

                _cachedAccessToken = shop?.OfflineAccessToken;
            }

            if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
                return _cachedAccessToken;

            // 3. Fallback to config
            var configToken = _config["Shopify:AccessToken"];
            if (!string.IsNullOrWhiteSpace(configToken))
            {
                _logger.LogDebug("Using access token from config");
                return configToken;
            }

            throw new InvalidOperationException(
                $"No access token available for shop '{ShopDomain}'. Please install the app first.");
        }
    }
}
