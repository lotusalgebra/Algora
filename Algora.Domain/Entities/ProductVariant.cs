namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a product variant (size, color, etc.) synced from the platform.
    /// </summary>
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public long PlatformVariantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int InventoryQuantity { get; set; }
        public string? Option1 { get; set; }
        public string? Option2 { get; set; }
        public string? Option3 { get; set; }
        public decimal Weight { get; set; }
        public string? WeightUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Cost of goods sold per unit (for profit margin calculations).
        /// </summary>
        public decimal? CostOfGoodsSold { get; set; }
    }
}