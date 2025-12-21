namespace Algora.Domain.Entities
{
    /// <summary>
    /// Pre-computed daily/weekly/monthly analytics metrics for faster dashboard loading.
    /// </summary>
    public class AnalyticsSnapshot
    {
        public int Id { get; set; }

        /// <summary>
        /// The shop domain this snapshot belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Date of the snapshot.
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// Period type: daily, weekly, monthly.
        /// </summary>
        public string PeriodType { get; set; } = "daily";

        /// <summary>
        /// Total number of orders in this period.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Total revenue in this period.
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total cost of goods sold.
        /// </summary>
        public decimal TotalCOGS { get; set; }

        /// <summary>
        /// Total advertising spend.
        /// </summary>
        public decimal TotalAdsSpend { get; set; }

        /// <summary>
        /// Gross profit (Revenue - COGS).
        /// </summary>
        public decimal GrossProfit { get; set; }

        /// <summary>
        /// Net profit (Gross Profit - Ads Spend - Refunds).
        /// </summary>
        public decimal NetProfit { get; set; }

        /// <summary>
        /// Total refund amount.
        /// </summary>
        public decimal TotalRefunds { get; set; }

        /// <summary>
        /// Number of new customers acquired.
        /// </summary>
        public int NewCustomers { get; set; }

        /// <summary>
        /// Number of returning customers.
        /// </summary>
        public int ReturningCustomers { get; set; }

        /// <summary>
        /// Total units sold.
        /// </summary>
        public int TotalUnitsSold { get; set; }

        /// <summary>
        /// Average order value.
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Conversion rate (orders / sessions if available).
        /// </summary>
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// JSON string containing top products data for flexibility.
        /// </summary>
        public string? TopProductsJson { get; set; }

        /// <summary>
        /// JSON string containing traffic sources data.
        /// </summary>
        public string? TrafficSourcesJson { get; set; }

        /// <summary>
        /// When the snapshot was generated.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
