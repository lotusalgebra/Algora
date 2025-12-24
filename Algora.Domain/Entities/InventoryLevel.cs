namespace Algora.Domain.Entities;

/// <summary>
/// Represents inventory levels for a product at a specific location.
/// </summary>
public class InventoryLevel
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Location reference
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    // Product reference
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Shopify inventory item reference
    public long ShopifyInventoryItemId { get; set; }

    // Inventory quantities
    public int Available { get; set; }
    public int Incoming { get; set; }
    public int Committed { get; set; }
    public int? OnHand { get; set; }

    // Sync
    public DateTime? LastSyncedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
