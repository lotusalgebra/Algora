namespace Algora.Domain.Entities;

/// <summary>
/// Represents an Instagram Direct Message.
/// </summary>
public class InstagramMessage
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
    /// Instagram's message ID.
    /// </summary>
    public string InstagramMessageId { get; set; } = string.Empty;

    /// <summary>
    /// Instagram sender ID (IGSID).
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Sender's Instagram username.
    /// </summary>
    public string? SenderUsername { get; set; }

    /// <summary>
    /// Instagram recipient ID.
    /// </summary>
    public string RecipientId { get; set; } = string.Empty;

    /// <summary>
    /// Message direction: inbound, outbound.
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Message type: text, image, video, story_reply, story_mention.
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
    /// Story ID if this is a story reply/mention.
    /// </summary>
    public string? StoryId { get; set; }

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
