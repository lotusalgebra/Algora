namespace Algora.Domain.Entities;

/// <summary>
/// Stores Amazon Advertising connection settings and OAuth tokens for a shop.
/// </summary>
public class AmazonAdsConnection
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // OAuth tokens (encrypted at rest)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    // Amazon Advertising Profile info
    public string? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? MarketplaceId { get; set; }
    public string? CountryCode { get; set; }

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
