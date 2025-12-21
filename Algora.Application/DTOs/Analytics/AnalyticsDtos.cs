namespace Algora.Application.DTOs.Analytics;

// ============================================
// Time Period Specification
// ============================================

public record AnalyticsTimePeriod(
    string PeriodType,  // today, 7days, 30days, 90days, 12months, custom
    DateTime? StartDate,
    DateTime? EndDate
);

// ============================================
// Dashboard Summary DTOs
// ============================================

public record DashboardSummaryDto(
    decimal TotalRevenue,
    decimal RevenueChange,
    int TotalOrders,
    int OrdersChange,
    decimal AverageOrderValue,
    decimal AovChange,
    decimal GrossProfit,
    decimal GrossProfitMargin,
    decimal NetProfit,
    decimal NetProfitMargin,
    int NewCustomers,
    int ReturningCustomers,
    decimal ConversionRate,
    int TotalUnitsSold
);

public record SalesTrendDto(
    List<SalesTrendPointDto> DataPoints,
    decimal TotalRevenue,
    decimal TotalProfit,
    int TotalOrders
);

public record SalesTrendPointDto(
    DateTime Date,
    decimal Revenue,
    decimal Profit,
    int Orders,
    decimal COGS,
    decimal AdsSpend
);

public record CostBreakdownDto(
    decimal Revenue,
    decimal COGS,
    decimal AdsSpend,
    decimal Refunds,
    decimal GrossProfit,
    decimal NetProfit
);

// ============================================
// Product Performance DTOs
// ============================================

public record ProductHeatmapDto(
    List<ProductPerformanceDto> Products,
    string SortBy,
    int TotalProducts
);

public record ProductPerformanceDto(
    int ProductId,
    long PlatformProductId,
    string ProductTitle,
    string? Sku,
    string? ImageUrl,
    int QuantitySold,
    decimal Revenue,
    decimal COGS,
    decimal Profit,
    decimal ProfitMargin,
    string PerformanceLevel,  // excellent, good, average, poor
    decimal RevenueShare
);

public record TopProductDto(
    int ProductId,
    string ProductTitle,
    string? Sku,
    string? ImageUrl,
    int QuantitySold,
    decimal Revenue,
    decimal Profit
);

// ============================================
// Customer Lifetime Value DTOs
// ============================================

public record ClvReportDto(
    ClvSummaryDto Summary,
    List<CustomerLifetimeValueDto> TopCustomers,
    List<CustomerLifetimeValueDto> AtRiskCustomers,
    List<ClvSegmentDto> Segments
);

public record ClvSummaryDto(
    decimal AverageClv,
    decimal TotalClv,
    int TotalCustomers,
    decimal HighValuePercentage,
    decimal ChurnRate
);

public record CustomerLifetimeValueDto(
    int CustomerId,
    string CustomerName,
    string Email,
    int TotalOrders,
    decimal TotalSpent,
    decimal AverageOrderValue,
    decimal PredictedLifetimeValue,
    string Segment,
    int DaysSinceLastOrder,
    DateTime FirstOrderDate,
    DateTime LastOrderDate,
    decimal? TotalProfit
);

public record ClvSegmentDto(
    string Segment,
    int CustomerCount,
    decimal TotalValue,
    decimal AverageValue,
    decimal PercentageOfTotal
);

// ============================================
// Cohort Analysis DTOs
// ============================================

public record CohortAnalysisDto(
    List<CohortDto> Cohorts,
    int MaxPeriods,
    string PeriodType  // monthly, weekly
);

public record CohortDto(
    DateTime CohortDate,
    string CohortLabel,
    int InitialCustomers,
    List<CohortPeriodDto> Periods
);

public record CohortPeriodDto(
    int PeriodNumber,
    int ActiveCustomers,
    decimal RetentionRate,
    decimal Revenue
);

// ============================================
// Profit Margin DTOs
// ============================================

public record ProfitMarginDto(
    decimal TotalRevenue,
    decimal TotalCOGS,
    decimal TotalAdsSpend,
    decimal TotalRefunds,
    decimal GrossProfit,
    decimal GrossProfitMargin,
    decimal NetProfit,
    decimal NetProfitMargin,
    List<ProfitByPeriodDto> TrendData,
    ProfitBreakdownDto Breakdown
);

public record ProfitByPeriodDto(
    DateTime Date,
    decimal Revenue,
    decimal GrossProfit,
    decimal NetProfit,
    decimal GrossMargin,
    decimal NetMargin
);

public record ProfitBreakdownDto(
    decimal COGSPercentage,
    decimal AdsSpendPercentage,
    decimal RefundsPercentage,
    decimal ProfitPercentage
);

// ============================================
// Ads Spend DTOs
// ============================================

public record AdsSpendDto(
    int Id,
    string Platform,
    string? CampaignName,
    string? CampaignId,
    DateTime SpendDate,
    decimal Amount,
    string Currency,
    int? Impressions,
    int? Clicks,
    int? Conversions,
    decimal? Revenue,
    string? Notes,
    decimal? ROAS,
    decimal? CPC,
    decimal? CPM
);

public record AdsSpendCreateDto(
    string Platform,
    string? CampaignName,
    string? CampaignId,
    DateTime SpendDate,
    decimal Amount,
    string Currency,
    int? Impressions,
    int? Clicks,
    int? Conversions,
    decimal? Revenue,
    string? Notes
);

public record AdsSpendSummaryDto(
    decimal TotalSpend,
    decimal TotalRevenue,
    decimal OverallROAS,
    int TotalConversions,
    List<AdsPlatformSummaryDto> ByPlatform
);

public record AdsPlatformSummaryDto(
    string Platform,
    decimal Spend,
    decimal Revenue,
    decimal ROAS,
    int Conversions,
    decimal SpendPercentage
);

// ============================================
// Analytics Snapshot DTO
// ============================================

public record AnalyticsSnapshotDto(
    int Id,
    DateTime SnapshotDate,
    string PeriodType,
    int TotalOrders,
    decimal TotalRevenue,
    decimal TotalCOGS,
    decimal TotalAdsSpend,
    decimal GrossProfit,
    decimal NetProfit,
    decimal TotalRefunds,
    int NewCustomers,
    int ReturningCustomers,
    int TotalUnitsSold,
    decimal AverageOrderValue,
    decimal? ConversionRate
);

// ============================================
// Traffic & Conversion DTOs
// ============================================

public record TrafficOverviewDto(
    int TotalSessions,
    int TotalPageViews,
    decimal BounceRate,
    decimal AverageSessionDuration,
    decimal ConversionRate,
    List<TrafficSourceDto> Sources
);

public record TrafficSourceDto(
    string Source,
    int Sessions,
    decimal Percentage,
    decimal ConversionRate,
    decimal Revenue
);
