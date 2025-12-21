namespace Algora.Domain.Entities
{
    /// <summary>
    /// Stores calculated Customer Lifetime Value (CLV) data per customer.
    /// </summary>
    public class CustomerLifetimeValue
    {
        public int Id { get; set; }

        /// <summary>
        /// The shop domain this CLV calculation belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the Customer.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Navigation property to the Customer.
        /// </summary>
        public Customer Customer { get; set; } = null!;

        /// <summary>
        /// Total number of orders placed by this customer.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Total amount spent by this customer.
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>
        /// Average order value.
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// First order date.
        /// </summary>
        public DateTime FirstOrderDate { get; set; }

        /// <summary>
        /// Most recent order date.
        /// </summary>
        public DateTime LastOrderDate { get; set; }

        /// <summary>
        /// Days since the last order.
        /// </summary>
        public int DaysSinceLastOrder { get; set; }

        /// <summary>
        /// Average days between orders.
        /// </summary>
        public decimal? AverageDaysBetweenOrders { get; set; }

        /// <summary>
        /// Predicted lifetime value based on purchase patterns.
        /// </summary>
        public decimal PredictedLifetimeValue { get; set; }

        /// <summary>
        /// Customer segment: high_value, medium_value, low_value, at_risk, churned.
        /// </summary>
        public string Segment { get; set; } = "low_value";

        /// <summary>
        /// Customer acquisition source if known.
        /// </summary>
        public string? AcquisitionSource { get; set; }

        /// <summary>
        /// Total profit from this customer (revenue - COGS).
        /// </summary>
        public decimal? TotalProfit { get; set; }

        /// <summary>
        /// When the CLV was last calculated.
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
