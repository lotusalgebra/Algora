using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing upsell offers and generating recommendations.
/// </summary>
public interface IUpsellRecommendationService
{
    /// <summary>
    /// Get upsell offers for a completed order, considering experiments and affinities.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="platformOrderId">Platform order ID</param>
    /// <param name="sessionId">Session ID for experiment consistency</param>
    /// <returns>List of upsell offers with cart URLs</returns>
    Task<List<UpsellOfferDto>> GetOffersForOrderAsync(
        string shopDomain,
        long platformOrderId,
        string sessionId);

    /// <summary>
    /// Get upsell offers for specific products (preview mode).
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="productIds">List of product IDs to get recommendations for</param>
    /// <param name="maxOffers">Maximum number of offers to return</param>
    /// <returns>List of upsell offers</returns>
    Task<List<UpsellOfferDto>> GetOffersForProductsAsync(
        string shopDomain,
        List<long> productIds,
        int maxOffers = 3);

    /// <summary>
    /// Generate a Shopify cart URL with products pre-added.
    /// Format: https://{shop}.myshopify.com/cart/{variant_id}:{quantity}[,{variant_id}:{quantity}...]
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="items">List of cart items</param>
    /// <param name="discountCode">Optional discount code to apply</param>
    /// <returns>Shopify cart URL</returns>
    Task<string> GenerateCartUrlAsync(
        string shopDomain,
        List<CartItemRequest> items,
        string? discountCode = null);

    /// <summary>
    /// Get product affinity recommendations based on order history analysis.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="productId">Platform product ID</param>
    /// <param name="limit">Maximum number of recommendations</param>
    /// <returns>List of affinity-based recommendations</returns>
    Task<List<ProductAffinityDto>> GetAffinityRecommendationsAsync(
        string shopDomain,
        long productId,
        int limit = 5);

    /// <summary>
    /// Get all configured upsell offers for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of offers</returns>
    Task<PaginatedResult<UpsellOfferDto>> GetOffersAsync(
        string shopDomain,
        bool? isActive = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get a single upsell offer by ID.
    /// </summary>
    /// <param name="offerId">Offer ID</param>
    /// <returns>Offer details or null if not found</returns>
    Task<UpsellOfferDto?> GetOfferByIdAsync(int offerId);

    /// <summary>
    /// Create a new upsell offer.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="dto">Offer creation data</param>
    /// <returns>Created offer</returns>
    Task<UpsellOfferDto> CreateOfferAsync(string shopDomain, CreateUpsellOfferDto dto);

    /// <summary>
    /// Update an existing upsell offer.
    /// </summary>
    /// <param name="offerId">Offer ID to update</param>
    /// <param name="dto">Updated offer data</param>
    /// <returns>Updated offer</returns>
    Task<UpsellOfferDto> UpdateOfferAsync(int offerId, CreateUpsellOfferDto dto);

    /// <summary>
    /// Delete an upsell offer.
    /// </summary>
    /// <param name="offerId">Offer ID to delete</param>
    Task DeleteOfferAsync(int offerId);

    /// <summary>
    /// Get upsell settings for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <returns>Settings configuration</returns>
    Task<UpsellSettingsDto> GetSettingsAsync(string shopDomain);

    /// <summary>
    /// Update upsell settings for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="dto">Updated settings</param>
    /// <returns>Updated settings</returns>
    Task<UpsellSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateUpsellSettingsDto dto);
}
