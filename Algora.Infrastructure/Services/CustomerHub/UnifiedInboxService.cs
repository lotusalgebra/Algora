using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for managing unified inbox conversations across all channels.
/// </summary>
public class UnifiedInboxService : IUnifiedInboxService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UnifiedInboxService> _logger;

    public UnifiedInboxService(AppDbContext db, ILogger<UnifiedInboxService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ConversationThreadDto>> GetConversationsAsync(string shopDomain, ConversationFilterDto? filter = null)
    {
        var query = _db.ConversationThreads
            .Where(c => c.ShopDomain == shopDomain);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Priority))
                query = query.Where(c => c.Priority == filter.Priority);

            if (!string.IsNullOrEmpty(filter.Channel))
                query = query.Where(c => c.Channel == filter.Channel);

            if (!string.IsNullOrEmpty(filter.AssignedToUserId))
                query = query.Where(c => c.AssignedToUserId == filter.AssignedToUserId);

            if (filter.UnreadOnly == true)
                query = query.Where(c => c.UnreadCount > 0);

            if (filter.CustomerId.HasValue)
                query = query.Where(c => c.CustomerId == filter.CustomerId);

            if (filter.FromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(c => c.CreatedAt <= filter.ToDate);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(c =>
                    (c.CustomerName != null && c.CustomerName.ToLower().Contains(term)) ||
                    (c.CustomerEmail != null && c.CustomerEmail.ToLower().Contains(term)) ||
                    (c.Subject != null && c.Subject.ToLower().Contains(term)) ||
                    (c.LastMessagePreview != null && c.LastMessagePreview.ToLower().Contains(term)));
            }

            query = query.Skip(filter.Skip).Take(filter.Take);
        }
        else
        {
            query = query.Take(50);
        }

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        return conversations.Select(MapToDto);
    }

    public async Task<ConversationThreadDto?> GetConversationAsync(int id)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id);
        return conversation != null ? MapToDto(conversation) : null;
    }

    public async Task<ConversationThreadDto> CreateConversationAsync(CreateConversationDto dto)
    {
        var conversation = new ConversationThread
        {
            ShopDomain = dto.ShopDomain,
            CustomerId = dto.CustomerId,
            CustomerEmail = dto.CustomerEmail,
            CustomerPhone = dto.CustomerPhone,
            CustomerName = dto.CustomerName,
            Subject = dto.Subject,
            Channel = dto.Channel,
            Status = "open",
            Priority = "normal",
            CreatedAt = DateTime.UtcNow
        };

        _db.ConversationThreads.Add(conversation);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(dto.InitialMessage))
        {
            var message = new ConversationMessage
            {
                ConversationThreadId = conversation.Id,
                Channel = dto.Channel,
                Direction = "inbound",
                SenderType = "customer",
                SenderName = dto.CustomerName,
                Content = dto.InitialMessage,
                ContentType = "text",
                Status = "delivered",
                SentAt = DateTime.UtcNow
            };

            _db.ConversationMessages.Add(message);
            conversation.LastMessageAt = message.SentAt;
            conversation.LastMessagePreview = dto.InitialMessage.Length > 100
                ? dto.InitialMessage[..100] + "..."
                : dto.InitialMessage;
            conversation.UnreadCount = 1;
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Created conversation {Id} for shop {ShopDomain}", conversation.Id, dto.ShopDomain);
        return MapToDto(conversation);
    }

    public async Task<ConversationThreadDto> UpdateConversationStatusAsync(int id, string status)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id)
            ?? throw new InvalidOperationException($"Conversation {id} not found");

        conversation.Status = status;
        conversation.UpdatedAt = DateTime.UtcNow;

        if (status == "resolved" || status == "closed")
            conversation.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(conversation);
    }

    public async Task<ConversationThreadDto> UpdateConversationPriorityAsync(int id, string priority)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id)
            ?? throw new InvalidOperationException($"Conversation {id} not found");

        conversation.Priority = priority;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(conversation);
    }

    public async Task<ConversationThreadDto> AssignConversationAsync(int id, string? userId)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id)
            ?? throw new InvalidOperationException($"Conversation {id} not found");

        conversation.AssignedToUserId = userId;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(conversation);
    }

    public async Task<bool> AddTagsAsync(int id, IEnumerable<string> tags)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id);
        if (conversation == null) return false;

        var existingTags = string.IsNullOrEmpty(conversation.Tags)
            ? new List<string>()
            : conversation.Tags.Split(',').ToList();

        existingTags.AddRange(tags.Where(t => !existingTags.Contains(t)));
        conversation.Tags = string.Join(",", existingTags);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTagsAsync(int id, IEnumerable<string> tags)
    {
        var conversation = await _db.ConversationThreads.FindAsync(id);
        if (conversation == null) return false;

        if (!string.IsNullOrEmpty(conversation.Tags))
        {
            var existingTags = conversation.Tags.Split(',').ToList();
            existingTags.RemoveAll(t => tags.Contains(t));
            conversation.Tags = existingTags.Count > 0 ? string.Join(",", existingTags) : null;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<IEnumerable<ConversationMessageDto>> GetMessagesAsync(int conversationId)
    {
        var messages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return messages.Select(m => new ConversationMessageDto(
            m.Id,
            m.ConversationThreadId,
            m.Channel,
            m.Direction,
            m.ExternalMessageId,
            m.SenderType,
            m.SenderName,
            m.Content,
            m.ContentType,
            m.MediaUrl,
            m.Status,
            m.SentAt,
            m.DeliveredAt,
            m.ReadAt,
            m.AiSuggestionUsed
        ));
    }

    public async Task<ConversationMessageDto> SendMessageAsync(int conversationId, SendMessageDto dto)
    {
        var conversation = await _db.ConversationThreads.FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found");

        var message = new ConversationMessage
        {
            ConversationThreadId = conversationId,
            Channel = dto.Channel,
            Direction = "outbound",
            SenderType = "agent",
            Content = dto.Content,
            ContentType = dto.ContentType,
            MediaUrl = dto.MediaUrl,
            Status = "pending",
            SentAt = DateTime.UtcNow,
            AiSuggestionUsed = dto.UseAiSuggestion
        };

        _db.ConversationMessages.Add(message);

        conversation.LastMessageAt = message.SentAt;
        conversation.LastMessagePreview = dto.Content.Length > 100 ? dto.Content[..100] + "..." : dto.Content;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // TODO: Dispatch to actual channel (WhatsApp, SMS, Email, etc.)
        message.Status = "sent";
        await _db.SaveChangesAsync();

        return new ConversationMessageDto(
            message.Id,
            message.ConversationThreadId,
            message.Channel,
            message.Direction,
            message.ExternalMessageId,
            message.SenderType,
            message.SenderName,
            message.Content,
            message.ContentType,
            message.MediaUrl,
            message.Status,
            message.SentAt,
            message.DeliveredAt,
            message.ReadAt,
            message.AiSuggestionUsed
        );
    }

    public async Task MarkAsReadAsync(int conversationId)
    {
        var conversation = await _db.ConversationThreads.FindAsync(conversationId);
        if (conversation == null) return;

        conversation.UnreadCount = 0;
        conversation.UpdatedAt = DateTime.UtcNow;

        var unreadMessages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId && m.ReadAt == null && m.Direction == "inbound")
            .ToListAsync();

        foreach (var msg in unreadMessages)
            msg.ReadAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<int> SyncMessagesAsync(string shopDomain)
    {
        // This would sync from WhatsApp, SMS, Email services
        // For now, return 0 as placeholder
        _logger.LogInformation("Syncing messages for shop {ShopDomain}", shopDomain);
        return 0;
    }

    public async Task<InboxSummaryDto> GetInboxSummaryAsync(string shopDomain)
    {
        var conversations = await _db.ConversationThreads
            .Where(c => c.ShopDomain == shopDomain)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return new InboxSummaryDto(
            TotalConversations: conversations.Count,
            OpenConversations: conversations.Count(c => c.Status == "open"),
            PendingConversations: conversations.Count(c => c.Status == "pending"),
            UnreadMessages: conversations.Sum(c => c.UnreadCount),
            ResolvedToday: conversations.Count(c => c.ResolvedAt?.Date == today),
            ConversationsByChannel: conversations.GroupBy(c => c.Channel).ToDictionary(g => g.Key, g => g.Count()),
            ConversationsByPriority: conversations.GroupBy(c => c.Priority).ToDictionary(g => g.Key, g => g.Count()),
            AverageResponseTimeMinutes: 0 // TODO: Calculate from message timestamps
        );
    }

    public async Task<IEnumerable<QuickReplyDto>> GetQuickRepliesAsync(string shopDomain)
    {
        var replies = await _db.QuickReplies
            .Where(r => r.ShopDomain == shopDomain && r.IsActive)
            .OrderByDescending(r => r.UsageCount)
            .ToListAsync();

        return replies.Select(r => new QuickReplyDto(
            r.Id, r.ShopDomain, r.Title, r.Content, r.Category, r.Shortcut, r.UsageCount, r.IsActive, r.CreatedAt
        ));
    }

    public async Task<QuickReplyDto> CreateQuickReplyAsync(CreateQuickReplyDto dto)
    {
        var reply = new QuickReply
        {
            ShopDomain = dto.ShopDomain,
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            Shortcut = dto.Shortcut,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.QuickReplies.Add(reply);
        await _db.SaveChangesAsync();

        return new QuickReplyDto(
            reply.Id, reply.ShopDomain, reply.Title, reply.Content, reply.Category, reply.Shortcut, reply.UsageCount, reply.IsActive, reply.CreatedAt
        );
    }

    public async Task<QuickReplyDto> UpdateQuickReplyAsync(int id, UpdateQuickReplyDto dto)
    {
        var reply = await _db.QuickReplies.FindAsync(id)
            ?? throw new InvalidOperationException($"Quick reply {id} not found");

        if (dto.Title != null) reply.Title = dto.Title;
        if (dto.Content != null) reply.Content = dto.Content;
        if (dto.Category != null) reply.Category = dto.Category;
        if (dto.Shortcut != null) reply.Shortcut = dto.Shortcut;
        if (dto.IsActive.HasValue) reply.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();

        return new QuickReplyDto(
            reply.Id, reply.ShopDomain, reply.Title, reply.Content, reply.Category, reply.Shortcut, reply.UsageCount, reply.IsActive, reply.CreatedAt
        );
    }

    public async Task<bool> DeleteQuickReplyAsync(int id)
    {
        var reply = await _db.QuickReplies.FindAsync(id);
        if (reply == null) return false;

        _db.QuickReplies.Remove(reply);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task IncrementQuickReplyUsageAsync(int id)
    {
        var reply = await _db.QuickReplies.FindAsync(id);
        if (reply != null)
        {
            reply.UsageCount++;
            await _db.SaveChangesAsync();
        }
    }

    private static ConversationThreadDto MapToDto(ConversationThread c) => new(
        c.Id,
        c.ShopDomain,
        c.CustomerId,
        c.CustomerEmail,
        c.CustomerPhone,
        c.CustomerName,
        c.Subject,
        c.Status,
        c.Priority,
        c.AssignedToUserId,
        c.Channel,
        c.LastMessageAt,
        c.LastMessagePreview,
        c.UnreadCount,
        string.IsNullOrEmpty(c.Tags) ? null : c.Tags.Split(','),
        c.CreatedAt,
        c.UpdatedAt,
        c.ResolvedAt
    );
}
