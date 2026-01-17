namespace Algora.Domain.Entities;

/// <summary>
/// Stores TikTok Ads connection settings per shop.
/// </summary>
public class TikTokAdsConnection
{
    public int Id { get; set; }

    /// <summary>
    /// Shop domain this connection belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// TikTok OAuth access token (encrypted).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// TikTok OAuth refresh token (encrypted).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// TikTok Advertiser ID.
    /// </summary>
    public string? AdvertiserId { get; set; }

    /// <summary>
    /// Display name of the advertiser account.
    /// </summary>
    public string? AdvertiserName { get; set; }

    /// <summary>
    /// Business Center ID if applicable.
    /// </summary>
    public string? BusinessCenterId { get; set; }

    /// <summary>
    /// Currency used by the account.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Timezone of the advertiser account.
    /// </summary>
    public string Timezone { get; set; } = "UTC";

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
    /// When data was last synced from TikTok Ads.
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
