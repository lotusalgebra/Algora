using Algora.Application.DTOs;
using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for calculating and managing inventory predictions based on sales history.
/// </summary>
public class InventoryPredictionService : IInventoryPredictionService
{
    private readonly AppDbContext _db;
    private readonly IShopifyProductService _productService;
    private readonly ILogger<InventoryPredictionService> _logger;

    public InventoryPredictionService(
        AppDbContext db,
        IShopifyProductService productService,
        ILogger<InventoryPredictionService> logger)
    {
        _db = db;
        _productService = productService;
        _logger = logger;
    }

    public async Task<int> CalculatePredictionsAsync(string shopDomain, int lookbackDays = 90)
    {
        _logger.LogInformation("Calculating inventory predictions for {Shop} with {Days} day lookback", shopDomain, lookbackDays);

        var settings = await GetOrCreateSettingsAsync(shopDomain);
        var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);

        // Get all products from Shopify
        var products = await _productService.GetProductsAsync(new ProductSearchFilter(null, null, null, null), 250);
        var updatedCount = 0;

        foreach (var product in products)
        {
            try
            {
                // Calculate for each variant
                foreach (var variant in product.Variants)
                {
                    var prediction = await CalculateAndSavePredictionAsync(
                        shopDomain,
                        product.NumericId,
                        ParseVariantId(variant.Id),
                        product.Title,
                        variant.Title,
                        variant.Sku,
                        variant.InventoryQuantity ?? 0,
                        cutoffDate,
                        settings);

                    if (prediction != null)
                        updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating prediction for product {ProductId}", product.NumericId);
            }
        }

        _logger.LogInformation("Updated {Count} inventory predictions for {Shop}", updatedCount, shopDomain);
        return updatedCount;
    }

    public async Task<InventoryPredictionDto?> CalculatePredictionForProductAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId = null)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        var cutoffDate = DateTime.UtcNow.AddDays(-90);

        // Get product from Shopify
        var product = await _productService.GetProductByIdAsync(platformProductId);
        if (product == null) return null;

        var variant = platformVariantId.HasValue
            ? product.Variants.FirstOrDefault(v => ParseVariantId(v.Id) == platformVariantId)
            : product.Variants.FirstOrDefault();

        if (variant == null) return null;

        var prediction = await CalculateAndSavePredictionAsync(
            shopDomain,
            product.NumericId,
            ParseVariantId(variant.Id),
            product.Title,
            variant.Title,
            variant.Sku,
            variant.InventoryQuantity ?? 0,
            cutoffDate,
            settings);

        return prediction != null ? MapToDto(prediction) : null;
    }

    public async Task<PaginatedResult<InventoryPredictionDto>> GetPredictionsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.InventoryPredictions
            .Where(p => p.ShopDomain == shopDomain)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.DaysUntilStockout)
            .ThenBy(p => p.ProductTitle)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<InventoryPredictionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InventoryPredictionSummaryDto> GetPredictionSummaryAsync(string shopDomain)
    {
        var predictions = await _db.InventoryPredictions
            .Where(p => p.ShopDomain == shopDomain)
            .ToListAsync();

        var outOfStock = predictions.Where(p => p.Status == "out_of_stock").ToList();
        var critical = predictions.Where(p => p.Status == "critical").ToList();
        var lowStock = predictions.Where(p => p.Status == "low_stock").ToList();
        var healthy = predictions.Where(p => p.Status == "ok").ToList();

        var topAtRisk = predictions
            .Where(p => p.Status != "ok")
            .OrderBy(p => p.DaysUntilStockout)
            .Take(10)
            .Select(MapToDto)
            .ToList();

        return new InventoryPredictionSummaryDto
        {
            TotalProducts = predictions.Count,
            OutOfStockCount = outOfStock.Count,
            CriticalStockCount = critical.Count,
            LowStockCount = lowStock.Count,
            HealthyStockCount = healthy.Count,
            TopAtRisk = topAtRisk
        };
    }

    public async Task<IReadOnlyList<InventoryPredictionDto>> GetAtRiskProductsAsync(
        string shopDomain,
        int withinDays = 14)
    {
        var predictions = await _db.InventoryPredictions
            .Where(p => p.ShopDomain == shopDomain)
            .Where(p => p.DaysUntilStockout <= withinDays)
            .OrderBy(p => p.DaysUntilStockout)
            .ToListAsync();

        return predictions.Select(MapToDto).ToList();
    }

    public async Task<ProductSalesVelocityDto?> GetSalesVelocityAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        var velocity = await CalculateSalesVelocityAsync(shopDomain, platformProductId, platformVariantId, cutoffDate);
        return velocity;
    }

    private async Task<InventoryPrediction?> CalculateAndSavePredictionAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId,
        string productTitle,
        string? variantTitle,
        string? sku,
        int currentQuantity,
        DateTime cutoffDate,
        InventoryAlertSettings settings)
    {
        // Calculate sales velocity
        var velocity = await CalculateSalesVelocityAsync(shopDomain, platformProductId, platformVariantId, cutoffDate);

        // Find existing prediction or create new
        var prediction = await _db.InventoryPredictions
            .FirstOrDefaultAsync(p =>
                p.ShopDomain == shopDomain &&
                p.PlatformProductId == platformProductId &&
                p.PlatformVariantId == platformVariantId);

        if (prediction == null)
        {
            prediction = new InventoryPrediction
            {
                ShopDomain = shopDomain,
                PlatformProductId = platformProductId,
                PlatformVariantId = platformVariantId,
                CreatedAt = DateTime.UtcNow
            };
            _db.InventoryPredictions.Add(prediction);
        }

        // Update prediction data
        prediction.ProductTitle = productTitle;
        prediction.VariantTitle = variantTitle;
        prediction.Sku = sku;
        prediction.CurrentQuantity = currentQuantity;

        if (velocity != null)
        {
            prediction.AverageDailySales = velocity.AverageDailySales;
            prediction.SalesDataPointsCount = velocity.OrderCount;
            prediction.OldestSaleDate = velocity.FirstSaleDate;
            prediction.NewestSaleDate = velocity.LastSaleDate;

            // Calculate days until stockout
            if (velocity.AverageDailySales > 0)
            {
                prediction.DaysUntilStockout = (int)Math.Floor(currentQuantity / velocity.AverageDailySales);
                prediction.ProjectedStockoutDate = DateTime.UtcNow.AddDays(prediction.DaysUntilStockout);
            }
            else
            {
                prediction.DaysUntilStockout = int.MaxValue;
                prediction.ProjectedStockoutDate = null;
            }

            // Calculate suggested reorder quantity
            var reorderDays = settings.DefaultLeadTimeDays + settings.DefaultSafetyStockDays;
            prediction.SuggestedReorderQuantity = (int)Math.Ceiling(velocity.AverageDailySales * reorderDays);
            prediction.SuggestedReorderDate = DateTime.UtcNow.AddDays(
                Math.Max(0, prediction.DaysUntilStockout - settings.DefaultLeadTimeDays));

            // Determine confidence level
            prediction.ConfidenceLevel = DetermineConfidenceLevel(velocity.DaysCovered, velocity.OrderCount);
        }
        else
        {
            // No sales data
            prediction.AverageDailySales = 0;
            prediction.DaysUntilStockout = currentQuantity > 0 ? int.MaxValue : 0;
            prediction.ProjectedStockoutDate = null;
            prediction.SuggestedReorderQuantity = 0;
            prediction.ConfidenceLevel = "low";
        }

        // Determine status based on settings thresholds
        prediction.Status = DetermineStatus(prediction, settings);
        prediction.CalculatedAt = DateTime.UtcNow;
        prediction.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return prediction;
    }

    private async Task<ProductSalesVelocityDto?> CalculateSalesVelocityAsync(
        string shopDomain,
        long platformProductId,
        long? platformVariantId,
        DateTime cutoffDate)
    {
        var query = _db.OrderLines
            .Include(ol => ol.Order)
            .Where(ol => ol.Order.ShopDomain == shopDomain)
            .Where(ol => ol.Order.OrderDate >= cutoffDate)
            .Where(ol => ol.Order.FinancialStatus != "cancelled" && ol.Order.FinancialStatus != "voided")
            .Where(ol => ol.PlatformProductId == platformProductId);

        if (platformVariantId.HasValue)
            query = query.Where(ol => ol.PlatformVariantId == platformVariantId);

        var salesData = await query
            .GroupBy(ol => new { ol.PlatformProductId, ol.PlatformVariantId })
            .Select(g => new
            {
                TotalQuantity = g.Sum(ol => ol.Quantity),
                OrderCount = g.Select(ol => ol.OrderId).Distinct().Count(),
                FirstSale = g.Min(ol => ol.Order.OrderDate),
                LastSale = g.Max(ol => ol.Order.OrderDate),
                ProductTitle = g.First().ProductTitle,
                VariantTitle = g.First().VariantTitle,
                Sku = g.First().Sku
            })
            .FirstOrDefaultAsync();

        if (salesData == null || salesData.TotalQuantity == 0)
            return null;

        var daysCovered = Math.Max(1, (DateTime.UtcNow - salesData.FirstSale).Days);
        var avgDailySales = (decimal)salesData.TotalQuantity / daysCovered;

        return new ProductSalesVelocityDto
        {
            PlatformProductId = platformProductId,
            PlatformVariantId = platformVariantId,
            ProductTitle = salesData.ProductTitle,
            VariantTitle = salesData.VariantTitle,
            Sku = salesData.Sku,
            TotalUnitsSold = salesData.TotalQuantity,
            OrderCount = salesData.OrderCount,
            AverageDailySales = Math.Round(avgDailySales, 4),
            FirstSaleDate = salesData.FirstSale,
            LastSaleDate = salesData.LastSale,
            DaysCovered = daysCovered
        };
    }

    private static string DetermineConfidenceLevel(int daysCovered, int orderCount)
    {
        if (daysCovered >= 60 && orderCount >= 30) return "high";
        if (daysCovered >= 30 && orderCount >= 10) return "medium";
        return "low";
    }

    private static string DetermineStatus(InventoryPrediction prediction, InventoryAlertSettings settings)
    {
        if (prediction.CurrentQuantity <= 0)
            return "out_of_stock";

        if (prediction.DaysUntilStockout <= settings.CriticalStockDaysThreshold)
            return "critical";

        if (prediction.DaysUntilStockout <= settings.LowStockDaysThreshold)
            return "low_stock";

        return "ok";
    }

    private async Task<InventoryAlertSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.InventoryAlertSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new InventoryAlertSettings
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.InventoryAlertSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    private static long? ParseVariantId(string? variantGid)
    {
        if (string.IsNullOrEmpty(variantGid)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(variantGid, @"(\d+)$");
        return match.Success && long.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }

    private static InventoryPredictionDto MapToDto(InventoryPrediction p) => new()
    {
        Id = p.Id,
        ShopDomain = p.ShopDomain,
        PlatformProductId = p.PlatformProductId,
        PlatformVariantId = p.PlatformVariantId,
        ProductTitle = p.ProductTitle,
        VariantTitle = p.VariantTitle,
        Sku = p.Sku,
        CurrentQuantity = p.CurrentQuantity,
        AverageDailySales = p.AverageDailySales,
        SevenDayAverageSales = p.SevenDayAverageSales,
        ThirtyDayAverageSales = p.ThirtyDayAverageSales,
        DaysUntilStockout = p.DaysUntilStockout == int.MaxValue ? 9999 : p.DaysUntilStockout,
        ProjectedStockoutDate = p.ProjectedStockoutDate,
        SuggestedReorderQuantity = p.SuggestedReorderQuantity,
        SuggestedReorderDate = p.SuggestedReorderDate,
        ConfidenceLevel = p.ConfidenceLevel,
        Status = p.Status,
        CalculatedAt = p.CalculatedAt
    };
}
