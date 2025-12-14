namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a line item in an order.
    /// </summary>
    public class OrderLine
    {
        /// <summary>
        /// Primary key for the order line.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the parent order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Navigation property to the parent order.
        /// </summary>
        public Order Order { get; set; } = null!;

        /// <summary>
        /// Platform line item identifier.
        /// </summary>
        public long PlatformLineItemId { get; set; }

        /// <summary>
        /// Platform product identifier.
        /// </summary>
        public long? PlatformProductId { get; set; }

        /// <summary>
        /// Platform variant identifier.
        /// </summary>
        public long? PlatformVariantId { get; set; }

        /// <summary>
        /// Product title at time of order.
        /// </summary>
        public string ProductTitle { get; set; } = string.Empty;

        /// <summary>
        /// Variant title (e.g., "Small / Red").
        /// </summary>
        public string? VariantTitle { get; set; }

        /// <summary>
        /// SKU at time of order.
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Quantity ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price at time of order.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount applied to this line item.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Tax amount for this line item.
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Total for this line (quantity * unit price - discount + tax).
        /// </summary>
        public decimal LineTotal { get; set; }
    }
}