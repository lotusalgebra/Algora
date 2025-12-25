namespace Algora.Chatbot.Application.DTOs;

public record OrderTrackingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public long? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public DateTime? OrderDate { get; init; }
    public string? FulfillmentStatus { get; init; }
    public string? FinancialStatus { get; init; }
    public decimal? Total { get; init; }
    public string? Currency { get; init; }
    public List<TrackingInfo>? TrackingInfo { get; init; }
    public List<OrderLineItemDto>? LineItems { get; init; }
    public ShippingAddressDto? ShippingAddress { get; init; }
    public DateTime? EstimatedDelivery { get; init; }
}

public record TrackingInfo
{
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    public string? Carrier { get; init; }
    public string? Status { get; init; }
    public DateTime? ShippedAt { get; init; }
}

public record OrderLineItemDto
{
    public long LineItemId { get; init; }
    public string Title { get; init; } = "";
    public string? VariantTitle { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
}

public record ShippingAddressDto
{
    public string? Name { get; init; }
    public string? Address1 { get; init; }
    public string? Address2 { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? Country { get; init; }
    public string? Zip { get; init; }
}

public record OrderSummaryDto
{
    public long OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public string FulfillmentStatus { get; init; } = "";
    public decimal Total { get; init; }
    public string Currency { get; init; } = "";
    public int ItemCount { get; init; }
}

public record ShopifyOrderDto
{
    public long Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public string FulfillmentStatus { get; init; } = "";
    public string FinancialStatus { get; init; } = "";
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = "";
    public string? CustomerEmail { get; init; }
    public long? CustomerId { get; init; }
    public List<OrderLineItemDto>? LineItems { get; init; }
    public ShippingAddressDto? ShippingAddress { get; init; }
    public List<TrackingInfo>? Fulfillments { get; init; }
}
