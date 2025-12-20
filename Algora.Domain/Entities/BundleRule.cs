namespace Algora.Domain.Entities;

/// <summary>
/// Represents rules for a mix-and-match bundle (eligible products, quantity constraints).
/// </summary>
public class BundleRule
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the bundle.
    /// </summary>
    public int BundleId { get; set; }

    /// <summary>
    /// Navigation property to the bundle.
    /// </summary>
    public Bundle Bundle { get; set; } = null!;

    /// <summary>
    /// Rule name for admin reference.
    /// </summary>
    public string? Name { get; set; }

    // Eligible products (JSON arrays)

    /// <summary>
    /// JSON array of eligible platform product IDs.
    /// Example: "[123456789, 987654321]"
    /// </summary>
    public string? EligibleProductIds { get; set; }

    /// <summary>
    /// JSON array of eligible collection IDs (products in these collections are eligible).
    /// Example: "[111111, 222222]"
    /// </summary>
    public string? EligibleCollectionIds { get; set; }

    /// <summary>
    /// JSON array of eligible tags (products with these tags are eligible).
    /// Example: '["sale", "bundle-eligible"]'
    /// </summary>
    public string? EligibleTags { get; set; }

    // Quantity constraints

    /// <summary>
    /// Minimum quantity customer must select from this rule.
    /// </summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity customer can select from this rule (null = unlimited up to bundle max).
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// Whether customers can select the same product multiple times.
    /// </summary>
    public bool AllowDuplicates { get; set; } = true;

    /// <summary>
    /// Display order for multiple rules in a bundle.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Label to show customers (e.g., "Choose your main dish", "Pick your sides").
    /// </summary>
    public string? DisplayLabel { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation

    /// <summary>
    /// Discount tiers for this rule.
    /// </summary>
    public ICollection<BundleRuleTier> Tiers { get; set; } = new List<BundleRuleTier>();
}
