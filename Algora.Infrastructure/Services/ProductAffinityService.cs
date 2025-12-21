using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for calculating product affinities using market basket analysis.
/// Analyzes order history to find products frequently purchased together.
/// </summary>
public class ProductAffinityService : IProductAffinityService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductAffinityService> _logger;

    public ProductAffinityService(
        AppDbContext db,
        ILogger<ProductAffinityService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> CalculateAffinitiesAsync(string shopDomain, int lookbackDays = 90)
    {
        _logger.LogInformation("Calculating product affinities for {Shop} with {Days} day lookback", shopDomain, lookbackDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        // Get all orders with their products
        var orderProducts = await _db.OrderLines
            .Include(ol => ol.Order)
            .Where(ol => ol.Order.ShopDomain == shopDomain)
            .Where(ol => ol.Order.OrderDate >= cutoffDate)
            .Where(ol => ol.Order.FinancialStatus != "cancelled" && ol.Order.FinancialStatus != "voided")
            .Where(ol => ol.PlatformProductId.HasValue)
            .GroupBy(ol => ol.OrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                Products = g.Select(ol => new
                {
                    ProductId = ol.PlatformProductId!.Value,
                    Title = ol.ProductTitle
                }).Distinct().ToList()
            })
            .ToListAsync();

        var totalOrders = orderProducts.Count;
        if (totalOrders < 10)
        {
            _logger.LogWarning("Not enough orders ({Count}) to calculate affinities for {Shop}", totalOrders, shopDomain);
            return 0;
        }

        // Count occurrences and co-occurrences
        var productCounts = new Dictionary<long, int>();
        var productTitles = new Dictionary<long, string>();
        var coOccurrences = new Dictionary<(long, long), int>();

        foreach (var order in orderProducts)
        {
            var products = order.Products.OrderBy(p => p.ProductId).ToList();

            foreach (var product in products)
            {
                productCounts[product.ProductId] = productCounts.GetValueOrDefault(product.ProductId) + 1;
                productTitles[product.ProductId] = product.Title;
            }

            // Generate pairs (ordered to avoid duplicates)
            for (int i = 0; i < products.Count; i++)
            {
                for (int j = i + 1; j < products.Count; j++)
                {
                    var pair = (products[i].ProductId, products[j].ProductId);
                    coOccurrences[pair] = coOccurrences.GetValueOrDefault(pair) + 1;
                }
            }
        }

        // Calculate and store affinities
        var affinities = new List<ProductAffinity>();

        foreach (var (pair, coCount) in coOccurrences)
        {
            if (coCount < settings.MinimumCoOccurrences) continue;

            var countA = productCounts[pair.Item1];
            var countB = productCounts[pair.Item2];

            var support = (decimal)coCount / totalOrders;
            var confidenceAB = (decimal)coCount / countA;
            var confidenceBA = (decimal)coCount / countB;
            var supportB = (decimal)countB / totalOrders;
            var liftAB = supportB > 0 ? confidenceAB / supportB : 0;

            // Use the higher confidence score
            var maxConfidence = Math.Max(confidenceAB, confidenceBA);

            if (maxConfidence < settings.MinimumConfidenceScore)
                continue;

            affinities.Add(new ProductAffinity
            {
                ShopDomain = shopDomain,
                PlatformProductIdA = pair.Item1,
                ProductTitleA = productTitles.GetValueOrDefault(pair.Item1, "Unknown"),
                PlatformProductIdB = pair.Item2,
                ProductTitleB = productTitles.GetValueOrDefault(pair.Item2, "Unknown"),
                CoOccurrenceCount = coCount,
                ProductAOrderCount = countA,
                ProductBOrderCount = countB,
                SupportScore = Math.Round(support, 4),
                ConfidenceScore = Math.Round(maxConfidence, 4),
                LiftScore = Math.Round(liftAB, 4),
                TotalOrdersAnalyzed = totalOrders,
                AnalysisStartDate = cutoffDate,
                AnalysisEndDate = DateTime.UtcNow,
                CalculatedAt = DateTime.UtcNow
            });
        }

        // Clear old affinities and insert new ones
        await ClearAffinitiesAsync(shopDomain);

        if (affinities.Count > 0)
        {
            _db.ProductAffinities.AddRange(affinities);
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Calculated {Count} product affinities for {Shop}", affinities.Count, shopDomain);
        return affinities.Count;
    }

    public async Task<List<ProductAffinityDto>> GetAffinitiesForProductAsync(
        string shopDomain,
        long productId,
        int limit = 10)
    {
        var affinities = await _db.ProductAffinities
            .Where(pa => pa.ShopDomain == shopDomain)
            .Where(pa => pa.PlatformProductIdA == productId || pa.PlatformProductIdB == productId)
            .OrderByDescending(pa => pa.ConfidenceScore)
            .ThenByDescending(pa => pa.LiftScore)
            .Take(limit)
            .ToListAsync();

        return affinities.Select(pa => MapToDto(pa, productId)).ToList();
    }

    public async Task<PaginatedResult<ProductAffinityDto>> GetAllAffinitiesAsync(
        string shopDomain,
        decimal? minConfidence = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.ProductAffinities
            .Where(pa => pa.ShopDomain == shopDomain)
            .AsQueryable();

        if (minConfidence.HasValue)
            query = query.Where(pa => pa.ConfidenceScore >= minConfidence.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(pa => pa.ConfidenceScore)
            .ThenByDescending(pa => pa.CoOccurrenceCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<ProductAffinityDto>
        {
            Items = items.Select(pa => MapToDto(pa)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AffinitySummaryDto> GetAffinitySummaryAsync(string shopDomain)
    {
        var affinities = await _db.ProductAffinities
            .Where(pa => pa.ShopDomain == shopDomain)
            .ToListAsync();

        var strongAffinities = affinities.Count(pa => pa.LiftScore >= 1.5m);
        var avgConfidence = affinities.Count > 0 ? affinities.Average(pa => pa.ConfidenceScore) : 0;
        var lastCalculated = affinities.FirstOrDefault()?.CalculatedAt;

        return new AffinitySummaryDto
        {
            TotalAffinities = affinities.Count,
            StrongAffinities = strongAffinities,
            AverageConfidence = avgConfidence,
            LastCalculated = lastCalculated
        };
    }

    public async Task<int> ClearAffinitiesAsync(string shopDomain)
    {
        var count = await _db.ProductAffinities
            .Where(pa => pa.ShopDomain == shopDomain)
            .ExecuteDeleteAsync();

        return count;
    }

    private async Task<UpsellSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.UpsellSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new UpsellSettings
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.UpsellSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    private static ProductAffinityDto MapToDto(ProductAffinity pa, long? perspectiveProductId = null)
    {
        // If viewing from a specific product's perspective, show the other product
        var (sourceId, sourceTitle, relatedId, relatedTitle) = perspectiveProductId.HasValue && perspectiveProductId == pa.PlatformProductIdB
            ? (pa.PlatformProductIdB, pa.ProductTitleB, pa.PlatformProductIdA, pa.ProductTitleA)
            : (pa.PlatformProductIdA, pa.ProductTitleA, pa.PlatformProductIdB, pa.ProductTitleB);

        return new ProductAffinityDto
        {
            Id = pa.Id,
            SourceProductId = sourceId,
            SourceProductTitle = sourceTitle,
            RelatedProductId = relatedId,
            RelatedProductTitle = relatedTitle,
            CoOccurrences = pa.CoOccurrenceCount,
            Support = pa.SupportScore,
            Confidence = pa.ConfidenceScore,
            Lift = pa.LiftScore,
            CalculatedAt = pa.CalculatedAt
        };
    }
}
