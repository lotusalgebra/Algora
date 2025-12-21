namespace Algora.Domain.Entities;

/// <summary>
/// Per-shop configuration for inventory alerts.
/// </summary>
public class InventoryAlertSettings
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Global alert settings
    public bool AlertsEnabled { get; set; } = true;

    // Threshold settings (days until stockout)
    public int LowStockDaysThreshold { get; set; } = 14; // Warn when < 14 days of stock
    public int CriticalStockDaysThreshold { get; set; } = 7; // Critical when < 7 days
    public int OutOfStockThreshold { get; set; } = 0; // Alert when quantity = 0

    // Alternative: quantity-based thresholds (optional, per-product override)
    public int? DefaultLowStockQuantity { get; set; }
    public int? DefaultCriticalStockQuantity { get; set; }

    // Reorder settings
    public int DefaultLeadTimeDays { get; set; } = 7; // Supplier lead time
    public int DefaultSafetyStockDays { get; set; } = 3; // Buffer days

    // Notification channels
    public bool EmailNotificationsEnabled { get; set; } = true;
    public string? NotificationEmail { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
    public string? NotificationPhone { get; set; }
    public bool WhatsAppNotificationsEnabled { get; set; }
    public string? WhatsAppPhone { get; set; }

    // Alert frequency
    public int MinHoursBetweenAlerts { get; set; } = 24; // Don't spam alerts
    public bool DailyDigestEnabled { get; set; }
    public TimeSpan? DailyDigestTime { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
