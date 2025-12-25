using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Domain.Entities;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<Conversation> StartConversationAsync(StartConversationRequest request, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationAsync(int id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationBySessionAsync(string shopDomain, string sessionId, CancellationToken cancellationToken = default);
    Task<List<Message>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default);
    Task EndConversationAsync(int conversationId, bool wasHelpful, int? rating, string? feedback, CancellationToken cancellationToken = default);
}
