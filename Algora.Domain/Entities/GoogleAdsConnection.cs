namespace Algora.Domain.Entities;

/// <summary>
/// Stores Google Ads connection settings per shop.
/// </summary>
public class GoogleAdsConnection
{
    public int Id { get; set; }

    /// <summary>
    /// Shop domain this connection belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth refresh token (encrypted).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Google Ads Customer ID (e.g., 123-456-7890 without dashes stored as 1234567890).
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Display name of the customer account.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Manager account ID if using MCC (My Client Center).
    /// </summary>
    public string? ManagerAccountId { get; set; }

    /// <summary>
    /// Currency used by the account.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// When the connection was established.
    /// </summary>
    public DateTime? ConnectedAt { get; set; }

    /// <summary>
    /// When data was last synced from Google Ads.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Last sync error message, if any.
    /// </summary>
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Auto-sync enabled.
    /// </summary>
    public bool AutoSyncEnabled { get; set; } = true;

    /// <summary>
    /// Sync frequency in hours.
    /// </summary>
    public int SyncFrequencyHours { get; set; } = 6;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
