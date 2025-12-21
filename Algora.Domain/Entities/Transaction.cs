namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a payment transaction for an order.
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public long? PlatformTransactionId { get; set; }
        public string Kind { get; set; } = string.Empty; // sale, authorization, capture, refund, void
        public string Status { get; set; } = string.Empty; // pending, success, failure, error
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Gateway { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}