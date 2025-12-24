namespace Algora.Application.DTOs.CustomerHub;

// ==================== Exchange DTOs ====================

public record ExchangeDto(
    int Id,
    string ShopDomain,
    string ExchangeNumber,
    int OrderId,
    string OrderNumber,
    int? CustomerId,
    string CustomerEmail,
    string? CustomerName,
    string Status,
    int? ReturnRequestId,
    int? NewOrderId,
    decimal PriceDifference,
    string Currency,
    string? Notes,
    DateTime? ApprovedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<ExchangeItemDto> Items
);

public record ExchangeItemDto(
    int Id,
    int ExchangeId,
    int OriginalOrderLineId,
    int OriginalProductId,
    int? OriginalProductVariantId,
    string OriginalProductTitle,
    string? OriginalVariantTitle,
    string? OriginalSku,
    decimal OriginalPrice,
    int Quantity,
    int? NewProductId,
    int? NewProductVariantId,
    string? NewProductTitle,
    string? NewVariantTitle,
    string? NewSku,
    decimal? NewPrice,
    string? Reason,
    string? CustomerNote
);

public record CreateExchangeDto(
    string ShopDomain,
    int OrderId,
    string CustomerEmail,
    string? CustomerName,
    IEnumerable<CreateExchangeItemDto> Items,
    string? Notes = null
);

public record CreateExchangeItemDto(
    int OrderLineId,
    int ProductId,
    int? ProductVariantId,
    string ProductTitle,
    string? VariantTitle,
    string? Sku,
    decimal Price,
    int Quantity,
    string? Reason = null,
    string? CustomerNote = null
);

public record UpdateExchangeItemsDto(
    IEnumerable<UpdateExchangeItemDto> Items
);

public record UpdateExchangeItemDto(
    int ExchangeItemId,
    int NewProductId,
    int? NewProductVariantId,
    string NewProductTitle,
    string? NewVariantTitle,
    string? NewSku,
    decimal NewPrice
);

public record ExchangeFilterDto(
    string? Status = null,
    int? CustomerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Skip = 0,
    int Take = 50
);

// ==================== Exchange Eligibility DTOs ====================

public record ExchangeEligibilityDto(
    bool IsEligible,
    string? Reason,
    int DaysSinceOrder,
    int MaxDaysAllowed,
    IEnumerable<ExchangeEligibleItemDto> EligibleItems
);

public record ExchangeEligibleItemDto(
    int OrderLineId,
    int ProductId,
    int? ProductVariantId,
    string ProductTitle,
    string? VariantTitle,
    string? Sku,
    decimal Price,
    int Quantity,
    int QuantityAvailableForExchange
);

// ==================== Exchange Product Options ====================

public record ExchangeProductOptionDto(
    int ProductId,
    int? ProductVariantId,
    string ProductTitle,
    string? VariantTitle,
    string? Sku,
    decimal Price,
    int InventoryQuantity,
    string? ImageUrl,
    bool InStock
);
