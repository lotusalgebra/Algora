namespace Algora.Application.DTOs.AI;

public record PricingOptimizationRequest
{
    public int ProductId { get; init; }
    public string Title { get; init; } = "";
    public string? Category { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal? CostOfGoodsSold { get; init; }
    public int InventoryQuantity { get; init; }
    public int SalesCount30Days { get; init; }
}

public record PricingOptimizationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int ProductId { get; init; }
    public int? SuggestionId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal SuggestedPrice { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public decimal PriceChange { get; init; }
    public decimal ChangePercent { get; init; }
    public decimal CurrentMargin { get; init; }
    public decimal? SuggestedMargin { get; init; }
    public string? Reasoning { get; init; }
    public decimal Confidence { get; init; }
    public string? Provider { get; init; }
}

public record PricingSuggestionDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal SuggestedPrice { get; init; }
    public decimal ChangePercent { get; init; }
    public string? Reasoning { get; init; }
    public decimal Confidence { get; init; }
    public bool WasApplied { get; init; }
    public DateTime CreatedAt { get; init; }
}
