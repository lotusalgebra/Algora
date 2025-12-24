using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface IChatbotService
{
    Task<ChatbotResponse> SendMessageAsync(ChatbotRequest request, CancellationToken ct = default);
    Task<ChatbotConversationDto> StartConversationAsync(string shopDomain, string sessionId, string? customerEmail, CancellationToken ct = default);
    Task<ChatbotConversationDto?> GetConversationAsync(int conversationId, CancellationToken ct = default);
    Task<ChatbotConversationDto?> GetConversationBySessionAsync(string sessionId, CancellationToken ct = default);
    Task EndConversationAsync(int conversationId, bool wasHelpful, CancellationToken ct = default);
    Task EscalateToAgentAsync(int conversationId, CancellationToken ct = default);
    Task<IEnumerable<ChatbotMessageDto>> GetMessagesAsync(int conversationId, CancellationToken ct = default);
}
