namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a refund for an order.
    /// </summary>
    public class Refund
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public long? PlatformRefundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Reason { get; set; }
        public string? Note { get; set; }
        public bool Restock { get; set; }
        public DateTime RefundedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}