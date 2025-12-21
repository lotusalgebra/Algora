namespace Algora.Domain.Entities;

/// <summary>
/// Stores calculated inventory predictions for a product/variant.
/// </summary>
public class InventoryPrediction
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Product reference
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Platform IDs for direct Shopify reference
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }

    // Product info (denormalized for quick access)
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }

    // Current inventory state
    public int CurrentQuantity { get; set; }

    // Sales velocity data
    public decimal AverageDailySales { get; set; }
    public decimal? SevenDayAverageSales { get; set; }
    public decimal? ThirtyDayAverageSales { get; set; }
    public decimal? NinetyDayAverageSales { get; set; }

    // Stockout projection
    public int DaysUntilStockout { get; set; }
    public DateTime? ProjectedStockoutDate { get; set; }

    // Reorder suggestion
    public int SuggestedReorderQuantity { get; set; }
    public DateTime? SuggestedReorderDate { get; set; }

    // Confidence and metadata
    public string ConfidenceLevel { get; set; } = "medium"; // low, medium, high
    public int SalesDataPointsCount { get; set; }
    public DateTime? OldestSaleDate { get; set; }
    public DateTime? NewestSaleDate { get; set; }

    // Status
    public string Status { get; set; } = "ok"; // ok, low_stock, critical, out_of_stock

    // Timestamps
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
