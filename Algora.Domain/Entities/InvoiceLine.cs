namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a line item on an invoice.
    /// </summary>
    public class InvoiceLine
    {
        /// <summary>
        /// Primary key for the invoice line.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the parent invoice.
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Navigation property to the parent invoice.
        /// </summary>
        public Invoice Invoice { get; set; } = null!;

        /// <summary>
        /// Product or service description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// SKU or item code.
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount for this line.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Tax for this line.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Line total (quantity * unit price - discount + tax).
        /// </summary>
        public decimal LineTotal { get; set; }
    }
}