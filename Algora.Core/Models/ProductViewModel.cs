using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Core.Models
{
    /// <summary>
    /// View model that represents a product for UI screens.
    /// Contains identifying information, merchandising fields and inventory/price data.
    /// </summary>
    public class ProductViewModel
    {
        /// <summary>
        /// Numeric product identifier (store or platform id).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Product title or name shown to customers.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Short product description or excerpt suitable for listing pages.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated tags associated with the product (used for filtering/search).
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Vendor or brand name for the product.
        /// </summary>
        public string Vendor { get; set; } = string.Empty;

        /// <summary>
        /// Current available stock quantity for the product.
        /// Use non-negative integers; implementations may cap or format this value for display.
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Display price for the product in store currency (unit price).
        /// </summary>
        public decimal Price { get; set; }
    }
}
