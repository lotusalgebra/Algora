using Algora.Application.DTOs.Analytics;

namespace Algora.Application.Interfaces;

public interface IAnalyticsService
{
    // ==================== Dashboard ====================
    
    /// <summary>
    /// Get dashboard summary metrics for a shop.
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(string shopDomain, AnalyticsTimePeriod period);
    
    /// <summary>
    /// Get sales trend data for charting.
    /// </summary>
    Task<SalesTrendDto> GetSalesTrendAsync(string shopDomain, AnalyticsTimePeriod period);
    
    /// <summary>
    /// Get cost breakdown for pie chart.
    /// </summary>
    Task<CostBreakdownDto> GetCostBreakdownAsync(string shopDomain, AnalyticsTimePeriod period);

    // ==================== Products ====================
    
    /// <summary>
    /// Get product performance heatmap data.
    /// </summary>
    Task<ProductHeatmapDto> GetProductHeatmapAsync(string shopDomain, AnalyticsTimePeriod period, int limit = 50);
    
    /// <summary>
    /// Get top performing products.
    /// </summary>
    Task<List<TopProductDto>> GetTopProductsAsync(string shopDomain, AnalyticsTimePeriod period, string sortBy = "revenue", int limit = 10);

    // ==================== Customer Lifetime Value ====================
    
    /// <summary>
    /// Get CLV report with segments and top/at-risk customers.
    /// </summary>
    Task<ClvReportDto> GetClvReportAsync(string shopDomain);
    
    /// <summary>
    /// Recalculate CLV for all customers in a shop.
    /// </summary>
    Task<int> RecalculateClvAsync(string shopDomain);
    
    /// <summary>
    /// Get CLV data for a specific customer.
    /// </summary>
    Task<CustomerLifetimeValueDto?> GetCustomerClvAsync(string shopDomain, int customerId);

    // ==================== Cohorts ====================
    
    /// <summary>
    /// Get cohort retention analysis.
    /// </summary>
    Task<CohortAnalysisDto> GetCohortAnalysisAsync(string shopDomain, int monthsBack = 12);

    // ==================== Profit ====================
    
    /// <summary>
    /// Get profit margin breakdown.
    /// </summary>
    Task<ProfitMarginDto> GetProfitMarginAsync(string shopDomain, AnalyticsTimePeriod period);

    // ==================== Ads Spend ====================
    
    /// <summary>
    /// Get all ads spend entries for a period.
    /// </summary>
    Task<List<AdsSpendDto>> GetAdsSpendAsync(string shopDomain, AnalyticsTimePeriod period);
    
    /// <summary>
    /// Get ads spend summary with ROAS and breakdown by platform.
    /// </summary>
    Task<AdsSpendSummaryDto> GetAdsSpendSummaryAsync(string shopDomain, AnalyticsTimePeriod period);
    
    /// <summary>
    /// Create or update an ads spend entry.
    /// </summary>
    Task<AdsSpendDto> SaveAdsSpendAsync(string shopDomain, AdsSpendCreateDto dto, int? id = null);
    
    /// <summary>
    /// Delete an ads spend entry.
    /// </summary>
    Task<bool> DeleteAdsSpendAsync(string shopDomain, int id);

    // ==================== Snapshots ====================
    
    /// <summary>
    /// Generate a snapshot for a specific date.
    /// </summary>
    Task GenerateSnapshotAsync(string shopDomain, DateTime date);
    
    /// <summary>
    /// Get pre-computed snapshots for a period.
    /// </summary>
    Task<List<AnalyticsSnapshotDto>> GetSnapshotsAsync(string shopDomain, AnalyticsTimePeriod period);
}
