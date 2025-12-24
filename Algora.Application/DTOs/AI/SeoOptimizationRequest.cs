namespace Algora.Application.DTOs.AI;

public record SeoOptimizationRequest
{
    public int ProductId { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Vendor { get; init; }
    public string? Tags { get; init; }
    public string? ExistingKeywords { get; init; }
}

public record SeoOptimizationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int ProductId { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? FocusKeyword { get; init; }
    public List<string> Keywords { get; init; } = new();
    public int SeoScore { get; init; }
    public string? SeoScoreExplanation { get; init; }
    public string? Provider { get; init; }
    public int? TokensUsed { get; init; }
    public decimal EstimatedCost { get; init; }
}
