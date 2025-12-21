namespace Algora.Application.DTOs.Inventory;

public record InventoryAlertDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public long PlatformProductId { get; init; }
    public long? PlatformVariantId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public string? Sku { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int CurrentQuantity { get; init; }
    public int ThresholdQuantity { get; init; }
    public int? DaysUntilStockout { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool EmailSent { get; init; }
    public bool SmsSent { get; init; }
    public bool WhatsAppSent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public record InventoryAlertSettingsDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public bool AlertsEnabled { get; init; }
    public int LowStockDaysThreshold { get; init; }
    public int CriticalStockDaysThreshold { get; init; }
    public int? DefaultLowStockQuantity { get; init; }
    public int? DefaultCriticalStockQuantity { get; init; }
    public int DefaultLeadTimeDays { get; init; }
    public int DefaultSafetyStockDays { get; init; }
    public bool EmailNotificationsEnabled { get; init; }
    public string? NotificationEmail { get; init; }
    public bool SmsNotificationsEnabled { get; init; }
    public string? NotificationPhone { get; init; }
    public bool WhatsAppNotificationsEnabled { get; init; }
    public string? WhatsAppPhone { get; init; }
    public int MinHoursBetweenAlerts { get; init; }
    public bool DailyDigestEnabled { get; init; }
}

public record UpdateInventoryAlertSettingsDto
{
    public bool AlertsEnabled { get; init; }
    public int LowStockDaysThreshold { get; init; }
    public int CriticalStockDaysThreshold { get; init; }
    public int? DefaultLowStockQuantity { get; init; }
    public int? DefaultCriticalStockQuantity { get; init; }
    public int DefaultLeadTimeDays { get; init; }
    public int DefaultSafetyStockDays { get; init; }
    public bool EmailNotificationsEnabled { get; init; }
    public string? NotificationEmail { get; init; }
    public bool SmsNotificationsEnabled { get; init; }
    public string? NotificationPhone { get; init; }
    public bool WhatsAppNotificationsEnabled { get; init; }
    public string? WhatsAppPhone { get; init; }
    public int MinHoursBetweenAlerts { get; init; }
    public bool DailyDigestEnabled { get; init; }
}

public record DismissAlertDto
{
    public string? Reason { get; init; }
}
