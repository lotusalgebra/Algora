namespace Algora.Domain.Entities;

/// <summary>
/// Represents a product/variant in a fixed bundle.
/// </summary>
public class BundleItem
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the bundle.
    /// </summary>
    public int BundleId { get; set; }

    /// <summary>
    /// Navigation property to the bundle.
    /// </summary>
    public Bundle Bundle { get; set; } = null!;

    // Product reference

    /// <summary>
    /// Platform (Shopify) product ID.
    /// </summary>
    public long PlatformProductId { get; set; }

    /// <summary>
    /// Platform (Shopify) variant ID (optional, for specific variant).
    /// </summary>
    public long? PlatformVariantId { get; set; }

    // Denormalized product info (for display without joins)

    /// <summary>
    /// Product title at time of bundle creation.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Variant title (e.g., "Size: M, Color: Blue").
    /// </summary>
    public string? VariantTitle { get; set; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    // Quantity and pricing

    /// <summary>
    /// Quantity of this item in the bundle.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price at time of bundle creation.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Display order within the bundle.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Inventory tracking

    /// <summary>
    /// Current inventory quantity (cached, updated periodically).
    /// </summary>
    public int CurrentInventory { get; set; }

    /// <summary>
    /// When inventory was last checked.
    /// </summary>
    public DateTime? InventoryCheckedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
