using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(string shopDomain, CancellationToken cancellationToken = default);
    Task<List<AnalyticsSnapshotDto>> GetAnalyticsHistoryAsync(string shopDomain, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<AnalyticsSnapshotDto>> GetAnalyticsAsync(string shopDomain, DateTime startDate, DateTime endDate);
    Task GenerateDailySnapshotAsync(string shopDomain, CancellationToken cancellationToken = default);
}
