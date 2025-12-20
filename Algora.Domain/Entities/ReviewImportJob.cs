namespace Algora.Domain.Entities;

/// <summary>
/// Represents a review import job from an external source (Amazon, AliExpress).
/// </summary>
public class ReviewImportJob
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Source configuration
    /// <summary>
    /// Source type: amazon, aliexpress
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string? SourceProductId { get; set; } // ASIN for Amazon
    public string? SourceProductTitle { get; set; }

    // Target product mapping
    public int? TargetProductId { get; set; }
    public Product? TargetProduct { get; set; }
    public long? TargetPlatformProductId { get; set; }
    public string? TargetProductTitle { get; set; }
    /// <summary>
    /// Mapping method: manual, sku, title
    /// </summary>
    public string MappingMethod { get; set; } = "manual";

    // Job status
    /// <summary>
    /// Status: pending, processing, completed, failed, cancelled
    /// </summary>
    public string Status { get; set; } = "pending";
    public int TotalReviews { get; set; }
    public int ImportedReviews { get; set; }
    public int SkippedReviews { get; set; } // duplicates
    public int FailedReviews { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProgressLog { get; set; } // JSON array of log entries

    // Filters
    public int? MinRating { get; set; } // Only import reviews >= this rating
    public bool IncludePhotosOnly { get; set; }
    public DateTime? ReviewsAfterDate { get; set; }
    public int? MaxReviews { get; set; } // Limit number of reviews to import

    // Timing
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
