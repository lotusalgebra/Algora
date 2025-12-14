namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a product collection/category synced from the platform.
    /// </summary>
    public class Collection
    {
        public int Id { get; set; }
        public long PlatformCollectionId { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Handle { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}