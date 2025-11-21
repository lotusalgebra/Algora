using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Core.Models
{
    /// <summary>
    /// View model representing an order for use in the UI.
    /// Contains customer and address information, order totals, status and the line items.
    /// </summary>
    public class OrderViewModel
    {
        /// <summary>
        /// Numeric identifier for the order (internal id or Shopify order number).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Full name of the customer who placed the order.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Customer email address associated with the order.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Total amount for the order in the store currency.
        /// Represents the order grand total (including taxes/shipping if applicable).
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Current order status (for example: "Pending", "Completed", "Cancelled").
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Date and time when the order was created. Prefer UTC for storage and conversions.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Shipping address formatted for display (single-line or multi-line as needed).
        /// </summary>
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Billing address formatted for display.
        /// </summary>
        public string BillingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Collection of line items included in the order.
        /// Each item represents a product/variant, quantity and unit price.
        /// </summary>
        public List<OrderItemViewModel> Items { get; set; } = new();
    }
}
