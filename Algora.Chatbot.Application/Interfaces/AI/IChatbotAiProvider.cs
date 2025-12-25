using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.AI;

public interface IChatbotAiProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    int Priority { get; }

    Task<ChatCompletionResult> GenerateResponseAsync(
        ChatContext context,
        CancellationToken cancellationToken = default);

    Task<IntentClassificationResult> ClassifyIntentAsync(
        string message,
        CancellationToken cancellationToken = default);

    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
