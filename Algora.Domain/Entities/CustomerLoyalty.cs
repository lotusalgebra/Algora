namespace Algora.Domain.Entities;

/// <summary>
/// Represents a customer's loyalty program membership and status.
/// </summary>
public class CustomerLoyalty
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the customer.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Foreign key to the loyalty program.
    /// </summary>
    public int LoyaltyProgramId { get; set; }

    /// <summary>
    /// Navigation property to the loyalty program.
    /// </summary>
    public LoyaltyProgram LoyaltyProgram { get; set; } = null!;

    /// <summary>
    /// Foreign key to the current tier.
    /// </summary>
    public int? CurrentTierId { get; set; }

    /// <summary>
    /// Navigation property to the current tier.
    /// </summary>
    public LoyaltyTier? CurrentTier { get; set; }

    /// <summary>
    /// Current available points balance.
    /// </summary>
    public int PointsBalance { get; set; }

    /// <summary>
    /// Total lifetime points earned (for tier calculation).
    /// </summary>
    public int LifetimePoints { get; set; }

    /// <summary>
    /// Total lifetime points redeemed.
    /// </summary>
    public int LifetimeRedeemed { get; set; }

    /// <summary>
    /// Total lifetime amount spent.
    /// </summary>
    public decimal LifetimeSpent { get; set; }

    /// <summary>
    /// Unique referral code for this member.
    /// </summary>
    public string? ReferralCode { get; set; }

    /// <summary>
    /// Foreign key to the member who referred this customer.
    /// </summary>
    public int? ReferredById { get; set; }

    /// <summary>
    /// Navigation property to the referring member.
    /// </summary>
    public CustomerLoyalty? ReferredBy { get; set; }

    /// <summary>
    /// Customer's birthday (for birthday bonus).
    /// </summary>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// When the customer joined the program.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the customer last earned/redeemed points.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// When the tier was last updated.
    /// </summary>
    public DateTime? TierUpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Points transaction history.
    /// </summary>
    public ICollection<LoyaltyPoints> PointsHistory { get; set; } = new List<LoyaltyPoints>();

    /// <summary>
    /// Members referred by this customer.
    /// </summary>
    public ICollection<CustomerLoyalty> Referrals { get; set; } = new List<CustomerLoyalty>();
}
