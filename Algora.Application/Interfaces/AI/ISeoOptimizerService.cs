using Algora.Application.DTOs.AI;

namespace Algora.Application.Interfaces.AI;

public interface ISeoOptimizerService
{
    Task<SeoOptimizationResponse> OptimizeAsync(SeoOptimizationRequest request, CancellationToken ct = default);
    Task<IEnumerable<SeoOptimizationResponse>> BulkOptimizeAsync(IEnumerable<int> productIds, CancellationToken ct = default);
    Task<int> GetSeoScoreAsync(int productId, CancellationToken ct = default);
    Task ApplySeoDataAsync(int productId, SeoOptimizationResponse data, CancellationToken ct = default);
    Task<SeoOptimizationResponse?> GetSeoDataAsync(int productId, CancellationToken ct = default);
}
