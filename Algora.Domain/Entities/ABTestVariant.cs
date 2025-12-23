namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a variant in an A/B test for automation emails.
    /// </summary>
    public class ABTestVariant
    {
        public int Id { get; set; }
        public int AutomationId { get; set; }
        public EmailAutomation Automation { get; set; } = null!;
        public int? StepId { get; set; }
        public EmailAutomationStep? Step { get; set; }
        public string VariantName { get; set; } = string.Empty; // A, B, C, or Control
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public int Weight { get; set; } = 50; // percentage weight for distribution
        public bool IsControl { get; set; }
        public int Impressions { get; set; }
        public int Opens { get; set; }
        public int Clicks { get; set; }
        public int Conversions { get; set; }
        public decimal Revenue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ABTestResult> Results { get; set; } = new List<ABTestResult>();

        // Calculated properties
        public decimal OpenRate => Impressions > 0 ? (decimal)Opens / Impressions * 100 : 0;
        public decimal ClickRate => Opens > 0 ? (decimal)Clicks / Opens * 100 : 0;
        public decimal ConversionRate => Impressions > 0 ? (decimal)Conversions / Impressions * 100 : 0;
    }
}
