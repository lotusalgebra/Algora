namespace Algora.Domain.Entities;

/// <summary>
/// Tracks inventory alerts and their notification status.
/// </summary>
public class InventoryAlert
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Reference to prediction
    public int InventoryPredictionId { get; set; }
    public InventoryPrediction InventoryPrediction { get; set; } = null!;

    // Product info (denormalized)
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }

    // Alert details
    public string AlertType { get; set; } = string.Empty; // stockout_warning, low_stock, critical_stock, out_of_stock, reorder_reminder
    public string Severity { get; set; } = "medium"; // low, medium, high, critical
    public string Message { get; set; } = string.Empty;

    // Threshold that triggered the alert
    public int CurrentQuantity { get; set; }
    public int ThresholdQuantity { get; set; }
    public int? DaysUntilStockout { get; set; }

    // Notification tracking
    public bool EmailSent { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public bool SmsSent { get; set; }
    public DateTime? SmsSentAt { get; set; }
    public bool WhatsAppSent { get; set; }
    public DateTime? WhatsAppSentAt { get; set; }

    // Status
    public string Status { get; set; } = "active"; // active, acknowledged, resolved, dismissed
    public string? DismissReason { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
