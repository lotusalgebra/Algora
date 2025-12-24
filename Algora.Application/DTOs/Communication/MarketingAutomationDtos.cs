namespace Algora.Application.DTOs.Communication;

// ==================== ENROLLMENT CONTEXT ====================

/// <summary>
/// Context for enrolling a customer in an automation workflow.
/// </summary>
public record EnrollmentContext(
    int? CustomerId,
    int? SubscriberId,
    string Email,
    long? AbandonedCheckoutId = null,
    int? OrderId = null,
    string? Metadata = null
);

// ==================== A/B TEST DTOs ====================

public record ABTestVariantDto(
    int Id,
    int AutomationId,
    int? StepId,
    string VariantName,
    string? Subject,
    string? Body,
    int Weight,
    bool IsControl,
    int Impressions,
    int Opens,
    int Clicks,
    int Conversions,
    decimal Revenue,
    decimal OpenRate,
    decimal ClickRate,
    decimal ConversionRate
);

public record CreateABTestVariantDto(
    int AutomationId,
    int? StepId,
    string VariantName,
    string? Subject,
    string? Body,
    int Weight,
    bool IsControl
);

public record ABTestResultDto(
    int Id,
    int EnrollmentId,
    int VariantId,
    string VariantName,
    bool Opened,
    bool Clicked,
    bool Converted,
    decimal? ConversionValue,
    DateTime AssignedAt,
    DateTime? OpenedAt,
    DateTime? ClickedAt,
    DateTime? ConvertedAt
);

public record ABTestStatisticsDto(
    int VariantId,
    string VariantName,
    bool IsControl,
    int SampleSize,
    decimal ConversionRate,
    decimal ConversionRateChange, // vs control
    decimal StatisticalSignificance,
    bool IsSignificant,
    decimal Revenue,
    decimal RevenuePerRecipient
);

// ==================== AUTOMATION STEP LOG DTOs ====================

public record AutomationStepLogDto(
    int Id,
    int EnrollmentId,
    int StepId,
    string StepName,
    string Status,
    string? Channel,
    string? ExternalMessageId,
    string? ErrorMessage,
    DateTime? ScheduledAt,
    DateTime? ExecutedAt,
    DateTime? DeliveredAt,
    DateTime? OpenedAt,
    DateTime? ClickedAt,
    DateTime? BouncedAt,
    DateTime? UnsubscribedAt
);

// ==================== WINBACK DTOs ====================

public record WinbackRuleDto(
    int Id,
    string ShopDomain,
    int AutomationId,
    string AutomationName,
    string Name,
    int DaysInactive,
    decimal? MinimumLifetimeValue,
    int? MinimumOrders,
    int? MaximumOrders,
    bool ExcludeRecentSubscribers,
    int? ExcludeSubscribedWithinDays,
    List<string>? CustomerTags,
    List<string>? ExcludeTags,
    bool IsActive,
    DateTime? LastRunAt,
    int CustomersEnrolledLastRun
);

public record CreateWinbackRuleDto(
    int AutomationId,
    string Name,
    int DaysInactive,
    decimal? MinimumLifetimeValue,
    int? MinimumOrders,
    int? MaximumOrders,
    bool ExcludeRecentSubscribers,
    int? ExcludeSubscribedWithinDays,
    List<string>? CustomerTags,
    List<string>? ExcludeTags
);

public record InactiveCustomerDto(
    int CustomerId,
    string Email,
    string? FirstName,
    string? LastName,
    int DaysSinceLastOrder,
    int TotalOrders,
    decimal TotalSpent,
    DateTime? LastOrderDate
);

// ==================== AUTOMATION ANALYTICS DTOs ====================

public record AutomationAnalyticsDto(
    int AutomationId,
    string AutomationName,
    string TriggerType,
    int TotalEnrolled,
    int ActiveEnrollments,
    int CompletedEnrollments,
    int ExitedEnrollments,
    decimal Revenue,
    decimal RevenuePerEnrollment,
    int TotalEmailsSent,
    int TotalSMSSent,
    int TotalWhatsAppSent,
    decimal OpenRate,
    decimal ClickRate,
    decimal ConversionRate,
    List<StepAnalyticsDto> StepAnalytics
);

public record StepAnalyticsDto(
    int StepId,
    int StepOrder,
    string StepType,
    string? Subject,
    int Sent,
    int Delivered,
    int Opens,
    int Clicks,
    int Bounced,
    int Unsubscribed,
    decimal OpenRate,
    decimal ClickRate,
    decimal BounceRate
);

// ==================== ENROLLMENT DTOs ====================

public record AutomationEnrollmentDto(
    int Id,
    int AutomationId,
    string AutomationName,
    int? CustomerId,
    string? CustomerName,
    string Email,
    int CurrentStepId,
    string CurrentStepName,
    string Status,
    DateTime? NextStepAt,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    DateTime? ExitedAt,
    string? ExitReason,
    long? AbandonedCheckoutId,
    int? OrderId,
    string? OrderNumber,
    List<AutomationStepLogDto> StepLogs
);

// ==================== PERSONALIZATION DTOs ====================

public record PersonalizationTokenDto(
    string Token,
    string Description,
    string Category,
    string? SampleValue
);

public record PersonalizationContextDto(
    string ShopDomain,
    string ShopName,
    int? CustomerId,
    string? CustomerEmail,
    string? CustomerFirstName,
    string? CustomerLastName,
    decimal? CustomerTotalSpent,
    int? CustomerOrderCount,
    int? OrderId,
    string? OrderNumber,
    decimal? OrderTotal,
    string? OrderItems,
    long? CheckoutId,
    string? CartRecoveryUrl,
    string? CartItems,
    decimal? CartTotal,
    Dictionary<string, string>? CustomTokens
);

// ==================== TRIGGER DTOs ====================

public record AbandonedCartTriggerDto(
    long CheckoutId,
    string Email,
    string? CustomerFirstName,
    string? CustomerLastName,
    decimal CartTotal,
    string Currency,
    string? RecoveryUrl,
    List<CartLineItemDto> LineItems,
    DateTime AbandonedAt
);

public record CartLineItemDto(
    long ProductId,
    long? VariantId,
    string Title,
    string? VariantTitle,
    int Quantity,
    decimal Price,
    string? ImageUrl
);

public record PostPurchaseTriggerDto(
    int OrderId,
    string OrderNumber,
    int? CustomerId,
    string Email,
    decimal OrderTotal,
    string Currency,
    DateTime OrderDate
);

public record WelcomeTriggerDto(
    int CustomerId,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime CreatedAt
);
