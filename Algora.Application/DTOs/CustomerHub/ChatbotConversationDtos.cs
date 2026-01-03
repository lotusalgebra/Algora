namespace Algora.Application.DTOs.CustomerHub;

public record ChatbotConversationDto
{
    public int Id { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? EscalationReason { get; init; }
    public DateTime? EscalatedAt { get; init; }
    public string? AssignedAgentEmail { get; init; }
    public string? LastMessage { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ChatbotConversationDetailDto
{
    public int Id { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string? VisitorId { get; init; }
    public long? ShopifyCustomerId { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PrimaryIntent { get; init; }
    public bool IsEscalated { get; init; }
    public string? EscalationReason { get; init; }
    public DateTime? EscalatedAt { get; init; }
    public string? AssignedAgentEmail { get; init; }
    public int? Rating { get; init; }
    public bool? WasHelpful { get; init; }
    public string? Feedback { get; init; }
    public string? CurrentPageUrl { get; init; }
    public List<ChatbotMessageDto> Messages { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ChatbotMessageDto
{
    public int Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? DetectedIntent { get; init; }
    public decimal? IntentConfidence { get; init; }
    public string? AiProvider { get; init; }
    public int? TokensUsed { get; init; }
    public string? SuggestedActionsJson { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SendAgentMessageDto(
    string Message,
    string AgentEmail,
    string? AgentName
);

public record ChatbotConversationListResult
{
    public List<ChatbotConversationDto> Conversations { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
