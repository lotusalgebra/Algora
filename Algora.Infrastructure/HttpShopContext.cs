using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Algora.Infrastructure;

public class HttpShopContext : IShopContext
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private string? _cachedAccessToken;

    public HttpShopContext(IHttpContextAccessor http, IConfiguration config, AppDbContext db)
    {
        _http = http;
        _config = config;
        _db = db;
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
            if (!string.IsNullOrWhiteSpace(h)) return h;

            // 2. Try to get from database based on ShopDomain
            if (_cachedAccessToken is null)
            {
                var shopDomain = ShopDomain;
                var shop = _db.Shops
                    .AsNoTracking()
                    .FirstOrDefault(s => s.Domain == shopDomain);

                _cachedAccessToken = shop?.OfflineAccessToken;
            }

            if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
                return _cachedAccessToken;

            // 3. Fallback to config
            return _config["Shopify:AccessToken"] ?? throw new InvalidOperationException(
                $"No access token available for shop '{ShopDomain}'. Please install the app first.");
        }
    }
}
