namespace Algora.Application.Interfaces.AI;

/// <summary>
/// Simple AI text generation provider for general-purpose text generation.
/// Used for AI-powered response suggestions in the unified inbox.
/// </summary>
public interface IAiTextProvider
{
    /// <summary>
    /// Generates text based on the given prompt.
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider information (name and model).
    /// </summary>
    (string ProviderName, string ModelName) GetProviderInfo();
}
