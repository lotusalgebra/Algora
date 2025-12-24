using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing Shopify locations and inventory levels.
/// </summary>
public interface ILocationService
{
    // Location management
    Task<IEnumerable<LocationDto>> GetLocationsAsync(string shopDomain, bool activeOnly = true);
    Task<LocationDto?> GetLocationAsync(int id);
    Task SyncLocationsFromShopifyAsync(string shopDomain);

    // Inventory levels
    Task<IEnumerable<InventoryLevelDto>> GetInventoryLevelsAsync(string shopDomain, int? locationId = null, int? productId = null);
    Task<InventoryLevelDto?> GetInventoryLevelAsync(int productId, int locationId, int? productVariantId = null);
    Task SyncInventoryLevelsAsync(string shopDomain, int? locationId = null);
    Task<InventoryLevelDto> AdjustInventoryAsync(AdjustInventoryDto dto);
    Task<TransferResultDto> TransferInventoryAsync(TransferInventoryDto dto);

    // Per-product thresholds
    Task<ProductInventoryThresholdDto?> GetProductThresholdAsync(int productId, int? productVariantId = null);
    Task<IEnumerable<ProductInventoryThresholdDto>> GetProductThresholdsAsync(string shopDomain);
    Task<ProductInventoryThresholdDto> SetProductThresholdAsync(SetProductThresholdDto dto);
    Task DeleteProductThresholdAsync(int productId, int? productVariantId = null);

    // Inventory summary
    Task<InventorySummaryDto> GetInventorySummaryAsync(string shopDomain);
}
