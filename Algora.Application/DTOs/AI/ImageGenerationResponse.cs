namespace Algora.Application.DTOs.AI;

public record ImageGenerationResponse
{
    public bool Success { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageBase64 { get; init; }
    public string? Error { get; init; }
    public decimal EstimatedCost { get; init; }
    public string ProviderUsed { get; init; } = string.Empty;
}
