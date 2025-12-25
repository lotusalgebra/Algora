using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Domain.Entities;
using Algora.Chatbot.Domain.Enums;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Chatbot.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ChatbotDbContext _db;
    private readonly IChatbotOrchestrator _orchestrator;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ChatbotDbContext db,
        IChatbotOrchestrator orchestrator,
        ILogger<ChatService> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get or create conversation
            Conversation? conversation;
            if (request.ConversationId.HasValue)
            {
                conversation = await _db.Conversations
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken);

                if (conversation == null)
                {
                    return new ChatResponse { Success = false, Error = "Conversation not found" };
                }
            }
            else
            {
                // Start new conversation
                conversation = await StartConversationAsync(new StartConversationRequest
                {
                    ShopDomain = request.ShopDomain,
                    SessionId = request.SessionId,
                    VisitorId = request.VisitorId,
                    CustomerEmail = request.CustomerEmail,
                    CurrentPageUrl = request.CurrentPageUrl
                }, cancellationToken);
            }

            // Process message through orchestrator
            return await _orchestrator.ProcessMessageAsync(
                request.ShopDomain,
                conversation.Id,
                request.Message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return new ChatResponse { Success = false, Error = "An error occurred" };
        }
    }

    public async Task<Conversation> StartConversationAsync(StartConversationRequest request, CancellationToken cancellationToken = default)
    {
        // Check for existing active conversation
        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c =>
                c.ShopDomain == request.ShopDomain &&
                c.SessionId == request.SessionId &&
                c.Status == ConversationStatus.Active,
                cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var conversation = new Conversation
        {
            ShopDomain = request.ShopDomain,
            SessionId = request.SessionId,
            VisitorId = request.VisitorId,
            ShopifyCustomerId = request.ShopifyCustomerId,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CurrentPageUrl = request.CurrentPageUrl,
            ReferrerUrl = request.ReferrerUrl,
            UserAgent = request.UserAgent,
            IpAddress = request.IpAddress,
            Status = ConversationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        // Add welcome message if configured
        var settings = await _db.ChatbotSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == request.ShopDomain, cancellationToken);

        if (!string.IsNullOrEmpty(settings?.WelcomeMessage))
        {
            var welcomeMsg = new Message
            {
                ConversationId = conversation.Id,
                Role = MessageRole.Assistant,
                Content = settings.WelcomeMessage,
                CreatedAt = DateTime.UtcNow
            };
            _db.Messages.Add(welcomeMsg);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return conversation;
    }

    public async Task<Conversation?> GetConversationAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetConversationBySessionAsync(string shopDomain, string sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c =>
                c.ShopDomain == shopDomain &&
                c.SessionId == sessionId &&
                c.Status == ConversationStatus.Active,
                cancellationToken);
    }

    public async Task<List<Message>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task EndConversationAsync(int conversationId, bool wasHelpful, int? rating, string? feedback, CancellationToken cancellationToken = default)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null) return;

        conversation.Status = ConversationStatus.Resolved;
        conversation.WasHelpful = wasHelpful;
        conversation.Rating = rating;
        conversation.FeedbackComment = feedback;
        conversation.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
