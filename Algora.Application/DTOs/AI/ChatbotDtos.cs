namespace Algora.Application.DTOs.AI;

public record ChatbotRequest
{
    public string ShopDomain { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string? CustomerEmail { get; init; }
    public string Message { get; init; } = "";
    public int? ConversationId { get; init; }
}

public record ChatbotResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int ConversationId { get; init; }
    public string Response { get; init; } = "";
    public string? Intent { get; init; }
    public decimal? Confidence { get; init; }
    public List<ChatbotAction> SuggestedActions { get; init; } = new();
}

public record ChatbotAction
{
    public string Label { get; init; } = "";
    public string Type { get; init; } = ""; // link, action, message
    public string Value { get; init; } = "";
}

public record ChatbotConversationDto
{
    public int Id { get; init; }
    public string SessionId { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Topic { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ChatbotMessageDto> Messages { get; init; } = new();
}

public record ChatbotMessageDto
{
    public int Id { get; init; }
    public string Role { get; init; } = "";
    public string Content { get; init; } = "";
    public string? Intent { get; init; }
    public List<ChatbotAction>? SuggestedActions { get; init; }
    public DateTime CreatedAt { get; init; }
}
