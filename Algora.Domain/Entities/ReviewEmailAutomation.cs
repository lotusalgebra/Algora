namespace Algora.Domain.Entities;

/// <summary>
/// Represents an automated review request email configuration.
/// </summary>
public class ReviewEmailAutomation
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Trigger type
    /// <summary>
    /// Trigger type: after_delivery, after_purchase
    /// </summary>
    public string TriggerType { get; set; } = "after_delivery";
    public int DelayDays { get; set; } = 7;
    public int DelayHours { get; set; }

    // Conditions (JSON arrays for product IDs and tags)
    public decimal? MinOrderValue { get; set; }
    public string? ProductIds { get; set; } // JSON array of product IDs to include
    public string? ExcludedProductIds { get; set; } // JSON array of product IDs to exclude
    public string? CustomerTags { get; set; } // JSON array of tags to include
    public string? ExcludedCustomerTags { get; set; } // JSON array of tags to exclude
    public bool ExcludeRepeatedCustomers { get; set; } = true;
    public int? RepeatedCustomerExclusionDays { get; set; } = 30;

    // Email content
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? EmailTemplateId { get; set; }
    public EmailTemplate? EmailTemplate { get; set; }

    // Stats
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalReviewsCollected { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ReviewEmailLog> EmailLogs { get; set; } = new List<ReviewEmailLog>();
}
