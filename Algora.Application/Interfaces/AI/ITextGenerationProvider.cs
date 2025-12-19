using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface ITextGenerationProvider
{
    string ProviderName { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }

    Task<TextGenerationResponse> GenerateTitleAsync(TextGenerationRequest request, CancellationToken ct = default);
    Task<TextGenerationResponse> GenerateDescriptionAsync(TextGenerationRequest request, CancellationToken ct = default);
    Task<TextGenerationResponse> GenerateAltTextAsync(TextGenerationRequest request, CancellationToken ct = default);
}
