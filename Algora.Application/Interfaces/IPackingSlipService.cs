using Algora.Application.DTOs.Operations;

namespace Algora.Application.Interfaces;

public interface IPackingSlipService
{
    /// <summary>
    /// Generates a packing slip PDF for a single order
    /// </summary>
    Task<PackingSlipResult> GeneratePackingSlipAsync(
        int orderId,
        PackingSlipSettings? settings = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates packing slips for multiple orders
    /// </summary>
    Task<BulkPackingSlipResult> GenerateBulkPackingSlipsAsync(
        int[] orderIds,
        PackingSlipSettings? settings = null,
        bool combineIntoPdf = true,
        CancellationToken ct = default);

    /// <summary>
    /// Gets packing slip data for an order (without generating PDF)
    /// </summary>
    Task<PackingSlipDto?> GetPackingSlipDataAsync(
        int orderId,
        CancellationToken ct = default);
}
