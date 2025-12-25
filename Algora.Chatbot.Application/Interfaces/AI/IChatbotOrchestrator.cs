using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.AI;

public interface IChatbotOrchestrator
{
    Task<ChatResponse> ProcessMessageAsync(
        string shopDomain,
        int conversationId,
        string userMessage,
        CancellationToken cancellationToken = default);

    Task<List<ProductRecommendationDto>> GetRecommendationsAsync(
        string shopDomain,
        int conversationId,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
