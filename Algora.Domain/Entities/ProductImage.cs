namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an image associated with a product.
    /// </summary>
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public long? PlatformImageId { get; set; }
        public string Src { get; set; } = string.Empty;
        public string? Alt { get; set; }
        public int Position { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}