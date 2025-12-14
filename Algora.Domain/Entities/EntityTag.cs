namespace Algora.Domain.Entities
{
    /// <summary>
    /// Many-to-many relationship between entities and tags.
    /// </summary>
    public class EntityTag
    {
        public int Id { get; set; }
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
        public string EntityType { get; set; } = string.Empty; // customer, order, product
        public int EntityId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}