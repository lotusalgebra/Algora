using Algora.Application.DTOs.Bundles;
using Algora.Application.DTOs.Inventory;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing product bundles.
/// </summary>
public interface IBundleService
{
    #region Bundle CRUD

    /// <summary>
    /// Gets all bundles for a shop with optional filtering.
    /// </summary>
    Task<PaginatedResult<BundleListDto>> GetBundlesAsync(
        string shopDomain,
        string? bundleType = null,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets active bundles for customer-facing pages.
    /// </summary>
    Task<List<BundleDto>> GetActiveBundlesAsync(string shopDomain);

    /// <summary>
    /// Gets a bundle by ID.
    /// </summary>
    Task<BundleDto?> GetBundleByIdAsync(int bundleId);

    /// <summary>
    /// Gets a bundle by slug for customer-facing pages.
    /// </summary>
    Task<BundleDto?> GetBundleBySlugAsync(string shopDomain, string slug);

    /// <summary>
    /// Creates a new bundle.
    /// </summary>
    Task<BundleDto> CreateBundleAsync(string shopDomain, CreateBundleDto dto);

    /// <summary>
    /// Updates an existing bundle.
    /// </summary>
    Task<BundleDto?> UpdateBundleAsync(int bundleId, UpdateBundleDto dto);

    /// <summary>
    /// Deletes a bundle.
    /// </summary>
    Task<bool> DeleteBundleAsync(int bundleId);

    /// <summary>
    /// Archives a bundle (soft delete).
    /// </summary>
    Task<bool> ArchiveBundleAsync(int bundleId);

    /// <summary>
    /// Activates a bundle.
    /// </summary>
    Task<bool> ActivateBundleAsync(int bundleId);

    #endregion

    #region Bundle Items

    /// <summary>
    /// Adds an item to a fixed bundle.
    /// </summary>
    Task<BundleItemDto?> AddBundleItemAsync(int bundleId, CreateBundleItemDto dto);

    /// <summary>
    /// Removes an item from a bundle.
    /// </summary>
    Task<bool> RemoveBundleItemAsync(int itemId);

    /// <summary>
    /// Updates bundle item quantity or order.
    /// </summary>
    Task<BundleItemDto?> UpdateBundleItemAsync(int itemId, int? quantity, int? displayOrder);

    #endregion

    #region Bundle Rules

    /// <summary>
    /// Adds a rule to a mix-and-match bundle.
    /// </summary>
    Task<BundleRuleDto?> AddBundleRuleAsync(int bundleId, CreateBundleRuleDto dto);

    /// <summary>
    /// Removes a rule from a bundle.
    /// </summary>
    Task<bool> RemoveBundleRuleAsync(int ruleId);

    /// <summary>
    /// Gets eligible products for a bundle rule.
    /// </summary>
    Task<List<EligibleProductDto>> GetEligibleProductsAsync(int bundleId, int? ruleId = null);

    #endregion

    #region Price Calculation

    /// <summary>
    /// Calculates the price for a customer's bundle selection.
    /// </summary>
    Task<BundlePriceCalculationDto> CalculateBundlePriceAsync(CustomerBundleSelectionDto selection);

    /// <summary>
    /// Generates a cart URL for a bundle selection.
    /// </summary>
    Task<BundleCartUrlDto> GenerateCartUrlAsync(string shopDomain, CustomerBundleSelectionDto selection);

    /// <summary>
    /// Calculates the available quantity for a fixed bundle based on component inventory.
    /// </summary>
    Task<int> CalculateAvailableQuantityAsync(int bundleId);

    #endregion

    #region Inventory

    /// <summary>
    /// Updates inventory cache for bundle items.
    /// </summary>
    Task UpdateBundleInventoryAsync(int bundleId);

    /// <summary>
    /// Updates inventory cache for all bundles in a shop.
    /// </summary>
    Task UpdateAllBundleInventoryAsync(string shopDomain);

    #endregion

    #region Settings

    /// <summary>
    /// Gets bundle settings for a shop.
    /// </summary>
    Task<BundleSettingsDto> GetSettingsAsync(string shopDomain);

    /// <summary>
    /// Updates bundle settings for a shop.
    /// </summary>
    Task<BundleSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateBundleSettingsDto dto);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets bundle analytics summary for a shop.
    /// </summary>
    Task<BundleAnalyticsSummaryDto> GetAnalyticsSummaryAsync(string shopDomain);

    /// <summary>
    /// Gets performance data for a specific bundle.
    /// </summary>
    Task<BundlePerformanceDto?> GetBundlePerformanceAsync(int bundleId);

    #endregion
}
