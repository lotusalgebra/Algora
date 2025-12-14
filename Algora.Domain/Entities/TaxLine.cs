namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a tax line applied to an order or line item.
    /// </summary>
    public class TaxLine
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public int? OrderLineId { get; set; }
        public OrderLine? OrderLine { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}