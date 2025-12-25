using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IEscalationService
{
    Task<bool> ShouldEscalateAsync(int conversationId, CancellationToken cancellationToken = default);
    Task EscalateAsync(int conversationId, string reason, CancellationToken cancellationToken = default);
    Task<List<EscalatedConversationDto>> GetPendingEscalationsAsync(string shopDomain, CancellationToken cancellationToken = default);
    Task AssignAgentAsync(int conversationId, string agentEmail, CancellationToken cancellationToken = default);
}
