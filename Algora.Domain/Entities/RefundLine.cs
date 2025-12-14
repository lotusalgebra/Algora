namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a line item included in a refund.
    /// </summary>
    public class RefundLine
    {
        public int Id { get; set; }
        public int RefundId { get; set; }
        public Refund Refund { get; set; } = null!;
        public int? OrderLineId { get; set; }
        public OrderLine? OrderLine { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public bool Restock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}