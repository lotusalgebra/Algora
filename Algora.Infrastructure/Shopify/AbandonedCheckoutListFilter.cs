using System;

namespace Algora.Infrastructure.Shopify
{
    /// <summary>
    /// Filter used when listing abandoned checkouts from Shopify.
    /// Maps to Shopify's CheckoutListFilter parameters.
    /// </summary>
    public class AbandonedCheckoutListFilter
    {
        /// <summary>
        /// Maximum number of checkouts to return.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Show checkouts created at or after this date.
        /// </summary>
        public DateTime? CreatedAtMin { get; set; }

        /// <summary>
        /// Show checkouts created at or before this date.
        /// </summary>
        public DateTime? CreatedAtMax { get; set; }

        /// <summary>
        /// Show checkouts last updated at or after this date.
        /// </summary>
        public DateTime? UpdatedAtMin { get; set; }

        /// <summary>
        /// Show checkouts last updated at or before this date.
        /// </summary>
        public DateTime? UpdatedAtMax { get; set; }

        /// <summary>
        /// Filter checkouts by status: "open" (abandoned), "closed" (completed).
        /// Default is "open" for abandoned checkouts.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Restrict results to after the specified ID.
        /// </summary>
        public long? SinceId { get; set; }
    }
}
