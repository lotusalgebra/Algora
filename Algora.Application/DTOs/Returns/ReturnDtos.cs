namespace Algora.Application.DTOs.Returns;

/// <summary>
/// DTO for return request details.
/// </summary>
public record ReturnRequestDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string RequestNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OrderId { get; init; }
    public long PlatformOrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int? CustomerId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public int? ReturnReasonId { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonDescription { get; init; } = string.Empty;
    public string? CustomerNote { get; init; }
    public bool IsAutoApproved { get; init; }
    public string? ApprovalNote { get; init; }
    public string? RejectionReason { get; init; }
    public decimal TotalRefundAmount { get; init; }
    public decimal ShippingCost { get; init; }
    public string Currency { get; init; } = "USD";
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    public string? TrackingCarrier { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
    public DateTime? RefundedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<ReturnItemDto> Items { get; init; } = new();
    public ReturnLabelDto? Label { get; init; }
}

/// <summary>
/// DTO for return item details.
/// </summary>
public record ReturnItemDto
{
    public int Id { get; init; }
    public int? OrderLineId { get; init; }
    public long? PlatformProductId { get; init; }
    public long? PlatformVariantId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public int QuantityOrdered { get; init; }
    public int QuantityReturned { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal RefundAmount { get; init; }
    public string? ReasonCode { get; init; }
    public string? CustomerNote { get; init; }
    public bool Restock { get; init; }
    public bool Restocked { get; init; }
    public string? Condition { get; init; }
    public string? ConditionNote { get; init; }
}

/// <summary>
/// DTO for creating a return request.
/// </summary>
public record CreateReturnRequestDto
{
    public int OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public string? CustomerEmail { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string? CustomerNote { get; init; }
    public List<CreateReturnItemDto> Items { get; init; } = new();
}

/// <summary>
/// DTO for creating a return item.
/// </summary>
public record CreateReturnItemDto
{
    public int OrderLineId { get; init; }
    public int Quantity { get; init; }
    public string? ReasonCode { get; init; }
    public string? CustomerNote { get; init; }
}

/// <summary>
/// DTO for return reason.
/// </summary>
public record ReturnReasonDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public bool RequiresNote { get; init; }
    public bool IsDefect { get; init; }
    public bool EligibleForAutoApproval { get; init; }
}

/// <summary>
/// DTO for creating/updating a return reason.
/// </summary>
public record CreateReturnReasonDto
{
    public string Code { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool RequiresNote { get; init; }
    public bool IsDefect { get; init; }
    public bool EligibleForAutoApproval { get; init; } = true;
}

/// <summary>
/// DTO for return label details.
/// </summary>
public record ReturnLabelDto
{
    public int Id { get; init; }
    public string ShippoTransactionId { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
    public string Carrier { get; init; } = string.Empty;
    public string ServiceLevel { get; init; } = string.Empty;
    public string LabelUrl { get; init; } = string.Empty;
    public string LabelFormat { get; init; } = "PDF";
    public decimal Cost { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for return settings.
/// </summary>
public record ReturnSettingsDto
{
    public int Id { get; init; }
    public bool IsEnabled { get; init; }
    public bool AllowSelfService { get; init; }
    public int ReturnWindowDays { get; init; }
    public bool RequireDeliveryConfirmation { get; init; }
    public int LabelExpirationDays { get; init; }
    public bool AutoApprovalEnabled { get; init; }
    public decimal AutoApprovalMaxAmount { get; init; }
    public bool AutoApprovalRequireReason { get; init; }
    public bool HasShippoApiKey { get; init; }
    public bool StorePayShipping { get; init; }
    public string? DefaultCarrier { get; init; }
    public string? DefaultServiceLevel { get; init; }
    public ReturnAddressDto? ReturnAddress { get; init; }
    public bool EmailNotificationsEnabled { get; init; }
    public bool SmsNotificationsEnabled { get; init; }
    public string? NotificationEmail { get; init; }
    public string? PageTitle { get; init; }
    public string? PolicyText { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
}

/// <summary>
/// DTO for updating return settings.
/// </summary>
public record UpdateReturnSettingsDto
{
    public bool? IsEnabled { get; init; }
    public bool? AllowSelfService { get; init; }
    public int? ReturnWindowDays { get; init; }
    public bool? RequireDeliveryConfirmation { get; init; }
    public int? LabelExpirationDays { get; init; }
    public bool? AutoApprovalEnabled { get; init; }
    public decimal? AutoApprovalMaxAmount { get; init; }
    public bool? AutoApprovalRequireReason { get; init; }
    public string? ShippoApiKey { get; init; }
    public bool? StorePayShipping { get; init; }
    public string? DefaultCarrier { get; init; }
    public string? DefaultServiceLevel { get; init; }
    public ReturnAddressDto? ReturnAddress { get; init; }
    public bool? EmailNotificationsEnabled { get; init; }
    public bool? SmsNotificationsEnabled { get; init; }
    public string? NotificationEmail { get; init; }
    public string? PageTitle { get; init; }
    public string? PolicyText { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
}

/// <summary>
/// DTO for return address.
/// </summary>
public record ReturnAddressDto
{
    public string? Name { get; init; }
    public string? Company { get; init; }
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}

/// <summary>
/// DTO for return analytics.
/// </summary>
public record ReturnAnalyticsDto
{
    public int TotalReturns { get; init; }
    public int PendingReturns { get; init; }
    public int ApprovedReturns { get; init; }
    public int ShippedReturns { get; init; }
    public int ReceivedReturns { get; init; }
    public int RefundedReturns { get; init; }
    public int RejectedReturns { get; init; }
    public decimal TotalRefundAmount { get; init; }
    public decimal TotalShippingCost { get; init; }
    public decimal ReturnRate { get; init; }
    public decimal AverageProcessingDays { get; init; }
    public Dictionary<string, int> ReturnsByReason { get; init; } = new();
    public Dictionary<string, int> ReturnsByStatus { get; init; } = new();
    public List<TopReturnedProductDto> TopReturnedProducts { get; init; } = new();
}

/// <summary>
/// DTO for top returned products.
/// </summary>
public record TopReturnedProductDto
{
    public long? PlatformProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public int ReturnCount { get; init; }
    public int TotalQuantityReturned { get; init; }
    public decimal TotalRefundAmount { get; init; }
}

/// <summary>
/// DTO for customer return eligibility check.
/// </summary>
public record CustomerReturnEligibilityDto
{
    public bool IsEligible { get; init; }
    public string? IneligibleReason { get; init; }
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReturnDeadline { get; init; }
    public int DaysRemaining { get; init; }
    public List<EligibleOrderLineDto> EligibleItems { get; init; } = new();
}

/// <summary>
/// DTO for eligible order line items.
/// </summary>
public record EligibleOrderLineDto
{
    public int OrderLineId { get; init; }
    public long? PlatformProductId { get; init; }
    public long? PlatformVariantId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public int QuantityOrdered { get; init; }
    public int QuantityAlreadyReturned { get; init; }
    public int QuantityReturnable { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// DTO for Shippo tracking information.
/// </summary>
public record ShippoTrackingDto
{
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? StatusDetails { get; init; }
    public DateTime? EstimatedDelivery { get; init; }
    public List<TrackingEventDto> Events { get; init; } = new();
}

/// <summary>
/// DTO for tracking events.
/// </summary>
public record TrackingEventDto
{
    public DateTime Timestamp { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// DTO for address validation result.
/// </summary>
public record AddressValidationResultDto
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public ReturnAddressDto? SuggestedAddress { get; init; }
}

/// <summary>
/// DTO for shipping rate.
/// </summary>
public record ShippingRateDto
{
    public string RateId { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public string ServiceLevel { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public int? EstimatedDays { get; init; }
}

/// <summary>
/// DTO for return summary on dashboard.
/// </summary>
public record ReturnSummaryDto
{
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int ShippedCount { get; init; }
    public int ReceivedCount { get; init; }
    public int TotalCount { get; init; }
    public decimal TotalRefundAmount { get; init; }
    public decimal TotalShippingCost { get; init; }
    public List<ReturnRequestDto> RecentReturns { get; init; } = new();
}
