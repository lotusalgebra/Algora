using Algora.Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Shopify
{
    public class ShopContext : IShopContext
    {
        private readonly ShopifyOptions _options;

        public ShopContext(IOptions<ShopifyOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        // For single-store mode, derive from AppUrl or set via config
        public string ShopDomain =>
            _options.AppUrl
            .Replace("https://", "")
            .Replace("http://", "")
            .TrimEnd('/');

        // Access token from environment or secrets for security
        public string AccessToken =>
            Environment.GetEnvironmentVariable("SHOPIFY_ACCESS_TOKEN")
            ?? throw new InvalidOperationException("Shopify access token not set. Please configure SHOPIFY_ACCESS_TOKEN environment variable.");
    }
}
