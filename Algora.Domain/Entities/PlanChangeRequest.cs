namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a request to change subscription plan (upgrade or downgrade).
    /// Downgrades require admin approval while upgrades can be processed immediately.
    /// </summary>
    public class PlanChangeRequest
    {
        /// <summary>
        /// Primary key for the request record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The shop's myshopify domain making the request.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// The plan name the shop is currently on.
        /// </summary>
        public string CurrentPlanName { get; set; } = string.Empty;

        /// <summary>
        /// The plan name the shop wants to change to.
        /// </summary>
        public string RequestedPlanName { get; set; } = string.Empty;

        /// <summary>
        /// Type of change: "upgrade" or "downgrade".
        /// </summary>
        public string RequestType { get; set; } = string.Empty;

        /// <summary>
        /// Status of the request: "pending", "approved", "rejected".
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Optional notes from admin when processing the request.
        /// </summary>
        public string? AdminNotes { get; set; }

        /// <summary>
        /// UTC timestamp when the request was created.
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the request was approved or rejected.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Email of the admin who processed the request.
        /// </summary>
        public string? ProcessedBy { get; set; }
    }
}
