namespace Algora.Application.DTOs.AI;

public record BulkGenerationRequest
{
    public IReadOnlyList<long> ProductIds { get; init; } = Array.Empty<long>();
    public bool GenerateTitles { get; init; }
    public bool GenerateDescriptions { get; init; }
    public bool GenerateAltText { get; init; }
    public bool GenerateImages { get; init; }
    public string? TextProvider { get; init; }
    public string? ImageProvider { get; init; }
    public string? Tone { get; init; }
    public int MaxDescriptionWords { get; init; } = 150;
}
