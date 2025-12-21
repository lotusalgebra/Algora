namespace Algora.Domain.Entities;

/// <summary>
/// Tracks review request emails sent to customers.
/// </summary>
public class ReviewEmailLog
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public int AutomationId { get; set; }
    public ReviewEmailAutomation Automation { get; set; } = null!;

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Status: pending, scheduled, sent, opened, clicked, review_submitted, failed
    /// </summary>
    public string Status { get; set; } = "pending";
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? ReviewSubmittedAt { get; set; }
    public int? ReviewId { get; set; }
    public Review? Review { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Unique token for tracking opens and clicks.
    /// </summary>
    public string? TrackingToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
