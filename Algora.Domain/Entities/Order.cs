namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an order record synced from the e-commerce platform.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Primary key for the order record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Platform order identifier (Shopify order id).
        /// </summary>
        public long PlatformOrderId { get; set; }

        /// <summary>
        /// Human-readable order number.
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// The shop domain this order belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the customer who placed the order.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Navigation property to the customer.
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// Customer email (denormalized for quick access).
        /// </summary>
        public string? CustomerEmail { get; set; }

        /// <summary>
        /// Order subtotal before tax and shipping.
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Total tax amount.
        /// </summary>
        public decimal TaxTotal { get; set; }

        /// <summary>
        /// Shipping cost.
        /// </summary>
        public decimal ShippingTotal { get; set; }

        /// <summary>
        /// Discount amount applied.
        /// </summary>
        public decimal DiscountTotal { get; set; }

        /// <summary>
        /// Grand total for the order.
        /// </summary>
        public decimal GrandTotal { get; set; }

        /// <summary>
        /// Currency code (e.g., USD, EUR).
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Order financial status (paid, pending, refunded, etc.).
        /// </summary>
        public string FinancialStatus { get; set; } = string.Empty;

        /// <summary>
        /// Order fulfillment status (fulfilled, unfulfilled, partial, etc.).
        /// </summary>
        public string FulfillmentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Billing address (JSON or formatted string).
        /// </summary>
        public string? BillingAddress { get; set; }

        /// <summary>
        /// Shipping address (JSON or formatted string).
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Notes or comments on the order.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// UTC timestamp when the order was placed.
        /// </summary>
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the order was created in local DB.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the order was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Navigation property for order line items.
        /// </summary>
        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();

        /// <summary>
        /// Navigation property for invoices generated for this order.
        /// </summary>
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}