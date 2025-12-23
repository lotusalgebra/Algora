namespace Algora.Domain.Entities;

/// <summary>
/// Represents a line item in a purchase order.
/// </summary>
public class PurchaseOrderLine
{
    public int Id { get; set; }

    // Purchase order reference
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    // Product reference
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Product info (denormalized)
    public string? Sku { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }

    // Quantities
    public int QuantityOrdered { get; set; }
    public int QuantityReceived { get; set; }

    // Pricing
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    // Receiving
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivingNotes { get; set; }
}
