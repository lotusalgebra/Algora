namespace Algora.Application.DTOs.CustomerHub;

// ==================== AI Suggestion DTOs ====================

public record AiSuggestionDto(
    int Id,
    int ConversationThreadId,
    int? ConversationMessageId,
    string SuggestionText,
    decimal Confidence,
    string Provider,
    string Model,
    int? TokensUsed,
    decimal? EstimatedCost,
    bool? WasAccepted,
    bool? WasModified,
    DateTime CreatedAt,
    DateTime? AcceptedAt
);

public record GenerateSuggestionsRequestDto(
    int ConversationId,
    int SuggestionCount = 3,
    string? AdditionalContext = null
);

// ==================== Sentiment Analysis DTOs ====================

public record SentimentAnalysisDto(
    string Sentiment, // positive, negative, neutral
    decimal ConfidenceScore,
    string? Summary,
    IEnumerable<string>? KeyPhrases,
    bool? RequiresUrgentAttention
);

// ==================== AI Response Context DTOs ====================

public record ConversationContextDto(
    int ConversationId,
    string CustomerName,
    string? CustomerEmail,
    int MessageCount,
    string LastCustomerMessage,
    IEnumerable<OrderSummaryDto>? RecentOrders,
    IEnumerable<string>? PreviousTopics,
    string? CustomerSegment,
    decimal? CustomerLifetimeValue
);

public record OrderSummaryDto(
    int OrderId,
    string OrderNumber,
    DateTime CreatedAt,
    decimal Total,
    string Status,
    int ItemCount
);
