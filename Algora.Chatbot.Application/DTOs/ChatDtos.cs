namespace Algora.Chatbot.Application.DTOs;

public record ChatRequest
{
    public string ShopDomain { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string? VisitorId { get; init; }
    public string? CustomerEmail { get; init; }
    public string Message { get; init; } = "";
    public int? ConversationId { get; init; }
    public string? CurrentPageUrl { get; init; }
}

public record ChatResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int ConversationId { get; init; }
    public string Response { get; init; } = "";
    public string? Intent { get; init; }
    public decimal? Confidence { get; init; }
    public List<SuggestedAction>? SuggestedActions { get; init; }
    public List<ProductRecommendationDto>? Products { get; init; }
    public OrderTrackingResult? OrderInfo { get; init; }
}

public record StartConversationRequest
{
    public string ShopDomain { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string? VisitorId { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public long? ShopifyCustomerId { get; init; }
    public string? CurrentPageUrl { get; init; }
    public string? ReferrerUrl { get; init; }
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
}

public record SuggestedAction
{
    public string Label { get; init; } = "";
    public string Type { get; init; } = "";
    public string Value { get; init; } = "";
}

public record ChatContext
{
    public string ShopDomain { get; init; } = "";
    public string SystemPrompt { get; init; } = "";
    public List<ChatMessage> History { get; init; } = new();
    public string CurrentMessage { get; init; } = "";
    public CustomerContext? Customer { get; init; }
    public List<OrderContext>? RecentOrders { get; init; }
    public List<ProductContext>? RelevantProducts { get; init; }
    public PolicyContext? Policies { get; init; }
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 500;
}

public record ChatMessage
{
    public string Role { get; init; } = "";
    public string Content { get; init; } = "";
}

public record CustomerContext
{
    public long? CustomerId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
}

public record OrderContext
{
    public long OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public string FulfillmentStatus { get; init; } = "";
    public decimal Total { get; init; }
}

public record ProductContext
{
    public long ProductId { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
}

public record PolicyContext
{
    public string? ReturnPolicy { get; init; }
    public string? ShippingPolicy { get; init; }
    public int? ReturnWindowDays { get; init; }
    public decimal? FreeShippingThreshold { get; init; }
}

public record ChatCompletionResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string Response { get; init; } = "";
    public string? DetectedIntent { get; init; }
    public decimal? Confidence { get; init; }
    public List<SuggestedAction>? SuggestedActions { get; init; }
    public int TokensUsed { get; init; }
    public decimal EstimatedCost { get; init; }
    public string ProviderUsed { get; init; } = "";
    public string ModelUsed { get; init; } = "";
}

public record IntentClassificationResult
{
    public string Intent { get; init; } = "";
    public decimal Confidence { get; init; }
    public Dictionary<string, decimal>? AllIntents { get; init; }
}
