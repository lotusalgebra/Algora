namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a recipient of an email campaign with tracking info.
    /// </summary>
    public class EmailCampaignRecipient
    {
        public int Id { get; set; }
        public int EmailCampaignId { get; set; }
        public EmailCampaign EmailCampaign { get; set; } = null!;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? SubscriberId { get; set; }
        public EmailSubscriber? Subscriber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, sent, delivered, bounced, failed
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public int OpenCount { get; set; }
        public DateTime? ClickedAt { get; set; }
        public int ClickCount { get; set; }
        public DateTime? BouncedAt { get; set; }
        public string? BounceType { get; set; } // hard, soft
        public string? BounceReason { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public DateTime? ComplainedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}