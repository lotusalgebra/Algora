namespace Algora.Application.DTOs.AI;

public record AiProviderInfo(
    string Name,
    string DisplayName,
    bool IsConfigured,
    string[] SupportedFeatures
);
