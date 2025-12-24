namespace Algora.Application.DTOs.CustomerHub;

// ==================== Conversation Thread DTOs ====================

public record ConversationThreadDto(
    int Id,
    string ShopDomain,
    int? CustomerId,
    string? CustomerEmail,
    string? CustomerPhone,
    string? CustomerName,
    string? Subject,
    string Status,
    string Priority,
    string? AssignedToUserId,
    string Channel,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount,
    IEnumerable<string>? Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ResolvedAt
);

public record CreateConversationDto(
    string ShopDomain,
    int? CustomerId,
    string? CustomerEmail,
    string? CustomerPhone,
    string? CustomerName,
    string? Subject,
    string Channel,
    string? InitialMessage = null
);

public record ConversationFilterDto(
    string? Status = null,
    string? Priority = null,
    string? Channel = null,
    string? AssignedToUserId = null,
    string? SearchTerm = null,
    bool? UnreadOnly = null,
    int? CustomerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Skip = 0,
    int Take = 50
);

// ==================== Conversation Message DTOs ====================

public record ConversationMessageDto(
    int Id,
    int ConversationThreadId,
    string Channel,
    string Direction,
    string? ExternalMessageId,
    string SenderType,
    string? SenderName,
    string Content,
    string ContentType,
    string? MediaUrl,
    string Status,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    bool AiSuggestionUsed
);

public record SendMessageDto(
    string Content,
    string Channel,
    string? MediaUrl = null,
    string ContentType = "text",
    bool UseAiSuggestion = false
);

// ==================== Inbox Summary DTOs ====================

public record InboxSummaryDto(
    int TotalConversations,
    int OpenConversations,
    int PendingConversations,
    int UnreadMessages,
    int ResolvedToday,
    Dictionary<string, int> ConversationsByChannel,
    Dictionary<string, int> ConversationsByPriority,
    double AverageResponseTimeMinutes
);

// ==================== Quick Reply DTOs ====================

public record QuickReplyDto(
    int Id,
    string ShopDomain,
    string Title,
    string Content,
    string? Category,
    string? Shortcut,
    int UsageCount,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateQuickReplyDto(
    string ShopDomain,
    string Title,
    string Content,
    string? Category = null,
    string? Shortcut = null
);

public record UpdateQuickReplyDto(
    string? Title = null,
    string? Content = null,
    string? Category = null,
    string? Shortcut = null,
    bool? IsActive = null
);
