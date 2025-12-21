namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a line item included in a fulfillment.
    /// </summary>
    public class FulfillmentLine
    {
        public int Id { get; set; }
        public int FulfillmentId { get; set; }
        public Fulfillment Fulfillment { get; set; } = null!;
        public int? OrderLineId { get; set; }
        public OrderLine? OrderLine { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}