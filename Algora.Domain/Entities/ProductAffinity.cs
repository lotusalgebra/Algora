namespace Algora.Domain.Entities;

/// <summary>
/// Represents a co-purchase relationship between two products.
/// Used for "frequently bought together" recommendations.
/// </summary>
public class ProductAffinity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this affinity belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Source product (the one being purchased)
    /// <summary>
    /// Platform product ID for product A.
    /// </summary>
    public long PlatformProductIdA { get; set; }

    /// <summary>
    /// Product A title (denormalized for display).
    /// </summary>
    public string ProductTitleA { get; set; } = string.Empty;

    // Related product (frequently bought together)
    /// <summary>
    /// Platform product ID for product B.
    /// </summary>
    public long PlatformProductIdB { get; set; }

    /// <summary>
    /// Product B title (denormalized for display).
    /// </summary>
    public string ProductTitleB { get; set; } = string.Empty;

    // Co-purchase statistics
    /// <summary>
    /// Number of times these products were purchased together.
    /// </summary>
    public int CoOccurrenceCount { get; set; }

    /// <summary>
    /// Total orders containing product A.
    /// </summary>
    public int ProductAOrderCount { get; set; }

    /// <summary>
    /// Total orders containing product B.
    /// </summary>
    public int ProductBOrderCount { get; set; }

    // Calculated metrics (market basket analysis)
    /// <summary>
    /// Support score: CoOccurrence / TotalOrders (0-1).
    /// How frequently these products appear together across all orders.
    /// </summary>
    public decimal SupportScore { get; set; }

    /// <summary>
    /// Confidence score: CoOccurrence / ProductAOrderCount (0-1).
    /// Given product A is purchased, probability of B being purchased.
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Lift score: Confidence / (ProductBOrderCount/TotalOrders).
    /// Values > 1 indicate positive association.
    /// </summary>
    public decimal LiftScore { get; set; }

    // Analysis window
    /// <summary>
    /// Total orders analyzed in the calculation.
    /// </summary>
    public int TotalOrdersAnalyzed { get; set; }

    /// <summary>
    /// Start date of the analysis window.
    /// </summary>
    public DateTime AnalysisStartDate { get; set; }

    /// <summary>
    /// End date of the analysis window.
    /// </summary>
    public DateTime AnalysisEndDate { get; set; }

    // Timestamps
    /// <summary>
    /// When this affinity was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
