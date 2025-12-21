namespace Algora.Domain.Entities;

/// <summary>
/// Represents a discount tier for mix-and-match bundles.
/// Example: Pick 3 = 10% off, Pick 5 = 15% off.
/// </summary>
public class BundleRuleTier
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the bundle rule.
    /// </summary>
    public int BundleRuleId { get; set; }

    /// <summary>
    /// Navigation property to the bundle rule.
    /// </summary>
    public BundleRule BundleRule { get; set; } = null!;

    /// <summary>
    /// Minimum quantity for this tier to apply.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Maximum quantity for this tier (null = unlimited, applies to MinQuantity and above).
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// Discount type: "percentage" or "fixed_amount".
    /// </summary>
    public string DiscountType { get; set; } = "percentage";

    /// <summary>
    /// Discount value (e.g., 15 for 15% or 10.00 for $10 off).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Display label for this tier (e.g., "Pick 3, Save 15%").
    /// </summary>
    public string? DisplayLabel { get; set; }

    /// <summary>
    /// Display order for sorting tiers.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
