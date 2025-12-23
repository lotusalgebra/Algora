using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing product exchanges.
/// </summary>
public interface IExchangeService
{
    /// <summary>
    /// Gets exchanges with optional filtering.
    /// </summary>
    Task<IEnumerable<ExchangeDto>> GetExchangesAsync(string shopDomain, ExchangeFilterDto? filter = null);

    /// <summary>
    /// Gets a single exchange by ID.
    /// </summary>
    Task<ExchangeDto?> GetExchangeAsync(int id);

    /// <summary>
    /// Gets a single exchange by exchange number.
    /// </summary>
    Task<ExchangeDto?> GetExchangeByNumberAsync(string shopDomain, string exchangeNumber);

    /// <summary>
    /// Creates a new exchange request.
    /// </summary>
    Task<ExchangeDto> CreateExchangeAsync(CreateExchangeDto dto);

    /// <summary>
    /// Updates exchange items (select new products).
    /// </summary>
    Task<ExchangeDto> UpdateExchangeItemsAsync(int id, UpdateExchangeItemsDto dto);

    /// <summary>
    /// Approves an exchange request.
    /// </summary>
    Task<ExchangeDto> ApproveExchangeAsync(int id, string? notes = null);

    /// <summary>
    /// Marks original items as received.
    /// </summary>
    Task<ExchangeDto> MarkItemsReceivedAsync(int id);

    /// <summary>
    /// Completes the exchange (after new items shipped).
    /// </summary>
    Task<ExchangeDto> CompleteExchangeAsync(int id);

    /// <summary>
    /// Cancels an exchange request.
    /// </summary>
    Task<ExchangeDto> CancelExchangeAsync(int id, string reason);

    /// <summary>
    /// Checks if an order is eligible for exchange.
    /// </summary>
    Task<ExchangeEligibilityDto> CheckEligibilityAsync(int orderId);

    /// <summary>
    /// Gets available products for exchange.
    /// </summary>
    Task<IEnumerable<ExchangeProductOptionDto>> GetExchangeOptionsAsync(string shopDomain, int? originalProductId = null);
}
