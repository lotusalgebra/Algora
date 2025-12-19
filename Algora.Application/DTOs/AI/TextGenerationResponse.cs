namespace Algora.Application.DTOs.AI;

public record TextGenerationResponse
{
    public bool Success { get; init; }
    public string? GeneratedText { get; init; }
    public string? Error { get; init; }
    public int TokensUsed { get; init; }
    public decimal EstimatedCost { get; init; }
    public string ProviderUsed { get; init; } = string.Empty;
}
