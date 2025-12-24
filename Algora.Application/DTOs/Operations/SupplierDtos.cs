namespace Algora.Application.DTOs.Operations;

// ==================== Supplier DTOs ====================

public record SupplierDto(
    int Id,
    string ShopDomain,
    string Name,
    string? Code,
    string? Email,
    string? Phone,
    string? Address,
    string? ContactPerson,
    string? Website,
    int DefaultLeadTimeDays,
    decimal? MinimumOrderAmount,
    string? PaymentTerms,
    string? Notes,
    bool IsActive,
    int TotalOrders,
    decimal TotalSpent,
    decimal? AverageDeliveryDays,
    decimal? OnTimeDeliveryRate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateSupplierDto(
    string ShopDomain,
    string Name,
    string? Code = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? ContactPerson = null,
    string? Website = null,
    int DefaultLeadTimeDays = 7,
    decimal? MinimumOrderAmount = null,
    string? PaymentTerms = null,
    string? Notes = null
);

public record UpdateSupplierDto(
    string Name,
    string? Code = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? ContactPerson = null,
    string? Website = null,
    int DefaultLeadTimeDays = 7,
    decimal? MinimumOrderAmount = null,
    string? PaymentTerms = null,
    string? Notes = null,
    bool IsActive = true
);

// ==================== SupplierProduct DTOs ====================

public record SupplierProductDto(
    int Id,
    int SupplierId,
    string SupplierName,
    int ProductId,
    string ProductTitle,
    int? ProductVariantId,
    string? VariantTitle,
    string? SupplierSku,
    string? SupplierProductName,
    decimal UnitCost,
    int MinimumOrderQuantity,
    int? LeadTimeDays,
    bool IsPreferred,
    DateTime? LastOrderedAt,
    DateTime CreatedAt
);

public record AddSupplierProductDto(
    int ProductId,
    int? ProductVariantId = null,
    string? SupplierSku = null,
    string? SupplierProductName = null,
    decimal UnitCost = 0,
    int MinimumOrderQuantity = 1,
    int? LeadTimeDays = null,
    bool IsPreferred = false
);

public record UpdateSupplierProductDto(
    string? SupplierSku = null,
    string? SupplierProductName = null,
    decimal? UnitCost = null,
    int? MinimumOrderQuantity = null,
    int? LeadTimeDays = null,
    bool? IsPreferred = null
);

// ==================== Analytics DTOs ====================

public record SupplierAnalyticsDto(
    int SupplierId,
    string SupplierName,
    int TotalOrders,
    decimal TotalSpent,
    int PendingOrders,
    decimal PendingOrdersValue,
    decimal? AverageDeliveryDays,
    decimal? OnTimeDeliveryRate,
    int ProductsSupplied,
    List<RecentOrderDto> RecentOrders,
    List<TopProductDto> TopProducts
);

public record RecentOrderDto(
    int OrderId,
    string OrderNumber,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    DateTime? ReceivedAt
);

public record TopProductDto(
    int ProductId,
    string ProductTitle,
    int TotalQuantityOrdered,
    decimal TotalSpent,
    int OrderCount
);
