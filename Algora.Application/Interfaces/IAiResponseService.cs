using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for AI-powered response suggestions.
/// </summary>
public interface IAiResponseService
{
    /// <summary>
    /// Generates AI response suggestions for a conversation.
    /// </summary>
    Task<IEnumerable<AiSuggestionDto>> GenerateSuggestionsAsync(int conversationId, int suggestionCount = 3);

    /// <summary>
    /// Marks a suggestion as accepted and optionally modified.
    /// </summary>
    Task<AiSuggestionDto> AcceptSuggestionAsync(int suggestionId, bool wasModified = false);

    /// <summary>
    /// Generates a contextual reply based on specific context.
    /// </summary>
    Task<string> GenerateContextualReplyAsync(int conversationId, string context);

    /// <summary>
    /// Analyzes the sentiment of text.
    /// </summary>
    Task<SentimentAnalysisDto> AnalyzeSentimentAsync(string text);

    /// <summary>
    /// Summarizes a conversation for quick agent review.
    /// </summary>
    Task<string> SummarizeConversationAsync(int conversationId);

    /// <summary>
    /// Suggests tags for a conversation based on content.
    /// </summary>
    Task<IEnumerable<string>> SuggestTagsAsync(int conversationId);
}
