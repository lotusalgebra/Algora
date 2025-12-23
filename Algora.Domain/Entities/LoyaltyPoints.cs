namespace Algora.Domain.Entities;

/// <summary>
/// Represents a points transaction (earn/redeem ledger entry).
/// </summary>
public class LoyaltyPoints
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the customer loyalty record.
    /// </summary>
    public int CustomerLoyaltyId { get; set; }

    /// <summary>
    /// Navigation property to the customer loyalty record.
    /// </summary>
    public CustomerLoyalty CustomerLoyalty { get; set; } = null!;

    /// <summary>
    /// Transaction type: earn, redeem, expire, adjust, bonus.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Points amount (positive for earn, negative for redeem/expire).
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Balance after this transaction.
    /// </summary>
    public int BalanceAfter { get; set; }

    /// <summary>
    /// Source of points: order, signup, birthday, review, referral, manual, redemption.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Source entity ID (OrderId, ReviewId, etc.).
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Description of the transaction.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When these points expire (for earned points).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the transaction occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
