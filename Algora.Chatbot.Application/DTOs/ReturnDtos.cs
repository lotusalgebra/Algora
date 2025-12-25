namespace Algora.Chatbot.Application.DTOs;

public record ReturnEligibilityResult
{
    public bool IsEligible { get; init; }
    public string? Reason { get; init; }
    public int? DaysRemaining { get; init; }
    public List<ReturnableItemDto>? ReturnableItems { get; init; }
    public string? PolicySummary { get; init; }
}

public record ReturnableItemDto
{
    public long LineItemId { get; init; }
    public string Title { get; init; } = "";
    public int Quantity { get; init; }
    public int ReturnableQuantity { get; init; }
    public decimal Price { get; init; }
}

public record ReturnInitiationRequest
{
    public string ShopDomain { get; init; } = "";
    public long OrderId { get; init; }
    public string CustomerEmail { get; init; } = "";
    public List<ReturnItemRequest> Items { get; init; } = new();
    public string Reason { get; init; } = "";
    public string? Comments { get; init; }
}

public record ReturnItemRequest
{
    public long LineItemId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
}

public record ReturnInitiationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ReturnNumber { get; init; }
    public string? ReturnLabelUrl { get; init; }
    public string? Instructions { get; init; }
    public decimal? RefundAmount { get; init; }
}

public record ReturnStatusResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ReturnNumber { get; init; }
    public string Status { get; init; } = "";
    public DateTime? CreatedAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
    public DateTime? RefundedAt { get; init; }
    public decimal? RefundAmount { get; init; }
    public List<ReturnableItemDto>? Items { get; init; }
}
