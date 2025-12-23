namespace Algora.Domain.Entities;

/// <summary>
/// Links a supplier to products they can provide, with cost and SKU information.
/// </summary>
public class SupplierProduct
{
    public int Id { get; set; }

    // Supplier reference
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // Product reference
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Supplier's product info
    public string? SupplierSku { get; set; }
    public string? SupplierProductName { get; set; }
    public decimal UnitCost { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public int? LeadTimeDays { get; set; }

    // Preference
    public bool IsPreferred { get; set; }
    public DateTime? LastOrderedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
