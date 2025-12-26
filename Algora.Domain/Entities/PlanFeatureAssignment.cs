namespace Algora.Domain.Entities
{
    /// <summary>
    /// Junction table mapping features to plans.
    /// Enables dynamic feature assignment without schema changes.
    /// </summary>
    public class PlanFeatureAssignment
    {
        /// <summary>
        /// Primary key for the assignment record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Plan.
        /// </summary>
        public int PlanId { get; set; }

        /// <summary>
        /// Navigation property to the Plan.
        /// </summary>
        public Plan Plan { get; set; } = null!;

        /// <summary>
        /// Foreign key to the PlanFeature.
        /// </summary>
        public int PlanFeatureId { get; set; }

        /// <summary>
        /// Navigation property to the PlanFeature.
        /// </summary>
        public PlanFeature PlanFeature { get; set; } = null!;

        /// <summary>
        /// When this assignment was created.
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who assigned this feature (admin email or system).
        /// </summary>
        public string? AssignedBy { get; set; }
    }
}
