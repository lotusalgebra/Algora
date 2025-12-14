namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a fulfillment/shipment for an order.
    /// </summary>
    public class Fulfillment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public long? PlatformFulfillmentId { get; set; }
        public string Status { get; set; } = "pending"; // pending, success, cancelled, error
        public string? TrackingNumber { get; set; }
        public string? TrackingUrl { get; set; }
        public string? TrackingCompany { get; set; }
        public string? ShipmentStatus { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}