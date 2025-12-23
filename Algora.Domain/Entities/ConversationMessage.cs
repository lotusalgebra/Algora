namespace Algora.Domain.Entities;

/// <summary>
/// Represents an individual message within a conversation thread.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the conversation thread.
    /// </summary>
    public int ConversationThreadId { get; set; }

    /// <summary>
    /// Navigation property to the conversation thread.
    /// </summary>
    public ConversationThread ConversationThread { get; set; } = null!;

    /// <summary>
    /// Message channel: email, sms, whatsapp, facebook, instagram.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Message direction: inbound, outbound.
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the external message (SmsMessage.Id, WhatsAppMessage.Id, etc.).
    /// </summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Type of sender: customer, agent, system.
    /// </summary>
    public string SenderType { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the sender.
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Message content/body.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type: text, html, media.
    /// </summary>
    public string ContentType { get; set; } = "text";

    /// <summary>
    /// URL for any attached media.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Message status: pending, sent, delivered, read, failed.
    /// </summary>
    public string Status { get; set; } = "sent";

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the message was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Whether an AI suggestion was used for this message.
    /// </summary>
    public bool AiSuggestionUsed { get; set; }
}
