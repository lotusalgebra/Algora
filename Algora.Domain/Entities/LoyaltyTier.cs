namespace Algora.Domain.Entities;

/// <summary>
/// Represents a loyalty tier (Bronze, Silver, Gold, Platinum, etc.).
/// </summary>
public class LoyaltyTier
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the loyalty program.
    /// </summary>
    public int LoyaltyProgramId { get; set; }

    /// <summary>
    /// Navigation property to the loyalty program.
    /// </summary>
    public LoyaltyProgram LoyaltyProgram { get; set; } = null!;

    /// <summary>
    /// Tier name (e.g., Bronze, Silver, Gold, Platinum).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum lifetime points required to reach this tier.
    /// </summary>
    public int MinimumPoints { get; set; }

    /// <summary>
    /// Points multiplier for this tier (1.0 = 1x, 1.5 = 1.5x).
    /// </summary>
    public decimal PointsMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Automatic percentage discount for this tier.
    /// </summary>
    public decimal? PercentageDiscount { get; set; }

    /// <summary>
    /// Whether this tier includes free shipping.
    /// </summary>
    public bool FreeShipping { get; set; }

    /// <summary>
    /// Whether this tier gets exclusive access to products/sales.
    /// </summary>
    public bool ExclusiveAccess { get; set; }

    /// <summary>
    /// Color code for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Display order (0 = lowest tier).
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When the tier was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
