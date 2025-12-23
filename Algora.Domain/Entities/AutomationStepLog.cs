namespace Algora.Domain.Entities
{
    /// <summary>
    /// Logs each step execution in an automation workflow for tracking and analytics.
    /// </summary>
    public class AutomationStepLog
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public EmailAutomationEnrollment Enrollment { get; set; } = null!;
        public int StepId { get; set; }
        public EmailAutomationStep Step { get; set; } = null!;
        public string Status { get; set; } = "pending"; // pending, sent, delivered, failed, skipped
        public string? Channel { get; set; } // email, sms, whatsapp
        public string? ExternalMessageId { get; set; } // provider message ID for tracking
        public string? ErrorMessage { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClickedAt { get; set; }
        public DateTime? BouncedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
