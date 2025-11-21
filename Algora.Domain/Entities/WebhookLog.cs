using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a received webhook event recorded for auditing, debugging and retry purposes.
    /// Store the raw payload and headers (if needed) so webhooks can be inspected or replayed later.
    /// </summary>
    public class WebhookLog
    {
        /// <summary>
        /// Primary key for the webhook log entry.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The shop domain that sent the webhook (for example: "example-shop.myshopify.com").
        /// Use this to correlate the webhook to a persisted Shop record.
        /// </summary>
        public string Shop { get; set; } = string.Empty;

        /// <summary>
        /// The webhook topic or event name (for example: "orders/create", "app/uninstalled").
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Raw webhook payload body (usually JSON). Keep the original payload so it can be inspected
        /// or re-processed if needed. Consider truncation or compression for extremely large payloads.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when the webhook was received by this application.
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
