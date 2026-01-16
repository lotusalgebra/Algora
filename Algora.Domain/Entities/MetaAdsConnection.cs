namespace Algora.Domain.Entities;

/// <summary>
/// Stores Meta (Facebook/Instagram) Ads connection settings per shop.
/// </summary>
public class MetaAdsConnection
{
    public int Id { get; set; }

    /// <summary>
    /// Shop domain this connection belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Meta Ads access token (encrypted).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Meta Ad Account ID (e.g., act_123456789).
    /// </summary>
    public string? AdAccountId { get; set; }

    /// <summary>
    /// Display name of the ad account.
    /// </summary>
    public string? AdAccountName { get; set; }

    /// <summary>
    /// Business name associated with the account.
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Currency used by the ad account.
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
    /// When data was last synced from Meta.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Last sync error message, if any.
    /// </summary>
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Token expiration date.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

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
