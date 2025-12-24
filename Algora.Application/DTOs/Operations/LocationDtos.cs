namespace Algora.Application.DTOs.Operations;

// ==================== Location DTOs ====================

public record LocationDto(
    int Id,
    string ShopDomain,
    long ShopifyLocationId,
    string Name,
    string? Address1,
    string? Address2,
    string? City,
    string? Province,
    string? ProvinceCode,
    string? Country,
    string? CountryCode,
    string? Zip,
    string? Phone,
    bool IsActive,
    bool IsPrimary,
    bool FulfillsOnlineOrders,
    DateTime? LastSyncedAt,
    DateTime CreatedAt,
    int TotalProducts,
    int TotalInventory
);

// ==================== Inventory Level DTOs ====================

public record InventoryLevelDto(
    int Id,
    string ShopDomain,
    int LocationId,
    string LocationName,
    int ProductId,
    string ProductTitle,
    int? ProductVariantId,
    string? VariantTitle,
    string? Sku,
    long ShopifyInventoryItemId,
    int Available,
    int Incoming,
    int Committed,
    int? OnHand,
    DateTime? LastSyncedAt
);

public record AdjustInventoryDto(
    string ShopDomain,
    int ProductId,
    int? ProductVariantId,
    int LocationId,
    int Adjustment,
    string Reason
);

public record TransferInventoryDto(
    string ShopDomain,
    int ProductId,
    int? ProductVariantId,
    int FromLocationId,
    int ToLocationId,
    int Quantity,
    string? Notes = null
);

public record TransferResultDto(
    bool Success,
    string Message,
    int QuantityTransferred,
    InventoryLevelDto? FromLevel,
    InventoryLevelDto? ToLevel
);

// ==================== Product Threshold DTOs ====================

public record ProductInventoryThresholdDto(
    int Id,
    string ShopDomain,
    int ProductId,
    string ProductTitle,
    int? ProductVariantId,
    string? VariantTitle,
    int? LowStockThreshold,
    int? CriticalStockThreshold,
    int? ReorderPoint,
    int? ReorderQuantity,
    int? SafetyStockDays,
    int? LeadTimeDays,
    int? PreferredSupplierId,
    string? PreferredSupplierName,
    bool AutoReorderEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SetProductThresholdDto(
    string ShopDomain,
    int ProductId,
    int? ProductVariantId = null,
    int? LowStockThreshold = null,
    int? CriticalStockThreshold = null,
    int? ReorderPoint = null,
    int? ReorderQuantity = null,
    int? SafetyStockDays = null,
    int? LeadTimeDays = null,
    int? PreferredSupplierId = null,
    bool AutoReorderEnabled = false
);

// ==================== Summary DTOs ====================

public record InventorySummaryDto(
    string ShopDomain,
    int TotalLocations,
    int TotalProducts,
    int TotalInventory,
    int LowStockProducts,
    int CriticalStockProducts,
    int OutOfStockProducts,
    List<LocationInventorySummaryDto> LocationSummaries
);

public record LocationInventorySummaryDto(
    int LocationId,
    string LocationName,
    int TotalProducts,
    int TotalInventory,
    int LowStockProducts,
    int OutOfStockProducts
);
