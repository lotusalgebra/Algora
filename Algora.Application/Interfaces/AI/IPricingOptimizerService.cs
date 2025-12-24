using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface IPricingOptimizerService
{
    Task<PricingOptimizationResponse> GetSuggestionAsync(int productId, CancellationToken ct = default);
    Task<IEnumerable<PricingOptimizationResponse>> GetBulkSuggestionsAsync(IEnumerable<int> productIds, CancellationToken ct = default);
    Task ApplySuggestionAsync(int suggestionId, CancellationToken ct = default);
    Task<IEnumerable<PricingSuggestionDto>> GetHistoryAsync(int productId, int count = 10, CancellationToken ct = default);
}
