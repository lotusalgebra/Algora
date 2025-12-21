namespace Algora.Application.DTOs.Inventory;

public record InventoryPredictionDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public long PlatformProductId { get; init; }
    public long? PlatformVariantId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public string? Sku { get; init; }
    public int CurrentQuantity { get; init; }
    public decimal AverageDailySales { get; init; }
    public decimal? SevenDayAverageSales { get; init; }
    public decimal? ThirtyDayAverageSales { get; init; }
    public int DaysUntilStockout { get; init; }
    public DateTime? ProjectedStockoutDate { get; init; }
    public int SuggestedReorderQuantity { get; init; }
    public DateTime? SuggestedReorderDate { get; init; }
    public string ConfidenceLevel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CalculatedAt { get; init; }
}

public record InventoryPredictionSummaryDto
{
    public int TotalProducts { get; init; }
    public int OutOfStockCount { get; init; }
    public int CriticalStockCount { get; init; }
    public int LowStockCount { get; init; }
    public int HealthyStockCount { get; init; }
    public List<InventoryPredictionDto> TopAtRisk { get; init; } = new();
}

public record ProductSalesVelocityDto
{
    public long PlatformProductId { get; init; }
    public long? PlatformVariantId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public string? Sku { get; init; }
    public int TotalUnitsSold { get; init; }
    public int OrderCount { get; init; }
    public decimal AverageDailySales { get; init; }
    public DateTime? FirstSaleDate { get; init; }
    public DateTime? LastSaleDate { get; init; }
    public int DaysCovered { get; init; }
}

public record PaginatedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
