namespace Algora.Application.DTOs.AI;

public record BulkGenerationResult
{
    public int TotalProducts { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyList<BulkGenerationItemResult> Results { get; init; } = Array.Empty<BulkGenerationItemResult>();
    public decimal TotalEstimatedCost { get; init; }
}

public record BulkGenerationItemResult
{
    public long ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? GeneratedTitle { get; init; }
    public string? GeneratedDescription { get; init; }
    public string? GeneratedAltText { get; init; }
    public string? GeneratedImageUrl { get; init; }
    public string? Error { get; init; }
}
