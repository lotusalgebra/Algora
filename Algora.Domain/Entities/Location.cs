namespace Algora.Domain.Entities;

/// <summary>
/// Represents a Shopify location (warehouse, store, etc.) for inventory tracking.
/// </summary>
public class Location
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Shopify reference
    public long ShopifyLocationId { get; set; }

    // Location info
    public string Name { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ProvinceCode { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public bool FulfillsOnlineOrders { get; set; } = true;

    // Sync
    public DateTime? LastSyncedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<InventoryLevel> InventoryLevels { get; set; } = new List<InventoryLevel>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
