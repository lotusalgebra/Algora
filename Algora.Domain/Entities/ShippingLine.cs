namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a shipping method/line for an order.
    /// </summary>
    public class ShippingLine
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Source { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public string? CarrierIdentifier { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}