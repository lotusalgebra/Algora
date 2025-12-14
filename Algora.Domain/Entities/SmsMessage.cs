namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an SMS message sent to a customer.
    /// </summary>
    public class SmsMessage
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string? ExternalMessageId { get; set; } // Provider message ID
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
        public SmsTemplate? Template { get; set; }
        public string Status { get; set; } = "pending"; // pending, sent, delivered, failed
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int SegmentCount { get; set; } = 1;
        public decimal? Cost { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}