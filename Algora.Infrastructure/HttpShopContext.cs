using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Algora.Infrastructure
{
    public class HttpShopContext : IShopContext
    {
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _config;

        public HttpShopContext(IHttpContextAccessor http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public string ShopDomain
        {
            get
            {
                // priority: query ?shop=, header X-Shop-Domain, fallback config
                var ctx = _http.HttpContext;
                var q = ctx?.Request.Query["shop"].ToString();
                if (!string.IsNullOrWhiteSpace(q)) return q;
                var h = ctx?.Request.Headers["X-Shop-Domain"].ToString();
                if (!string.IsNullOrWhiteSpace(h)) return h;
                return _config["Shopify:ShopDomain"] ?? "your-shop.myshopify.com";
            }
        }

        public string AccessToken
        {
            get
            {
                var ctx = _http.HttpContext;
                var h = ctx?.Request.Headers["X-Shopify-Access-Token"].ToString();
                if (!string.IsNullOrWhiteSpace(h)) return h;
                return _config["Shopify:AccessToken"] ?? "shpat_xxx";
            }
        }
    }
}
