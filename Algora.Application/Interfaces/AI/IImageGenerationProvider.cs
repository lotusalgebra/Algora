using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface IImageGenerationProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }

    Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request, CancellationToken ct = default);
}
