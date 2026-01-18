namespace Algora.Domain.Entities;

/// <summary>
/// Stores Microsoft Advertising (Bing Ads) connection settings and OAuth tokens for a shop.
/// </summary>
public class BingAdsConnection
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // OAuth tokens (encrypted at rest)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    // Microsoft Advertising Account info
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? CustomerId { get; set; }

    public string Currency { get; set; } = "USD";
    public bool IsConnected { get; set; }

    // Token expiration tracking
    public DateTime? TokenExpiresAt { get; set; }

    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastSyncError { get; set; }

    // Sync settings
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncFrequencyHours { get; set; } = 6;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
