namespace Algora.Domain.Entities;

/// <summary>
/// Stores Pinterest Ads connection settings per shop.
/// </summary>
public class PinterestAdsConnection
{
    public int Id { get; set; }

    /// <summary>
    /// Shop domain this connection belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Pinterest OAuth access token (encrypted).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Pinterest OAuth refresh token (encrypted).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Pinterest Ad Account ID.
    /// </summary>
    public string? AdAccountId { get; set; }

    /// <summary>
    /// Display name of the ad account.
    /// </summary>
    public string? AdAccountName { get; set; }

    /// <summary>
    /// Business ID if applicable.
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// Currency used by the account.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Country of the ad account.
    /// </summary>
    public string Country { get; set; } = "US";

    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// When the connection was established.
    /// </summary>
    public DateTime? ConnectedAt { get; set; }

    /// <summary>
    /// When data was last synced from Pinterest Ads.
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
