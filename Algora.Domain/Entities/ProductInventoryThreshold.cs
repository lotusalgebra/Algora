namespace Algora.Domain.Entities;

/// <summary>
/// Per-product inventory threshold overrides for alerts and auto-reordering.
/// </summary>
public class ProductInventoryThreshold
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Product reference
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Alert thresholds (override shop-level settings)
    public int? LowStockThreshold { get; set; }
    public int? CriticalStockThreshold { get; set; }

    // Reorder settings
    public int? ReorderPoint { get; set; }
    public int? ReorderQuantity { get; set; }
    public int? SafetyStockDays { get; set; }
    public int? LeadTimeDays { get; set; }

    // Preferred supplier for auto-reorder
    public int? PreferredSupplierId { get; set; }
    public Supplier? PreferredSupplier { get; set; }

    // Automation
    public bool AutoReorderEnabled { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
