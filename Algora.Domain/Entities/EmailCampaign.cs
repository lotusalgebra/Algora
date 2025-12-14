namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an email marketing campaign.
    /// </summary>
    public class EmailCampaign
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? PreviewText { get; set; }
        public string Body { get; set; } = string.Empty; // HTML content
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public string? ReplyToEmail { get; set; }
        public string CampaignType { get; set; } = "regular"; // regular, automated, ab_test
        public string Status { get; set; } = "draft"; // draft, scheduled, sending, sent, paused, cancelled
        public int? EmailTemplateId { get; set; }
        public EmailTemplate? EmailTemplate { get; set; }
        public int? SegmentId { get; set; }
        public CustomerSegment? Segment { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public int TotalRecipients { get; set; }
        public int TotalSent { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalOpened { get; set; }
        public int TotalClicked { get; set; }
        public int TotalBounced { get; set; }
        public int TotalUnsubscribed { get; set; }
        public int TotalComplaints { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<EmailCampaignRecipient> Recipients { get; set; } = new List<EmailCampaignRecipient>();
    }
}