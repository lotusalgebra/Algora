using Algora.Application.DTOs.Inventory;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for calculating and managing inventory predictions.
/// </summary>
public interface IInventoryPredictionService
{
    /// <summary>
    /// Calculate predictions for all products in a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="lookbackDays">Number of days to analyze sales history</param>
    /// <returns>Number of predictions updated</returns>
    Task<int> CalculatePredictionsAsync(string shopDomain, int lookbackDays = 90);

    /// <summary>
    /// Calculate prediction for a specific product.
    /// </summary>
    Task<InventoryPredictionDto?> CalculatePredictionForProductAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId = null);

    /// <summary>
    /// Get all predictions for a shop with optional filtering.
    /// </summary>
    Task<PaginatedResult<InventoryPredictionDto>> GetPredictionsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get a summary of inventory health.
    /// </summary>
    Task<InventoryPredictionSummaryDto> GetPredictionSummaryAsync(string shopDomain);

    /// <summary>
    /// Get products at risk of stockout within specified days.
    /// </summary>
    Task<IReadOnlyList<InventoryPredictionDto>> GetAtRiskProductsAsync(
        string shopDomain,
        int withinDays = 14);

    /// <summary>
    /// Get sales velocity data for a product.
    /// </summary>
    Task<ProductSalesVelocityDto?> GetSalesVelocityAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId = null);
}
