namespace Algora.Domain.Entities
{
    /// <summary>
    /// Tracks A/B test variant assignment and conversion results per enrollment.
    /// </summary>
    public class ABTestResult
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public EmailAutomationEnrollment Enrollment { get; set; } = null!;
        public int VariantId { get; set; }
        public ABTestVariant Variant { get; set; } = null!;
        public bool Opened { get; set; }
        public bool Clicked { get; set; }
        public bool Converted { get; set; }
        public decimal? ConversionValue { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClickedAt { get; set; }
        public DateTime? ConvertedAt { get; set; }
    }
}
