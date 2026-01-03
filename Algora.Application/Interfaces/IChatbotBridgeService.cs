using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

public interface IChatbotBridgeService
{
    /// <summary>
    /// Get all escalated conversations for a shop
    /// </summary>
    Task<ChatbotConversationListResult> GetEscalatedConversationsAsync(
        string shopDomain,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chatbot conversations for a shop
    /// </summary>
    Task<ChatbotConversationListResult> GetConversationsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific conversation with all messages
    /// </summary>
    Task<ChatbotConversationDetailDto?> GetConversationAsync(
        int conversationId,
        string shopDomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message as an agent
    /// </summary>
    Task<bool> SendAgentMessageAsync(
        int conversationId,
        string shopDomain,
        SendAgentMessageDto message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign an agent to a conversation
    /// </summary>
    Task<bool> AssignAgentAsync(
        int conversationId,
        string shopDomain,
        string agentEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a conversation
    /// </summary>
    Task<bool> ResolveConversationAsync(
        int conversationId,
        string shopDomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of escalated conversations waiting for agent
    /// </summary>
    Task<int> GetEscalatedCountAsync(
        string shopDomain,
        CancellationToken cancellationToken = default);
}
