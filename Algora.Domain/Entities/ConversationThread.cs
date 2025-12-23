namespace Algora.Domain.Entities;

/// <summary>
/// Represents a unified conversation thread linking messages across all channels.
/// </summary>
public class ConversationThread
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this conversation belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the customer (if identified).
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Customer's email address.
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Customer's phone number.
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Customer's display name.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Conversation subject or topic.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Current status: open, pending, resolved, closed.
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// Priority level: low, normal, high, urgent.
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// User ID of the assigned agent.
    /// </summary>
    public string? AssignedToUserId { get; set; }

    /// <summary>
    /// Primary channel: email, sms, whatsapp, facebook, instagram.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last message.
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Preview of the last message (truncated).
    /// </summary>
    public string? LastMessagePreview { get; set; }

    /// <summary>
    /// Count of unread messages.
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Comma-separated tags for categorization.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// When the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the conversation was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the conversation was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Messages in this conversation.
    /// </summary>
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    /// <summary>
    /// AI suggestions for this conversation.
    /// </summary>
    public ICollection<AiSuggestion> AiSuggestions { get; set; } = new List<AiSuggestion>();
}
