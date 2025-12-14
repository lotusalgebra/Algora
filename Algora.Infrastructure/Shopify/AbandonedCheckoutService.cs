using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShopifySharp.Filters;

namespace Algora.Infrastructure.Shopify
{
    // Minimal local model used by AbandonedCartService to allow compilation.
    // This is a lightweight placeholder. Replace with a real implementation that
    // calls the Shopify API (REST or GraphQL) to fetch abandoned checkouts.
    public class AbandonedCheckout
    {
        public long? Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        // Some SDKs expose TotalPrice as decimal? or string; keep string so TryParse works like existing code.
        public string? TotalPrice { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public string? AbandonedCheckoutUrl { get; set; }

        // Lightweight line item model
        public List<AbandonedCheckoutLineItem> LineItems { get; set; } = new();
        public AbandonedCheckoutCustomer? Customer { get; set; }
    }

    public class AbandonedCheckoutLineItem
    {
        public string? Title { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    public class AbandonedCheckoutCustomer
    {
        public long? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Placeholder AbandonedCheckoutService used by the app until a full Shopify-backed implementation is provided.
    /// Provides the methods used by <see cref="Algora.Infrastructure.Shopify.AbandonedCartService"/>.
    /// </summary>
    public class AbandonedCheckoutService
    {
        private readonly string _shopDomain;
        private readonly string _accessToken;

        public AbandonedCheckoutService(string shopDomain, string accessToken)
        {
            _shopDomain = shopDomain;
            _accessToken = accessToken;
        }

        // NOTE: These implementations return empty results to keep the app compiling.
        // Replace with real calls to Shopify (REST or GraphQL) to fetch abandoned checkout data.
        public Task<List<AbandonedCheckout>> ListAsync(AbandonedCheckoutListFilter filter)
        {
            return Task.FromResult(new List<AbandonedCheckout>());
        }

        public Task<AbandonedCheckout?> GetAsync(long id)
        {
            return Task.FromResult<AbandonedCheckout?>(null);
        }
    }
}