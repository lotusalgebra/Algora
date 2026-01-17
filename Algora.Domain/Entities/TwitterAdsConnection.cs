namespace Algora.Domain.Entities;

/// <summary>
/// Stores Twitter Ads connection settings and OAuth tokens for a shop.
/// </summary>
public class TwitterAdsConnection
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // OAuth 2.0 tokens (encrypted at rest)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    // Twitter Ad Account info
    public string? AdAccountId { get; set; }
    public string? AdAccountName { get; set; }

    public string Currency { get; set; } = "USD";
    public bool IsConnected { get; set; }

    // Token expiration tracking
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastSyncError { get; set; }

    // Sync settings
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncFrequencyHours { get; set; } = 6;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
