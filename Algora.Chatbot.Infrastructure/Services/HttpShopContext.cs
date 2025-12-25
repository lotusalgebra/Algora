using Algora.Chatbot.Application.Interfaces;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Algora.Chatbot.Infrastructure.Services;

public class HttpShopContext : IShopContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ChatbotDbContext _db;
    private readonly IConfiguration _configuration;
    private string? _shopDomain;
    private string? _accessToken;

    public HttpShopContext(
        IHttpContextAccessor httpContextAccessor,
        ChatbotDbContext db,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _configuration = configuration;
    }

    public string ShopDomain
    {
        get
        {
            if (_shopDomain != null) return _shopDomain;

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return string.Empty;

            // Try query string
            if (context.Request.Query.TryGetValue("shop", out var shopFromQuery))
            {
                _shopDomain = shopFromQuery.ToString();
                return _shopDomain;
            }

            // Try header
            if (context.Request.Headers.TryGetValue("X-Shop-Domain", out var shopFromHeader))
            {
                _shopDomain = shopFromHeader.ToString();
                return _shopDomain;
            }

            // Try claims
            var shopClaim = context.User?.FindFirst("shop_domain")?.Value;
            if (!string.IsNullOrEmpty(shopClaim))
            {
                _shopDomain = shopClaim;
                return _shopDomain;
            }

            // Fallback to config
            _shopDomain = _configuration["Shopify:ShopDomain"] ?? string.Empty;
            return _shopDomain;
        }
    }

    public string? AccessToken
    {
        get
        {
            if (_accessToken != null) return _accessToken;

            var context = _httpContextAccessor.HttpContext;

            // Try header
            if (context?.Request.Headers.TryGetValue("X-Shopify-Access-Token", out var tokenFromHeader) == true)
            {
                _accessToken = tokenFromHeader.ToString();
                return _accessToken;
            }

            // Try database
            if (!string.IsNullOrEmpty(ShopDomain))
            {
                var shop = _db.Shops.FirstOrDefault(s => s.Domain == ShopDomain);
                if (shop?.OfflineAccessToken != null)
                {
                    _accessToken = shop.OfflineAccessToken;
                    return _accessToken;
                }
            }

            // Fallback to config
            _accessToken = _configuration["Shopify:AccessToken"];
            return _accessToken;
        }
    }
}
