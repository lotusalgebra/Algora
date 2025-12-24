namespace Algora.Domain.Entities;

/// <summary>
/// Represents an AI-generated response suggestion for a conversation.
/// </summary>
public class AiSuggestion
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
    /// Foreign key to the specific message this suggestion responds to.
    /// </summary>
    public int? ConversationMessageId { get; set; }

    /// <summary>
    /// Navigation property to the specific message.
    /// </summary>
    public ConversationMessage? ConversationMessage { get; set; }

    /// <summary>
    /// The suggested response text.
    /// </summary>
    public string SuggestionText { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0-100).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// AI provider used: openai, anthropic.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Model name/ID used.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens consumed.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Estimated cost of the API call.
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Whether this suggestion was accepted by the agent.
    /// </summary>
    public bool? WasAccepted { get; set; }

    /// <summary>
    /// Whether the suggestion was modified before sending.
    /// </summary>
    public bool? WasModified { get; set; }

    /// <summary>
    /// When the suggestion was generated.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the suggestion was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }
}
