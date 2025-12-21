namespace Algora.Domain.Entities
{
    /// <summary>
    /// Many-to-many relationship between products and collections.
    /// </summary>
    public class ProductCollection
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int CollectionId { get; set; }
        public Collection Collection { get; set; } = null!;
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}