namespace Algora.Domain.Entities;

/// <summary>
/// Represents a supplier/vendor who provides products to the shop.
/// </summary>
public class Supplier
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Basic info
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }

    // Ordering terms
    public int DefaultLeadTimeDays { get; set; } = 7;
    public decimal? MinimumOrderAmount { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Performance metrics (calculated from PurchaseOrders)
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal? AverageDeliveryDays { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
