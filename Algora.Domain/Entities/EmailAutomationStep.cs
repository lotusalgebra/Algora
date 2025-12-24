namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a step in an email automation workflow.
    /// </summary>
    public class EmailAutomationStep
    {
        public int Id { get; set; }
        public int AutomationId { get; set; }
        public EmailAutomation Automation { get; set; } = null!;
        public int StepOrder { get; set; }
        public string StepType { get; set; } = "email"; // email, delay, condition, sms, whatsapp
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public int? EmailTemplateId { get; set; }
        public EmailTemplate? EmailTemplate { get; set; }
        public int DelayMinutes { get; set; } // delay before this step
        public string? Conditions { get; set; } // JSON conditions for branching
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // SMS fields
        public int? SmsTemplateId { get; set; }
        public string? SmsBody { get; set; }

        // WhatsApp fields
        public string? WhatsAppTemplateId { get; set; }
        public string? WhatsAppBody { get; set; }

        // A/B testing
        public bool IsABTestEnabled { get; set; }

        public ICollection<ABTestVariant> ABTestVariants { get; set; } = new List<ABTestVariant>();
        public ICollection<AutomationStepLog> StepLogs { get; set; } = new List<AutomationStepLog>();
    }
}