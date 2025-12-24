using Algora.Application.DTOs.Operations;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Operations;

/// <summary>
/// Service for managing Shopify locations and inventory levels.
/// </summary>
public class LocationService : ILocationService
{
    private readonly AppDbContext _db;
    private readonly IShopifyGraphClient _shopifyClient;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        AppDbContext db,
        IShopifyGraphClient shopifyClient,
        ILogger<LocationService> logger)
    {
        _db = db;
        _shopifyClient = shopifyClient;
        _logger = logger;
    }

    public async Task<IEnumerable<LocationDto>> GetLocationsAsync(string shopDomain, bool activeOnly = true)
    {
        var query = _db.Locations.Where(l => l.ShopDomain == shopDomain);

        if (activeOnly)
            query = query.Where(l => l.IsActive);

        var locations = await query.OrderBy(l => l.Name).ToListAsync();

        var result = new List<LocationDto>();
        foreach (var loc in locations)
        {
            var stats = await _db.InventoryLevels
                .Where(il => il.LocationId == loc.Id)
                .GroupBy(il => 1)
                .Select(g => new
                {
                    TotalProducts = g.Count(),
                    TotalInventory = g.Sum(il => il.Available)
                })
                .FirstOrDefaultAsync();

            result.Add(MapToDto(loc, stats?.TotalProducts ?? 0, stats?.TotalInventory ?? 0));
        }

        return result;
    }

    public async Task<LocationDto?> GetLocationAsync(int id)
    {
        var location = await _db.Locations.FindAsync(id);
        if (location == null) return null;

        var stats = await _db.InventoryLevels
            .Where(il => il.LocationId == id)
            .GroupBy(il => 1)
            .Select(g => new
            {
                TotalProducts = g.Count(),
                TotalInventory = g.Sum(il => il.Available)
            })
            .FirstOrDefaultAsync();

        return MapToDto(location, stats?.TotalProducts ?? 0, stats?.TotalInventory ?? 0);
    }

    public async Task SyncLocationsFromShopifyAsync(string shopDomain)
    {
        try
        {
            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
            if (shop?.OfflineAccessToken == null)
            {
                _logger.LogWarning("No access token for shop {ShopDomain}", shopDomain);
                return;
            }

            // Fetch locations from Shopify using GraphQL
            var query = @"
                query {
                    locations(first: 50) {
                        nodes {
                            id
                            name
                            isActive
                            isPrimary
                            fulfillsOnlineOrders
                            address {
                                address1
                                address2
                                city
                                province
                                provinceCode
                                country
                                countryCode
                                zip
                                phone
                            }
                        }
                    }
                }";

            var result = await _shopifyClient.QueryAsync<LocationsResponse>(shopDomain, query);

            if (result?.Data?.Locations?.Nodes == null)
            {
                _logger.LogWarning("No locations returned from Shopify for {ShopDomain}", shopDomain);
                return;
            }

            foreach (var shopifyLoc in result.Data.Locations.Nodes)
            {
                var locationId = ExtractIdFromGid(shopifyLoc.Id);

                var location = await _db.Locations
                    .FirstOrDefaultAsync(l => l.ShopDomain == shopDomain && l.ShopifyLocationId == locationId);

                if (location == null)
                {
                    location = new Location
                    {
                        ShopDomain = shopDomain,
                        ShopifyLocationId = locationId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Locations.Add(location);
                }

                location.Name = shopifyLoc.Name;
                location.IsActive = shopifyLoc.IsActive;
                location.IsPrimary = shopifyLoc.IsPrimary;
                location.FulfillsOnlineOrders = shopifyLoc.FulfillsOnlineOrders;
                location.Address1 = shopifyLoc.Address?.Address1;
                location.Address2 = shopifyLoc.Address?.Address2;
                location.City = shopifyLoc.Address?.City;
                location.Province = shopifyLoc.Address?.Province;
                location.ProvinceCode = shopifyLoc.Address?.ProvinceCode;
                location.Country = shopifyLoc.Address?.Country;
                location.CountryCode = shopifyLoc.Address?.CountryCode;
                location.Zip = shopifyLoc.Address?.Zip;
                location.Phone = shopifyLoc.Address?.Phone;
                location.LastSyncedAt = DateTime.UtcNow;
                location.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} locations for {ShopDomain}",
                result.Data.Locations.Nodes.Count, shopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync locations for {ShopDomain}", shopDomain);
            throw;
        }
    }

    public async Task<IEnumerable<InventoryLevelDto>> GetInventoryLevelsAsync(
        string shopDomain,
        int? locationId = null,
        int? productId = null)
    {
        var query = _db.InventoryLevels
            .Include(il => il.Location)
            .Include(il => il.Product)
            .Include(il => il.ProductVariant)
            .Where(il => il.ShopDomain == shopDomain);

        if (locationId.HasValue)
            query = query.Where(il => il.LocationId == locationId);

        if (productId.HasValue)
            query = query.Where(il => il.ProductId == productId);

        return await query
            .OrderBy(il => il.Location.Name)
            .ThenBy(il => il.Product.Title)
            .Select(il => MapToDto(il))
            .ToListAsync();
    }

    public async Task<InventoryLevelDto?> GetInventoryLevelAsync(int productId, int locationId, int? productVariantId = null)
    {
        var query = _db.InventoryLevels
            .Include(il => il.Location)
            .Include(il => il.Product)
            .Include(il => il.ProductVariant)
            .Where(il => il.ProductId == productId && il.LocationId == locationId);

        if (productVariantId.HasValue)
            query = query.Where(il => il.ProductVariantId == productVariantId);

        var level = await query.FirstOrDefaultAsync();
        return level == null ? null : MapToDto(level);
    }

    public async Task SyncInventoryLevelsAsync(string shopDomain, int? locationId = null)
    {
        try
        {
            // Get locations to sync
            var locations = await _db.Locations
                .Where(l => l.ShopDomain == shopDomain && l.IsActive)
                .Where(l => !locationId.HasValue || l.Id == locationId)
                .ToListAsync();

            foreach (var location in locations)
            {
                await SyncInventoryLevelsForLocationAsync(shopDomain, location);
            }

            _logger.LogInformation("Synced inventory levels for {Count} locations in {ShopDomain}",
                locations.Count, shopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync inventory levels for {ShopDomain}", shopDomain);
            throw;
        }
    }

    private async Task SyncInventoryLevelsForLocationAsync(string shopDomain, Location location)
    {
        // Simplified: Just query local products and sync from Shopify
        // In a real implementation, you'd use Shopify's inventoryLevels query
        var products = await _db.Products
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .ToListAsync();

        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.ShopDomain == shopDomain && v.Product.IsActive)
            .ToListAsync();

        // For each variant, ensure we have an inventory level record
        foreach (var variant in variants)
        {
            var level = await _db.InventoryLevels
                .FirstOrDefaultAsync(il => il.LocationId == location.Id
                    && il.ProductId == variant.ProductId
                    && il.ProductVariantId == variant.Id);

            if (level == null)
            {
                level = new InventoryLevel
                {
                    ShopDomain = shopDomain,
                    LocationId = location.Id,
                    ProductId = variant.ProductId,
                    ProductVariantId = variant.Id,
                    ShopifyInventoryItemId = variant.PlatformVariantId,
                    Available = variant.InventoryQuantity,
                    CreatedAt = DateTime.UtcNow
                };
                _db.InventoryLevels.Add(level);
            }
            else
            {
                level.Available = variant.InventoryQuantity;
                level.LastSyncedAt = DateTime.UtcNow;
                level.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<InventoryLevelDto> AdjustInventoryAsync(AdjustInventoryDto dto)
    {
        var level = await _db.InventoryLevels
            .Include(il => il.Location)
            .Include(il => il.Product)
            .Include(il => il.ProductVariant)
            .FirstOrDefaultAsync(il => il.ProductId == dto.ProductId
                && il.LocationId == dto.LocationId
                && il.ProductVariantId == dto.ProductVariantId);

        if (level == null)
            throw new InvalidOperationException("Inventory level not found");

        level.Available += dto.Adjustment;
        level.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Adjusted inventory for product {ProductId} at location {LocationId} by {Adjustment}: {Reason}",
            dto.ProductId, dto.LocationId, dto.Adjustment, dto.Reason);

        return MapToDto(level);
    }

    public async Task<TransferResultDto> TransferInventoryAsync(TransferInventoryDto dto)
    {
        var fromLevel = await _db.InventoryLevels
            .Include(il => il.Location)
            .Include(il => il.Product)
            .Include(il => il.ProductVariant)
            .FirstOrDefaultAsync(il => il.ProductId == dto.ProductId
                && il.LocationId == dto.FromLocationId
                && il.ProductVariantId == dto.ProductVariantId);

        if (fromLevel == null)
            return new TransferResultDto(false, "Source inventory level not found", 0, null, null);

        if (fromLevel.Available < dto.Quantity)
            return new TransferResultDto(false, $"Insufficient inventory. Available: {fromLevel.Available}", 0, null, null);

        var toLevel = await _db.InventoryLevels
            .Include(il => il.Location)
            .Include(il => il.Product)
            .Include(il => il.ProductVariant)
            .FirstOrDefaultAsync(il => il.ProductId == dto.ProductId
                && il.LocationId == dto.ToLocationId
                && il.ProductVariantId == dto.ProductVariantId);

        if (toLevel == null)
        {
            // Create inventory level at destination
            toLevel = new InventoryLevel
            {
                ShopDomain = dto.ShopDomain,
                LocationId = dto.ToLocationId,
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                ShopifyInventoryItemId = fromLevel.ShopifyInventoryItemId,
                Available = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.InventoryLevels.Add(toLevel);
        }

        fromLevel.Available -= dto.Quantity;
        toLevel.Available += dto.Quantity;
        fromLevel.UpdatedAt = DateTime.UtcNow;
        toLevel.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(toLevel).Reference(il => il.Location).LoadAsync();

        _logger.LogInformation("Transferred {Quantity} of product {ProductId} from location {FromId} to {ToId}",
            dto.Quantity, dto.ProductId, dto.FromLocationId, dto.ToLocationId);

        return new TransferResultDto(
            true,
            $"Successfully transferred {dto.Quantity} units",
            dto.Quantity,
            MapToDto(fromLevel),
            MapToDto(toLevel)
        );
    }

    public async Task<ProductInventoryThresholdDto?> GetProductThresholdAsync(int productId, int? productVariantId = null)
    {
        var threshold = await _db.ProductInventoryThresholds
            .Include(t => t.Product)
            .Include(t => t.ProductVariant)
            .Include(t => t.PreferredSupplier)
            .FirstOrDefaultAsync(t => t.ProductId == productId && t.ProductVariantId == productVariantId);

        return threshold == null ? null : MapToDto(threshold);
    }

    public async Task<IEnumerable<ProductInventoryThresholdDto>> GetProductThresholdsAsync(string shopDomain)
    {
        return await _db.ProductInventoryThresholds
            .Include(t => t.Product)
            .Include(t => t.ProductVariant)
            .Include(t => t.PreferredSupplier)
            .Where(t => t.ShopDomain == shopDomain)
            .OrderBy(t => t.Product.Title)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<ProductInventoryThresholdDto> SetProductThresholdAsync(SetProductThresholdDto dto)
    {
        var threshold = await _db.ProductInventoryThresholds
            .FirstOrDefaultAsync(t => t.ShopDomain == dto.ShopDomain
                && t.ProductId == dto.ProductId
                && t.ProductVariantId == dto.ProductVariantId);

        if (threshold == null)
        {
            threshold = new ProductInventoryThreshold
            {
                ShopDomain = dto.ShopDomain,
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                CreatedAt = DateTime.UtcNow
            };
            _db.ProductInventoryThresholds.Add(threshold);
        }

        threshold.LowStockThreshold = dto.LowStockThreshold;
        threshold.CriticalStockThreshold = dto.CriticalStockThreshold;
        threshold.ReorderPoint = dto.ReorderPoint;
        threshold.ReorderQuantity = dto.ReorderQuantity;
        threshold.SafetyStockDays = dto.SafetyStockDays;
        threshold.LeadTimeDays = dto.LeadTimeDays;
        threshold.PreferredSupplierId = dto.PreferredSupplierId;
        threshold.AutoReorderEnabled = dto.AutoReorderEnabled;
        threshold.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(threshold).Reference(t => t.Product).LoadAsync();
        if (dto.ProductVariantId.HasValue)
            await _db.Entry(threshold).Reference(t => t.ProductVariant).LoadAsync();
        if (dto.PreferredSupplierId.HasValue)
            await _db.Entry(threshold).Reference(t => t.PreferredSupplier).LoadAsync();

        _logger.LogInformation("Set inventory threshold for product {ProductId}", dto.ProductId);

        return MapToDto(threshold);
    }

    public async Task DeleteProductThresholdAsync(int productId, int? productVariantId = null)
    {
        var threshold = await _db.ProductInventoryThresholds
            .FirstOrDefaultAsync(t => t.ProductId == productId && t.ProductVariantId == productVariantId);

        if (threshold != null)
        {
            _db.ProductInventoryThresholds.Remove(threshold);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted inventory threshold for product {ProductId}", productId);
        }
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(string shopDomain)
    {
        var settings = await _db.InventoryAlertSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        var lowThreshold = settings?.LowStockDaysThreshold ?? 10;
        var criticalThreshold = settings?.CriticalStockDaysThreshold ?? 5;

        var locations = await _db.Locations
            .Where(l => l.ShopDomain == shopDomain && l.IsActive)
            .ToListAsync();

        var inventoryLevels = await _db.InventoryLevels
            .Where(il => il.ShopDomain == shopDomain)
            .ToListAsync();

        var locationSummaries = new List<LocationInventorySummaryDto>();

        foreach (var location in locations)
        {
            var locLevels = inventoryLevels.Where(il => il.LocationId == location.Id).ToList();

            locationSummaries.Add(new LocationInventorySummaryDto(
                location.Id,
                location.Name,
                locLevels.Count,
                locLevels.Sum(il => il.Available),
                locLevels.Count(il => il.Available > 0 && il.Available <= lowThreshold),
                locLevels.Count(il => il.Available <= 0)
            ));
        }

        return new InventorySummaryDto(
            shopDomain,
            locations.Count,
            inventoryLevels.Select(il => il.ProductId).Distinct().Count(),
            inventoryLevels.Sum(il => il.Available),
            inventoryLevels.Count(il => il.Available > 0 && il.Available <= lowThreshold),
            inventoryLevels.Count(il => il.Available > 0 && il.Available <= criticalThreshold),
            inventoryLevels.Count(il => il.Available <= 0),
            locationSummaries
        );
    }

    private static long ExtractIdFromGid(string gid)
    {
        // Format: gid://shopify/Location/12345
        var parts = gid.Split('/');
        return long.TryParse(parts.Last(), out var id) ? id : 0;
    }

    private static LocationDto MapToDto(Location l, int totalProducts, int totalInventory) => new(
        l.Id,
        l.ShopDomain,
        l.ShopifyLocationId,
        l.Name,
        l.Address1,
        l.Address2,
        l.City,
        l.Province,
        l.ProvinceCode,
        l.Country,
        l.CountryCode,
        l.Zip,
        l.Phone,
        l.IsActive,
        l.IsPrimary,
        l.FulfillsOnlineOrders,
        l.LastSyncedAt,
        l.CreatedAt,
        totalProducts,
        totalInventory
    );

    private static InventoryLevelDto MapToDto(InventoryLevel il) => new(
        il.Id,
        il.ShopDomain,
        il.LocationId,
        il.Location?.Name ?? "",
        il.ProductId,
        il.Product?.Title ?? "",
        il.ProductVariantId,
        il.ProductVariant?.Title,
        il.ProductVariant?.Sku ?? il.Product?.Sku,
        il.ShopifyInventoryItemId,
        il.Available,
        il.Incoming,
        il.Committed,
        il.OnHand,
        il.LastSyncedAt
    );

    private static ProductInventoryThresholdDto MapToDto(ProductInventoryThreshold t) => new(
        t.Id,
        t.ShopDomain,
        t.ProductId,
        t.Product?.Title ?? "",
        t.ProductVariantId,
        t.ProductVariant?.Title,
        t.LowStockThreshold,
        t.CriticalStockThreshold,
        t.ReorderPoint,
        t.ReorderQuantity,
        t.SafetyStockDays,
        t.LeadTimeDays,
        t.PreferredSupplierId,
        t.PreferredSupplier?.Name,
        t.AutoReorderEnabled,
        t.CreatedAt,
        t.UpdatedAt
    );

    // Response classes for Shopify GraphQL
    private class LocationsResponse
    {
        public LocationsData? Data { get; set; }
    }

    private class LocationsData
    {
        public LocationsConnection? Locations { get; set; }
    }

    private class LocationsConnection
    {
        public List<ShopifyLocation> Nodes { get; set; } = new();
    }

    private class ShopifyLocation
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
        public bool FulfillsOnlineOrders { get; set; }
        public ShopifyAddress? Address { get; set; }
    }

    private class ShopifyAddress
    {
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? ProvinceCode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
    }
}
