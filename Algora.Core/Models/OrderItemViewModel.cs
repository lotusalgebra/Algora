using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Core.Models
{
    /// <summary>
    /// Represents a single line item within an order used by the UI.
    /// Contains the display name, quantity and unit price for the item.
    /// </summary>
    public class OrderItemViewModel
    {
        /// <summary>
        /// Product or variant name to display for this line item.
        /// May include SKU or option information when helpful for the merchant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Quantity ordered of this item. Must be a non-negative integer.
        /// </summary>
        public int Qty { get; set; }

        /// <summary>
        /// Unit price for this item in the store currency.
        /// This value represents the price per single unit (not line total).
        /// </summary>
        public decimal Price { get; set; }
    }
}
