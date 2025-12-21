namespace Algora.WhatsApp.Entities;

/// <summary>
/// Represents a WhatsApp broadcast campaign.
/// </summary>
public class WhatsAppCampaign
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public WhatsAppTemplate Template { get; set; } = null!;
    public int? SegmentId { get; set; } // Reference to CustomerSegment in main domain
    public string Status { get; set; } = "draft"; // draft, scheduled, sending, sent, paused, cancelled
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalRead { get; set; }
    public int TotalFailed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
