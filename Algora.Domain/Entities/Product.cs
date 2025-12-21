namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a product record synced from the e-commerce platform.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Primary key for the product record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Platform product identifier (Shopify product id).
        /// </summary>
        public long PlatformProductId { get; set; }

        /// <summary>
        /// The shop domain this product belongs to.
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// Product title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Product description (HTML or plain text).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Product vendor or brand.
        /// </summary>
        public string? Vendor { get; set; }

        /// <summary>
        /// Product type/category.
        /// </summary>
        public string? ProductType { get; set; }

        /// <summary>
        /// Comma-separated tags.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// SKU (Stock Keeping Unit) for inventory tracking.
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Current price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Compare-at price (original price before discount).
        /// </summary>
        public decimal? CompareAtPrice { get; set; }

        /// <summary>
        /// Current inventory quantity.
        /// </summary>
        public int InventoryQuantity { get; set; }

        /// <summary>
        /// Whether the product is active and visible.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// UTC timestamp when the product was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the product was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Cost of goods sold per unit (for profit margin calculations).
        /// </summary>
        public decimal? CostOfGoodsSold { get; set; }
    }
}