using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for unified communication history across all channels.
/// </summary>
public interface ICommunicationHistoryService
{
    /// <summary>
    /// Gets paginated communication history with filters.
    /// </summary>
    Task<CommunicationHistoryResultDto> GetHistoryAsync(string shopDomain, CommunicationHistoryFilterDto filter);

    /// <summary>
    /// Gets communication statistics for the shop.
    /// </summary>
    Task<CommunicationStatsDto> GetStatsAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets a single communication history item by ID and channel.
    /// </summary>
    Task<CommunicationHistoryItemDto?> GetByIdAsync(string shopDomain, int id, string channel);
}
