namespace Algora.Application.DTOs.AI;

public record ImageGenerationRequest
{
    public long ProductId { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? ProductTitle { get; init; }
    public string? ProductDescription { get; init; }
    public string? Style { get; init; } = "product photography, white background, professional lighting";
    public string Size { get; init; } = "1024x1024";
    public string Quality { get; init; } = "standard";
}
