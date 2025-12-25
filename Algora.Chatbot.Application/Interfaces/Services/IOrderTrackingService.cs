using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IOrderTrackingService
{
    Task<OrderTrackingResult> TrackOrderAsync(string shopDomain, string orderNumber, string? email, CancellationToken cancellationToken = default);
    Task<OrderTrackingResult> TrackOrderByIdAsync(string shopDomain, long orderId, CancellationToken cancellationToken = default);
    Task<List<OrderSummaryDto>> GetRecentOrdersAsync(string shopDomain, string customerEmail, int limit = 5, CancellationToken cancellationToken = default);
}
