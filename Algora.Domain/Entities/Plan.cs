namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a subscription plan with pricing, limits, and feature access.
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// Primary key for the plan record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique plan name (e.g., "Free", "Premium", "Enterprise").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the plan.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Monthly price for the plan (0 for free tier).
        /// </summary>
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Maximum number of orders allowed per month. -1 means unlimited.
        /// </summary>
        public int OrderLimit { get; set; }

        /// <summary>
        /// Maximum number of products allowed. -1 means unlimited.
        /// </summary>
        public int ProductLimit { get; set; }

        /// <summary>
        /// Maximum number of customers allowed. -1 means unlimited.
        /// </summary>
        public int CustomerLimit { get; set; }

        /// <summary>
        /// Whether WhatsApp messaging feature is available.
        /// </summary>
        public bool HasWhatsApp { get; set; }

        /// <summary>
        /// Whether email campaign feature is available.
        /// </summary>
        public bool HasEmailCampaigns { get; set; }

        /// <summary>
        /// Whether SMS messaging feature is available.
        /// </summary>
        public bool HasSms { get; set; }

        /// <summary>
        /// Whether advanced reporting feature is available.
        /// </summary>
        public bool HasAdvancedReports { get; set; }

        /// <summary>
        /// Whether API access is available.
        /// </summary>
        public bool HasApiAccess { get; set; }

        /// <summary>
        /// Display order for the plan in listings.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether the plan is currently available for selection.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Number of trial days offered for this plan (0 for no trial).
        /// </summary>
        public int TrialDays { get; set; }
    }
}
