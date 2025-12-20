using Algora.Application.DTOs.Bundles;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for syncing bundles to Shopify as products.
/// </summary>
public interface IBundleShopifyService
{
    /// <summary>
    /// Syncs a bundle to Shopify as a product.
    /// Creates a new product or updates an existing one.
    /// </summary>
    /// <param name="bundleId">The bundle ID to sync.</param>
    /// <returns>The updated bundle DTO with Shopify product info.</returns>
    Task<BundleDto?> SyncBundleToShopifyAsync(int bundleId);

    /// <summary>
    /// Removes a bundle product from Shopify.
    /// </summary>
    /// <param name="bundleId">The bundle ID to remove.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RemoveBundleFromShopifyAsync(int bundleId);

    /// <summary>
    /// Updates the bundle product price in Shopify.
    /// </summary>
    /// <param name="bundleId">The bundle ID to update.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateBundlePriceInShopifyAsync(int bundleId);

    /// <summary>
    /// Updates the bundle product inventory in Shopify based on component inventory.
    /// </summary>
    /// <param name="bundleId">The bundle ID to update.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateBundleInventoryInShopifyAsync(int bundleId);

    /// <summary>
    /// Syncs all active bundles to Shopify for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain.</param>
    /// <returns>Number of bundles synced.</returns>
    Task<int> SyncAllBundlesToShopifyAsync(string shopDomain);

    /// <summary>
    /// Gets the Shopify product URL for a synced bundle.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <returns>The product URL or null if not synced.</returns>
    Task<string?> GetShopifyProductUrlAsync(int bundleId);
}
