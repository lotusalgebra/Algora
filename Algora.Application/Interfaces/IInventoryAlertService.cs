using Algora.Application.DTOs.Inventory;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing inventory alerts and notifications.
/// </summary>
public interface IInventoryAlertService
{
    /// <summary>
    /// Check predictions and create alerts based on thresholds.
    /// </summary>
    /// <returns>Number of alerts generated</returns>
    Task<int> GenerateAlertsAsync(string shopDomain);

    /// <summary>
    /// Send pending notifications for alerts.
    /// </summary>
    /// <returns>Number of notifications sent</returns>
    Task<int> SendPendingNotificationsAsync(string shopDomain);

    /// <summary>
    /// Get active alerts for a shop.
    /// </summary>
    Task<PaginatedResult<InventoryAlertDto>> GetAlertsAsync(
        string shopDomain,
        string? status = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get alert count by severity.
    /// </summary>
    Task<Dictionary<string, int>> GetAlertCountsBySeverityAsync(string shopDomain);

    /// <summary>
    /// Acknowledge an alert.
    /// </summary>
    Task AcknowledgeAlertAsync(int alertId);

    /// <summary>
    /// Dismiss an alert.
    /// </summary>
    Task DismissAlertAsync(int alertId, string? reason = null);

    /// <summary>
    /// Mark alert as resolved (typically when inventory is restocked).
    /// </summary>
    Task ResolveAlertAsync(int alertId);

    /// <summary>
    /// Get alert settings for a shop.
    /// </summary>
    Task<InventoryAlertSettingsDto> GetSettingsAsync(string shopDomain);

    /// <summary>
    /// Update alert settings for a shop.
    /// </summary>
    Task<InventoryAlertSettingsDto> UpdateSettingsAsync(
        string shopDomain,
        UpdateInventoryAlertSettingsDto settings);
}
