namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a reusable tag entity.
    /// </summary>
    public class Tag
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty; // customer, order, product
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}