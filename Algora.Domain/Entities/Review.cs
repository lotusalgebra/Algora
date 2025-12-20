namespace Algora.Domain.Entities;

/// <summary>
/// Represents a product review from any source (manual, imported, or customer-submitted).
/// </summary>
public class Review
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Product association
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public long? PlatformProductId { get; set; }
    public string? ProductTitle { get; set; }
    public string? ProductSku { get; set; }

    // Review content
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerEmail { get; set; }
    public int Rating { get; set; } // 1-5
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }

    // Source tracking
    /// <summary>
    /// Source of the review: manual, amazon, aliexpress, email_request
    /// </summary>
    public string Source { get; set; } = "manual";
    public string? SourceUrl { get; set; }
    public string? ExternalReviewId { get; set; }
    public int? ImportJobId { get; set; }
    public ReviewImportJob? ImportJob { get; set; }

    // Order association (for email-collected reviews)
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Moderation
    /// <summary>
    /// Status: pending, approved, rejected
    /// </summary>
    public string Status { get; set; } = "pending";
    public bool IsFeatured { get; set; }
    public string? ModerationNote { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Helpful votes
    public int HelpfulVotes { get; set; }
    public int UnhelpfulVotes { get; set; }

    // Dates
    public DateTime ReviewDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ReviewMedia> Media { get; set; } = new List<ReviewMedia>();
}
