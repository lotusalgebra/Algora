namespace Algora.Application.DTOs.AI;

public record TextGenerationRequest
{
    public long ProductId { get; init; }
    public string? CurrentTitle { get; init; }
    public string? CurrentDescription { get; init; }
    public string? Category { get; init; }
    public string? Vendor { get; init; }
    public string? ProductType { get; init; }
    public string? Tags { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Features { get; init; }
    public string? ImageUrl { get; init; }
    public string? Tone { get; init; } = "professional, persuasive";
    public int MaxWords { get; init; } = 150;
}
