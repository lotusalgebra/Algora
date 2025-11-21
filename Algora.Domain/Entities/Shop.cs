using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a persisted shop record for an installed merchant.
    /// Stores identifying information and the offline access token issued by Shopify.
    /// </summary>
    public class Shop
    {
        /// <summary>
        /// Primary identifier for the shop record in the local database.
        /// Generated as a GUID to support distributed systems and avoid exposing platform ids.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The shop's myshopify domain (for example: "example-shop.myshopify.com").
        /// Used as the canonical key to identify the store across API calls and webhooks.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// The offline access token returned by Shopify after the OAuth install flow.
        /// This token permits server-side Admin API calls even when the merchant is not online.
        /// Treat this value as sensitive: encrypt in storage or protect with appropriate secrets management.
        /// </summary>
        public string? OfflineAccessToken { get; set; }

        /// <summary>
        /// UTC timestamp when the app was installed for this shop.
        /// Useful for auditing, billing reconciliations and cleanup logic.
        /// </summary>
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    }
}
