namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a WhatsApp message sent or received.
    /// </summary>
    public class WhatsAppMessage
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string? ExternalMessageId { get; set; } // WhatsApp message ID
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Direction { get; set; } = "outbound"; // inbound, outbound
        public string MessageType { get; set; } = "text"; // text, template, image, document, audio, video, interactive
        public int? TemplateId { get; set; }
        public WhatsAppTemplate? Template { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaMimeType { get; set; }
        public string Status { get; set; } = "pending"; // pending, sent, delivered, read, failed
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}