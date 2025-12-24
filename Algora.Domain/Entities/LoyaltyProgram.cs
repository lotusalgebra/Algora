namespace Algora.Domain.Entities;

/// <summary>
/// Represents a loyalty program configuration for a shop.
/// </summary>
public class LoyaltyProgram
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain (unique).
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Program display name.
    /// </summary>
    public string Name { get; set; } = "Rewards Program";

    /// <summary>
    /// Whether the program is active.
    /// </summary>
    public bool IsActive { get; set; }

    // Points configuration

    /// <summary>
    /// Points earned per dollar spent.
    /// </summary>
    public int PointsPerDollar { get; set; } = 1;

    /// <summary>
    /// Value of 1 point in cents (for redemption).
    /// </summary>
    public int PointsValueCents { get; set; } = 1;

    /// <summary>
    /// Minimum points required to redeem.
    /// </summary>
    public int MinimumRedemption { get; set; } = 100;

    // Earning rules

    /// <summary>
    /// Bonus points for signing up.
    /// </summary>
    public int SignupBonus { get; set; }

    /// <summary>
    /// Bonus points on customer's birthday.
    /// </summary>
    public int BirthdayBonus { get; set; }

    /// <summary>
    /// Bonus points for writing a review.
    /// </summary>
    public int ReviewBonus { get; set; }

    /// <summary>
    /// Bonus points for successful referral.
    /// </summary>
    public int ReferralBonus { get; set; }

    // Expiration

    /// <summary>
    /// Number of months until points expire (null = never expire).
    /// </summary>
    public int? PointsExpireMonths { get; set; }

    // Branding

    /// <summary>
    /// Custom name for points (e.g., "Stars", "Coins").
    /// </summary>
    public string PointsName { get; set; } = "Points";

    /// <summary>
    /// Currency for value calculations.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// When the program was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the program was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Tiers in this program.
    /// </summary>
    public ICollection<LoyaltyTier> Tiers { get; set; } = new List<LoyaltyTier>();

    /// <summary>
    /// Rewards in this program.
    /// </summary>
    public ICollection<LoyaltyReward> Rewards { get; set; } = new List<LoyaltyReward>();

    /// <summary>
    /// Members enrolled in this program.
    /// </summary>
    public ICollection<CustomerLoyalty> Members { get; set; } = new List<CustomerLoyalty>();
}
