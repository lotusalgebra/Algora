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

        // Marketing automation fields
        public long? AbandonedCheckoutId { get; set; } // Shopify checkout ID for abandoned cart
        public int? OrderId { get; set; } // Order that triggered post-purchase automation
        public Order? Order { get; set; }
        public int? ABTestVariantId { get; set; }
        public ABTestVariant? ABTestVariant { get; set; }
        public string? Metadata { get; set; } // JSON for additional context data

        public ICollection<AutomationStepLog> StepLogs { get; set; } = new List<AutomationStepLog>();
        public ICollection<ABTestResult> ABTestResults { get; set; } = new List<ABTestResult>();
    }
}