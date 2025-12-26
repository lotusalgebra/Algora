namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a feature that can be assigned to subscription plans.
    /// Features are managed dynamically and can be added/removed without schema changes.
    /// </summary>
    public class PlanFeature
    {
        /// <summary>
        /// Primary key for the feature record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique code/key for the feature (e.g., "whatsapp", "email_campaigns", "ai_chatbot").
        /// Used in code to check feature access.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable display name (e.g., "WhatsApp Integration", "Email Campaigns").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what this feature provides.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category for grouping features (e.g., "Communication", "AI Tools", "Operations").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Icon class for UI display (e.g., "fas fa-envelope", "fas fa-robot").
        /// </summary>
        public string? IconClass { get; set; }

        /// <summary>
        /// Display order within category for sorting.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this feature is currently available for assignment.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this feature was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for plan assignments.
        /// </summary>
        public ICollection<PlanFeatureAssignment> PlanAssignments { get; set; } = new List<PlanFeatureAssignment>();
    }
}
