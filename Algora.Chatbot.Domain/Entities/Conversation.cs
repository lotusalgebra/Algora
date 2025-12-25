using Algora.Chatbot.Domain.Enums;

namespace Algora.Chatbot.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Session
    public string SessionId { get; set; } = string.Empty;
    public string? VisitorId { get; set; }

    // Customer (optional - may be guest)
    public long? ShopifyCustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }

    // Context
    public string? CurrentPageUrl { get; set; }
    public string? ReferrerUrl { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    // Status
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public string? PrimaryIntent { get; set; }
    public decimal? OverallSentiment { get; set; }

    // Related Data
    public long? RelatedOrderId { get; set; }
    public long? RelatedProductId { get; set; }
    public int? ReturnRequestId { get; set; }

    // Escalation
    public bool IsEscalated { get; set; }
    public string? EscalationReason { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public string? AssignedAgentEmail { get; set; }

    // Feedback
    public int? Rating { get; set; }
    public string? FeedbackComment { get; set; }
    public bool? WasHelpful { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
