namespace Algora.Application.DTOs.Operations;

// ==================== Purchase Order DTOs ====================

public record PurchaseOrderDto(
    int Id,
    string ShopDomain,
    int SupplierId,
    string SupplierName,
    string OrderNumber,
    string Status,
    int? LocationId,
    string? LocationName,
    decimal Subtotal,
    decimal Tax,
    decimal Shipping,
    decimal Total,
    string Currency,
    string? Notes,
    string? SupplierReference,
    string? TrackingNumber,
    DateTime? ExpectedDeliveryDate,
    DateTime? OrderedAt,
    DateTime? ConfirmedAt,
    DateTime? ShippedAt,
    DateTime? ReceivedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<PurchaseOrderLineDto> Lines
);

public record CreatePurchaseOrderDto(
    string ShopDomain,
    int SupplierId,
    int? LocationId = null,
    string? Notes = null,
    DateTime? ExpectedDeliveryDate = null,
    List<CreatePurchaseOrderLineDto>? Lines = null,
    string Currency = "USD"
);

public record CreatePurchaseOrderLineDto(
    int ProductId,
    int? ProductVariantId = null,
    int QuantityOrdered = 1,
    decimal UnitCost = 0
);

public record UpdatePurchaseOrderDto(
    int? SupplierId = null,
    int? LocationId = null,
    string? Notes = null,
    decimal? Tax = null,
    decimal? Shipping = null
);

// ==================== Purchase Order Line DTOs ====================

public record PurchaseOrderLineDto(
    int Id,
    int PurchaseOrderId,
    int ProductId,
    int? ProductVariantId,
    string? Sku,
    string ProductTitle,
    string? VariantTitle,
    int QuantityOrdered,
    int QuantityReceived,
    decimal UnitCost,
    decimal TotalCost,
    DateTime? ReceivedAt,
    string? ReceivingNotes
);

public record AddPurchaseOrderLineDto(
    int ProductId,
    int? ProductVariantId = null,
    int QuantityOrdered = 1,
    decimal? UnitCost = null
);

public record UpdatePurchaseOrderLineDto(
    int? QuantityOrdered = null,
    decimal? UnitCost = null
);

// ==================== Receiving DTOs ====================

public record ReceiveItemsDto(
    List<ReceiveLineDto> Lines,
    string? Notes = null
);

public record ReceiveLineDto(
    int LineId,
    int QuantityReceived
);

public record ReceiveLineItemDto(
    int LineId,
    int QuantityReceived,
    string? Notes = null
);

// ==================== Filter DTOs ====================

public record PurchaseOrderFilterDto(
    string? Status = null,
    int? SupplierId = null,
    int? LocationId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null
);

// ==================== Suggestion DTOs ====================

public record SuggestedPurchaseOrderDto(
    int SupplierId,
    string SupplierName,
    string? SupplierEmail,
    int? LocationId,
    string? LocationName,
    List<SuggestedLineItemDto> Lines,
    decimal EstimatedTotal,
    string Currency,
    string Reason
);

public record SuggestedLineItemDto(
    int ProductId,
    int? ProductVariantId,
    string ProductTitle,
    string? VariantTitle,
    string? Sku,
    int CurrentStock,
    int SuggestedQuantity,
    decimal UnitCost,
    int DaysUntilStockout,
    decimal AverageDailySales
);
