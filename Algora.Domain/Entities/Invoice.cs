namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an invoice generated for an order.
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// Primary key for the invoice.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique invoice number (e.g., "INV-2025-0001").
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// The shop domain this invoice belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the related order.
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Navigation property to the related order.
        /// </summary>
        public Order? Order { get; set; }

        /// <summary>
        /// Foreign key to the customer.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Navigation property to the customer.
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// Customer name (denormalized).
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Customer email (denormalized).
        /// </summary>
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Billing address.
        /// </summary>
        public string? BillingAddress { get; set; }

        /// <summary>
        /// Shipping address.
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Invoice date.
        /// </summary>
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Due date for payment.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Subtotal before tax.
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Tax amount.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Shipping amount.
        /// </summary>
        public decimal Shipping { get; set; }

        /// <summary>
        /// Discount amount.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Grand total.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Invoice status (draft, sent, paid, cancelled).
        /// </summary>
        public string Status { get; set; } = "draft";

        /// <summary>
        /// Notes or terms.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// UTC timestamp when the invoice was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the invoice was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Navigation property for invoice line items.
        /// </summary>
        public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    }
}