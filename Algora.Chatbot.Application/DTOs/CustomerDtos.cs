namespace Algora.Chatbot.Application.DTOs;

public record ShopifyCustomerDto
{
    public long Id { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public int OrdersCount { get; init; }
    public decimal TotalSpent { get; init; }
    public string? Currency { get; init; }
    public DateTime? CreatedAt { get; init; }
    public ShippingAddressDto? DefaultAddress { get; init; }
}

public record EscalatedConversationDto
{
    public int ConversationId { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public string? PrimaryIntent { get; init; }
    public string? EscalationReason { get; init; }
    public DateTime EscalatedAt { get; init; }
    public int MessageCount { get; init; }
    public string? LastMessage { get; init; }
}
