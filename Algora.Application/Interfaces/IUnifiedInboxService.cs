using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing unified inbox conversations across all channels.
/// </summary>
public interface IUnifiedInboxService
{
    // Conversations
    Task<IEnumerable<ConversationThreadDto>> GetConversationsAsync(string shopDomain, ConversationFilterDto? filter = null);
    Task<ConversationThreadDto?> GetConversationAsync(int id);
    Task<ConversationThreadDto> CreateConversationAsync(CreateConversationDto dto);
    Task<ConversationThreadDto> UpdateConversationStatusAsync(int id, string status);
    Task<ConversationThreadDto> UpdateConversationPriorityAsync(int id, string priority);
    Task<ConversationThreadDto> AssignConversationAsync(int id, string? userId);
    Task<bool> AddTagsAsync(int id, IEnumerable<string> tags);
    Task<bool> RemoveTagsAsync(int id, IEnumerable<string> tags);

    // Messages
    Task<IEnumerable<ConversationMessageDto>> GetMessagesAsync(int conversationId);
    Task<ConversationMessageDto> SendMessageAsync(int conversationId, SendMessageDto dto);
    Task MarkAsReadAsync(int conversationId);

    // Aggregation
    Task<int> SyncMessagesAsync(string shopDomain);
    Task<InboxSummaryDto> GetInboxSummaryAsync(string shopDomain);

    // Quick Replies
    Task<IEnumerable<QuickReplyDto>> GetQuickRepliesAsync(string shopDomain);
    Task<QuickReplyDto> CreateQuickReplyAsync(CreateQuickReplyDto dto);
    Task<QuickReplyDto> UpdateQuickReplyAsync(int id, UpdateQuickReplyDto dto);
    Task<bool> DeleteQuickReplyAsync(int id);
    Task IncrementQuickReplyUsageAsync(int id);
}
