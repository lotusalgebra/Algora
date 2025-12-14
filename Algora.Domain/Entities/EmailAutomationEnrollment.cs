namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a customer enrolled in an automation workflow.
    /// </summary>
    public class EmailAutomationEnrollment
    {
        public int Id { get; set; }
        public int AutomationId { get; set; }
        public EmailAutomation Automation { get; set; } = null!;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? SubscriberId { get; set; }
        public EmailSubscriber? Subscriber { get; set; }
        public string Email { get; set; } = string.Empty;
        public int CurrentStepId { get; set; }
        public string Status { get; set; } = "active"; // active, completed, exited, paused
        public DateTime? NextStepAt { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExitedAt { get; set; }
        public string? ExitReason { get; set; }
    }
}