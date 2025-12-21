using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface IAiContentService
{
    IReadOnlyList<AiProviderInfo> GetAvailableTextProviders();
    IReadOnlyList<AiProviderInfo> GetAvailableImageProviders();

    Task<TextGenerationResponse> GenerateTitleAsync(TextGenerationRequest request, string? preferredProvider = null, CancellationToken ct = default);
    Task<TextGenerationResponse> GenerateDescriptionAsync(TextGenerationRequest request, string? preferredProvider = null, CancellationToken ct = default);
    Task<TextGenerationResponse> GenerateAltTextAsync(TextGenerationRequest request, string? preferredProvider = null, CancellationToken ct = default);
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request, string? preferredProvider = null, CancellationToken ct = default);

    Task<BulkGenerationResult> GenerateBulkContentAsync(BulkGenerationRequest request, CancellationToken ct = default);
}
