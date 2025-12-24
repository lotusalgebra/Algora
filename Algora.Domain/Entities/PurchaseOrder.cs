namespace Algora.Domain.Entities;

/// <summary>
/// Represents a purchase order sent to a supplier for restocking inventory.
/// </summary>
public class PurchaseOrder
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Supplier reference
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // Order identification
    public string OrderNumber { get; set; } = string.Empty;

    // Status: draft, sent, confirmed, shipped, received, cancelled
    public string Status { get; set; } = "draft";

    // Destination location
    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    // Totals
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";

    // Notes and tracking
    public string? Notes { get; set; }
    public string? SupplierReference { get; set; }
    public string? TrackingNumber { get; set; }

    // Dates
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
