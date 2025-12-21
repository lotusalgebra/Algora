namespace Algora.WhatsApp.Entities;

/// <summary>
/// Represents a WhatsApp message sent or received via Facebook WhatsApp Business API.
/// </summary>
public class WhatsAppMessage
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string? ExternalMessageId { get; set; } // WhatsApp message ID from Meta
    public int? CustomerId { get; set; } // Reference to Customer in main domain
    public int? OrderId { get; set; } // Reference to Order in main domain
    public int? ConversationId { get; set; }
    public WhatsAppConversation? Conversation { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = "outbound"; // inbound, outbound
    public string MessageType { get; set; } = "text"; // text, template, image, document, audio, video, interactive
    public int? TemplateId { get; set; }
    public WhatsAppTemplate? Template { get; set; }
    public string? Content { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaMimeType { get; set; }
    public string? MediaCaption { get; set; }
    public string Status { get; set; } = "pending"; // pending, sent, delivered, read, failed
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
