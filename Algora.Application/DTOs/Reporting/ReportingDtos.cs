namespace Algora.Application.DTOs.Reporting;

// ============= Common Report Request/Response DTOs =============

public record DateRangeRequest(
    DateTime StartDate,
    DateTime EndDate
);

public record TimeSeriesDataPoint(
    DateTime Date,
    string Label,
    decimal Value,
    decimal? SecondaryValue = null
);

// ============= Sales Report DTOs =============

public record SalesReportDto(
    decimal TotalRevenue,
    decimal TotalSubtotal,
    decimal TotalTax,
    decimal TotalShipping,
    decimal TotalDiscounts,
    int TotalOrders,
    decimal AverageOrderValue,
    decimal RevenueGrowthPercent,
    int OrderGrowthPercent,
    Dictionary<string, decimal> RevenueByDay,
    Dictionary<string, int> OrdersByStatus,
    List<TimeSeriesDataPoint> DailyRevenue,
    List<TimeSeriesDataPoint> DailyOrders
);

public record SalesByPeriodDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal Revenue,
    int Orders,
    decimal AverageOrderValue,
    decimal GrowthPercent
);

// ============= Product Report DTOs =============

public record ProductPerformanceDto(
    long ProductId,
    string ProductTitle,
    string? Vendor,
    string? ProductType,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal AverageSellingPrice,
    int InventoryQuantity,
    decimal? CostOfGoodsSold,
    decimal? GrossProfit,
    decimal? ProfitMargin
);

public record ProductReportSummaryDto(
    int TotalProducts,
    int TotalProductsSold,
    decimal TotalProductRevenue,
    int TotalUnitsSold,
    decimal AveragePricePoint,
    List<ProductPerformanceDto> BestSellers,
    List<ProductPerformanceDto> LowStock,
    List<ProductPerformanceDto> NoSales,
    Dictionary<string, decimal> RevenueByVendor,
    Dictionary<string, decimal> RevenueByProductType
);

// ============= Customer Report DTOs =============

public record CustomerReportDto(
    int TotalCustomers,
    int NewCustomers,
    int ReturningCustomers,
    decimal AverageCustomerLifetimeValue,
    Dictionary<string, int> CustomersBySegment,
    List<TimeSeriesDataPoint> CustomerAcquisitionTrend,
    List<CustomerSegmentDto> SegmentDetails
);

public record CustomerSegmentDto(
    string Segment,
    int CustomerCount,
    decimal TotalSpent,
    decimal AverageSpent,
    decimal AveragePredictedLTV,
    int AverageOrderCount
);

public record TopCustomerDto(
    long CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalSpent,
    int TotalOrders,
    decimal AverageOrderValue,
    string Segment,
    DateTime FirstOrderDate,
    DateTime LastOrderDate
);

// ============= Financial Report DTOs =============

public record FinancialReportDto(
    decimal GrossRevenue,
    decimal TotalRefunds,
    decimal NetRevenue,
    decimal TotalCOGS,
    decimal GrossProfit,
    decimal GrossProfitMargin,
    decimal TotalAdsSpend,
    decimal NetProfit,
    decimal NetProfitMargin,
    List<TimeSeriesDataPoint> RevenueVsRefunds,
    List<TimeSeriesDataPoint> ProfitTrend,
    Dictionary<string, decimal> RefundsByReason,
    List<RefundSummaryDto> RecentRefunds
);

public record RefundSummaryDto(
    int RefundId,
    int OrderId,
    string OrderNumber,
    decimal Amount,
    string? Reason,
    DateTime RefundedAt
);

// ============= Advertising Report DTOs =============

public record AdvertisingReportDto(
    decimal TotalSpend,
    decimal TotalRevenue,
    decimal ROAS,
    int TotalImpressions,
    int TotalClicks,
    int TotalConversions,
    decimal CTR,
    decimal ConversionRate,
    decimal CostPerClick,
    decimal CostPerConversion,
    Dictionary<string, AdPlatformMetricsDto> MetricsByPlatform,
    List<CampaignPerformanceDto> TopCampaigns,
    List<TimeSeriesDataPoint> SpendVsRevenueTrend
);

public record AdPlatformMetricsDto(
    string Platform,
    decimal Spend,
    decimal Revenue,
    decimal ROAS,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal CTR,
    decimal ConversionRate
);

public record CampaignPerformanceDto(
    string CampaignName,
    string Platform,
    decimal Spend,
    decimal Revenue,
    decimal ROAS,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal CTR,
    decimal ConversionRate,
    decimal CPC,
    decimal CPA
);

// ============= Dashboard Overview DTO =============

public record ReportingDashboardDto(
    // Summary Cards
    decimal TotalRevenue,
    decimal RevenueGrowth,
    int TotalOrders,
    int OrderGrowth,
    int TotalCustomers,
    int CustomerGrowth,
    decimal TotalAdsSpend,
    decimal AdsSpendGrowth,

    // Quick metrics
    decimal AverageOrderValue,
    decimal ROAS,
    decimal NetProfitMargin,

    // Mini charts data
    List<TimeSeriesDataPoint> RevenueSparkline,
    List<TimeSeriesDataPoint> OrdersSparkline,

    // Top performers
    List<ProductPerformanceDto> TopProducts,
    List<TopCustomerDto> TopCustomers
);
