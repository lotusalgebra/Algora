using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for calculating and managing product affinities (co-purchase analysis).
/// </summary>
public interface IProductAffinityService
{
    /// <summary>
    /// Calculate product affinities from order history for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="lookbackDays">Number of days to analyze order history</param>
    /// <returns>Number of affinities calculated</returns>
    Task<int> CalculateAffinitiesAsync(string shopDomain, int lookbackDays = 90);

    /// <summary>
    /// Get top affinity products for a given product.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="productId">Platform product ID</param>
    /// <param name="limit">Maximum number of affinities to return</param>
    /// <returns>List of product affinities ordered by confidence score</returns>
    Task<List<ProductAffinityDto>> GetAffinitiesForProductAsync(
        string shopDomain,
        long productId,
        int limit = 10);

    /// <summary>
    /// Get all calculated affinities for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="minConfidence">Optional minimum confidence score filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of affinities</returns>
    Task<PaginatedResult<ProductAffinityDto>> GetAllAffinitiesAsync(
        string shopDomain,
        decimal? minConfidence = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get affinity summary/stats for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <returns>Summary of affinity calculations</returns>
    Task<AffinitySummaryDto> GetAffinitySummaryAsync(string shopDomain);

    /// <summary>
    /// Delete old affinity records for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <returns>Number of records deleted</returns>
    Task<int> ClearAffinitiesAsync(string shopDomain);
}
