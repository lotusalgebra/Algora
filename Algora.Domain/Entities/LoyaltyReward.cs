namespace Algora.Domain.Entities;

/// <summary>
/// Represents a redeemable reward in the loyalty program.
/// </summary>
public class LoyaltyReward
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
    /// Reward name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reward description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reward type: discount_percent, discount_fixed, free_shipping, free_product.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Points required to redeem this reward.
    /// </summary>
    public int PointsCost { get; set; }

    /// <summary>
    /// Value of the reward (discount amount or percentage).
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Minimum order amount to use this reward.
    /// </summary>
    public decimal? MinimumOrderAmount { get; set; }

    /// <summary>
    /// Foreign key to the product (for free product rewards).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Navigation property to the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Maximum redemptions per customer (null = unlimited).
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Whether this reward is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the reward becomes available.
    /// </summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>
    /// When the reward expires.
    /// </summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>
    /// Image URL for the reward.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// When the reward was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
