using System;

namespace Algora.Infrastructure.Shopify
{
    /// <summary>
    /// Minimal filter used when listing abandoned checkouts.
    /// Matches the usage in AbandonedCartService and the placeholder AbandonedCheckoutService.
    /// Extend with additional properties if your real Shopify implementation requires them.
    /// </summary>
    public class AbandonedCheckoutListFilter
    {
        public int? Limit { get; set; }
        public DateTime? CreatedAtMin { get; set; }
        // Add other filter properties as needed (e.g. PageInfo, SinceId, etc.)
    }
}