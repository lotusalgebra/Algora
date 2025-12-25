using Algora.Chatbot.Domain.Enums;

namespace Algora.Chatbot.Domain.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    // Content
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    // AI Metadata
    public string? DetectedIntent { get; set; }
    public decimal? IntentConfidence { get; set; }
    public decimal? Sentiment { get; set; }
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public int? TokensUsed { get; set; }
    public decimal? AiCost { get; set; }

    // Suggested Actions (JSON array)
    public string? SuggestedActionsJson { get; set; }

    // Rich Content (product cards, order details, etc.)
    public string? AttachmentsJson { get; set; }

    // Delivery Status
    public bool IsDelivered { get; set; } = true;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
