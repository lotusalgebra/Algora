namespace Algora.Domain.Entities;

/// <summary>
/// Represents a Facebook Messenger message.
/// </summary>
public class FacebookMessage
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this message belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Facebook's message ID.
    /// </summary>
    public string FacebookMessageId { get; set; } = string.Empty;

    /// <summary>
    /// Facebook sender ID (PSID).
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Sender's display name.
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Facebook recipient ID (Page ID).
    /// </summary>
    public string RecipientId { get; set; } = string.Empty;

    /// <summary>
    /// Message direction: inbound, outbound.
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Message type: text, image, video, audio, file.
    /// </summary>
    public string MessageType { get; set; } = "text";

    /// <summary>
    /// Message text content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URL for attached media.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Message status: pending, sent, delivered, read, failed.
    /// </summary>
    public string Status { get; set; } = "sent";

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// When the message was delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the message was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
