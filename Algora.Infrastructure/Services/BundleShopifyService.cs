using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for syncing bundles to Shopify as products.
/// </summary>
public class BundleShopifyService : IBundleShopifyService
{
    private readonly AppDbContext _db;
    private readonly IShopifyGraphClient _shopifyClient;
    private readonly IBundleService _bundleService;
    private readonly ILogger<BundleShopifyService> _logger;

    public BundleShopifyService(
        AppDbContext db,
        IShopifyGraphClient shopifyClient,
        IBundleService bundleService,
        ILogger<BundleShopifyService> logger)
    {
        _db = db;
        _shopifyClient = shopifyClient;
        _bundleService = bundleService;
        _logger = logger;
    }

    public async Task<BundleDto?> SyncBundleToShopifyAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null)
        {
            _logger.LogWarning("Bundle {BundleId} not found for Shopify sync", bundleId);
            return null;
        }

        try
        {
            var settings = await _db.BundleSettings
                .FirstOrDefaultAsync(s => s.ShopDomain == bundle.ShopDomain);

            var productType = settings?.ShopifyProductType ?? "Bundle";
            var tags = settings?.ShopifyProductTags ?? "bundle";

            // Build description with component info
            var description = bundle.Description ?? "";
            if (bundle.BundleType == "fixed" && bundle.Items.Any())
            {
                description += "\n\n<h4>This bundle includes:</h4>\n<ul>";
                foreach (var item in bundle.Items.OrderBy(i => i.DisplayOrder))
                {
                    description += $"\n<li>{item.Quantity}x {item.ProductTitle}";
                    if (!string.IsNullOrEmpty(item.VariantTitle))
                        description += $" ({item.VariantTitle})";
                    description += "</li>";
                }
                description += "\n</ul>";
            }

            if (bundle.ShopifyProductId.HasValue)
            {
                // Update existing product
                var updateResult = await UpdateShopifyProductAsync(
                    bundle.ShopDomain,
                    bundle.ShopifyProductId.Value,
                    bundle.Name,
                    description,
                    bundle.BundlePrice,
                    bundle.ImageUrl,
                    productType,
                    tags);

                if (updateResult)
                {
                    bundle.ShopifySyncStatus = "synced";
                    bundle.ShopifySyncError = null;
                    bundle.ShopifySyncedAt = DateTime.UtcNow;
                }
                else
                {
                    bundle.ShopifySyncStatus = "error";
                    bundle.ShopifySyncError = "Failed to update product in Shopify";
                }
            }
            else
            {
                // Create new product
                var createResult = await CreateShopifyProductAsync(
                    bundle.ShopDomain,
                    bundle.Name,
                    description,
                    bundle.BundlePrice,
                    bundle.ImageUrl,
                    productType,
                    tags);

                if (createResult.HasValue)
                {
                    bundle.ShopifyProductId = createResult.Value.ProductId;
                    bundle.ShopifyVariantId = createResult.Value.VariantId;
                    bundle.ShopifySyncStatus = "synced";
                    bundle.ShopifySyncError = null;
                    bundle.ShopifySyncedAt = DateTime.UtcNow;
                }
                else
                {
                    bundle.ShopifySyncStatus = "error";
                    bundle.ShopifySyncError = "Failed to create product in Shopify";
                }
            }

            bundle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await _bundleService.GetBundleByIdAsync(bundleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bundle {BundleId} to Shopify", bundleId);

            bundle.ShopifySyncStatus = "error";
            bundle.ShopifySyncError = ex.Message;
            bundle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await _bundleService.GetBundleByIdAsync(bundleId);
        }
    }

    public async Task<bool> RemoveBundleFromShopifyAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);

        if (bundle == null || !bundle.ShopifyProductId.HasValue)
        {
            return false;
        }

        try
        {
            var success = await DeleteShopifyProductAsync(bundle.ShopDomain, bundle.ShopifyProductId.Value);

            if (success)
            {
                bundle.ShopifyProductId = null;
                bundle.ShopifyVariantId = null;
                bundle.ShopifySyncStatus = "pending";
                bundle.ShopifySyncError = null;
                bundle.ShopifySyncedAt = null;
                bundle.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle {BundleId} from Shopify", bundleId);
            return false;
        }
    }

    public async Task<bool> UpdateBundlePriceInShopifyAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);

        if (bundle == null || !bundle.ShopifyProductId.HasValue || !bundle.ShopifyVariantId.HasValue)
        {
            return false;
        }

        try
        {
            return await UpdateShopifyVariantPriceAsync(
                bundle.ShopDomain,
                bundle.ShopifyVariantId.Value,
                bundle.BundlePrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle {BundleId} price in Shopify", bundleId);
            return false;
        }
    }

    public async Task<bool> UpdateBundleInventoryInShopifyAsync(int bundleId)
    {
        var bundle = await _db.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == bundleId);

        if (bundle == null || !bundle.ShopifyVariantId.HasValue || bundle.BundleType != "fixed")
        {
            return false;
        }

        try
        {
            // Calculate available quantity
            var availableQty = await _bundleService.CalculateAvailableQuantityAsync(bundleId);

            // Update Shopify inventory
            return await UpdateShopifyInventoryAsync(
                bundle.ShopDomain,
                bundle.ShopifyVariantId.Value,
                availableQty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle {BundleId} inventory in Shopify", bundleId);
            return false;
        }
    }

    public async Task<int> SyncAllBundlesToShopifyAsync(string shopDomain)
    {
        var bundles = await _db.Bundles
            .Where(b => b.ShopDomain == shopDomain && b.IsActive && b.Status == "active")
            .ToListAsync();

        var syncedCount = 0;

        foreach (var bundle in bundles)
        {
            var result = await SyncBundleToShopifyAsync(bundle.Id);
            if (result != null && result.ShopifySyncStatus == "synced")
            {
                syncedCount++;
            }
        }

        return syncedCount;
    }

    public async Task<string?> GetShopifyProductUrlAsync(int bundleId)
    {
        var bundle = await _db.Bundles.FindAsync(bundleId);

        if (bundle == null || !bundle.ShopifyProductId.HasValue)
        {
            return null;
        }

        return $"https://{bundle.ShopDomain}/products/{bundle.Slug}";
    }

    #region Private Shopify API Methods

    private async Task<(long ProductId, long VariantId)?> CreateShopifyProductAsync(
        string shopDomain,
        string title,
        string description,
        decimal price,
        string? imageUrl,
        string productType,
        string tags)
    {
        // GraphQL mutation to create product
        var mutation = @"
            mutation productCreate($input: ProductInput!) {
                productCreate(input: $input) {
                    product {
                        id
                        variants(first: 1) {
                            edges {
                                node {
                                    id
                                }
                            }
                        }
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                title,
                descriptionHtml = description,
                productType,
                tags = tags.Split(',').Select(t => t.Trim()).ToArray(),
                variants = new[]
                {
                    new { price = price.ToString("F2") }
                }
            }
        };

        try
        {
            var response = await _shopifyClient.QueryAsync<CreateProductResponse>(mutation, variables);

            if (response?.ProductCreate?.Product != null)
            {
                var productGid = response.ProductCreate.Product.Id;
                var variantGid = response.ProductCreate.Product.Variants?.Edges?.FirstOrDefault()?.Node?.Id;

                // Parse GIDs to get numeric IDs
                var productId = ParseShopifyGid(productGid);
                var variantId = variantGid != null ? ParseShopifyGid(variantGid) : 0;

                if (productId > 0)
                {
                    return (productId, variantId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Shopify product for bundle");
        }

        return null;
    }

    private async Task<bool> UpdateShopifyProductAsync(
        string shopDomain,
        long productId,
        string title,
        string description,
        decimal price,
        string? imageUrl,
        string productType,
        string tags)
    {
        var mutation = @"
            mutation productUpdate($input: ProductInput!) {
                productUpdate(input: $input) {
                    product {
                        id
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                id = $"gid://shopify/Product/{productId}",
                title,
                descriptionHtml = description,
                productType,
                tags = tags.Split(',').Select(t => t.Trim()).ToArray()
            }
        };

        try
        {
            var response = await _shopifyClient.QueryAsync<UpdateProductResponse>(mutation, variables);
            return response?.ProductUpdate?.Product != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Shopify product {ProductId}", productId);
            return false;
        }
    }

    private async Task<bool> DeleteShopifyProductAsync(string shopDomain, long productId)
    {
        var mutation = @"
            mutation productDelete($input: ProductDeleteInput!) {
                productDelete(input: $input) {
                    deletedProductId
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                id = $"gid://shopify/Product/{productId}"
            }
        };

        try
        {
            var response = await _shopifyClient.QueryAsync<DeleteProductResponse>(mutation, variables);
            return response?.ProductDelete?.DeletedProductId != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Shopify product {ProductId}", productId);
            return false;
        }
    }

    private async Task<bool> UpdateShopifyVariantPriceAsync(string shopDomain, long variantId, decimal price)
    {
        var mutation = @"
            mutation productVariantUpdate($input: ProductVariantInput!) {
                productVariantUpdate(input: $input) {
                    productVariant {
                        id
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                id = $"gid://shopify/ProductVariant/{variantId}",
                price = price.ToString("F2")
            }
        };

        try
        {
            var response = await _shopifyClient.QueryAsync<UpdateVariantResponse>(mutation, variables);
            return response?.ProductVariantUpdate?.ProductVariant != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Shopify variant {VariantId} price", variantId);
            return false;
        }
    }

    private async Task<bool> UpdateShopifyInventoryAsync(string shopDomain, long variantId, int quantity)
    {
        // This would require getting the inventory item ID first, then updating
        // For now, return true as a placeholder
        _logger.LogInformation("Would update inventory for variant {VariantId} to {Quantity}", variantId, quantity);
        return await Task.FromResult(true);
    }

    private static long ParseShopifyGid(string gid)
    {
        // Parse "gid://shopify/Product/12345" to 12345
        var parts = gid.Split('/');
        if (parts.Length > 0 && long.TryParse(parts[^1], out var id))
        {
            return id;
        }
        return 0;
    }

    #endregion

    #region Response Types

    private class CreateProductResponse
    {
        public ProductCreatePayload? ProductCreate { get; set; }
    }

    private class ProductCreatePayload
    {
        public ProductNode? Product { get; set; }
    }

    private class ProductNode
    {
        public string Id { get; set; } = string.Empty;
        public VariantConnection? Variants { get; set; }
    }

    private class VariantConnection
    {
        public List<VariantEdge>? Edges { get; set; }
    }

    private class VariantEdge
    {
        public VariantNode? Node { get; set; }
    }

    private class VariantNode
    {
        public string Id { get; set; } = string.Empty;
    }

    private class UpdateProductResponse
    {
        public ProductUpdatePayload? ProductUpdate { get; set; }
    }

    private class ProductUpdatePayload
    {
        public ProductNode? Product { get; set; }
    }

    private class DeleteProductResponse
    {
        public ProductDeletePayload? ProductDelete { get; set; }
    }

    private class ProductDeletePayload
    {
        public string? DeletedProductId { get; set; }
    }

    private class UpdateVariantResponse
    {
        public ProductVariantUpdatePayload? ProductVariantUpdate { get; set; }
    }

    private class ProductVariantUpdatePayload
    {
        public VariantNode? ProductVariant { get; set; }
    }

    #endregion
}
