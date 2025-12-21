namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an automated email workflow/sequence.
    /// </summary>
    public class EmailAutomation
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TriggerType { get; set; } = string.Empty; // welcome, abandoned_cart, post_purchase, birthday, winback, etc.
        public string? TriggerConditions { get; set; } // JSON
        public bool IsActive { get; set; }
        public int TotalEnrolled { get; set; }
        public int TotalCompleted { get; set; }
        public decimal? Revenue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<EmailAutomationStep> Steps { get; set; } = new List<EmailAutomationStep>();
        public ICollection<EmailAutomationEnrollment> Enrollments { get; set; } = new List<EmailAutomationEnrollment>();
    }
}