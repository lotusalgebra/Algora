using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing review request email automations.
/// </summary>
public interface IReviewEmailService
{
    #region Email Automations

    /// <summary>
    /// Gets email automations for a shop.
    /// </summary>
    Task<List<ReviewEmailAutomationListDto>> GetAutomationsAsync(string shopDomain);

    /// <summary>
    /// Gets an email automation by ID.
    /// </summary>
    Task<ReviewEmailAutomationDto?> GetAutomationByIdAsync(int automationId);

    /// <summary>
    /// Creates a new email automation.
    /// </summary>
    Task<ReviewEmailAutomationDto> CreateAutomationAsync(string shopDomain, CreateReviewEmailAutomationDto dto);

    /// <summary>
    /// Updates an email automation.
    /// </summary>
    Task<ReviewEmailAutomationDto?> UpdateAutomationAsync(int automationId, UpdateReviewEmailAutomationDto dto);

    /// <summary>
    /// Deletes an email automation.
    /// </summary>
    Task<bool> DeleteAutomationAsync(int automationId);

    /// <summary>
    /// Toggles automation active status.
    /// </summary>
    Task<bool> ToggleAutomationActiveAsync(int automationId);

    /// <summary>
    /// Duplicates an automation.
    /// </summary>
    Task<ReviewEmailAutomationDto?> DuplicateAutomationAsync(int automationId);

    #endregion

    #region Email Logs

    /// <summary>
    /// Gets email logs with pagination.
    /// </summary>
    Task<PaginatedResult<ReviewEmailLogDto>> GetEmailLogsAsync(
        string shopDomain,
        int? automationId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets email log by ID.
    /// </summary>
    Task<ReviewEmailLogDto?> GetEmailLogByIdAsync(int logId);

    /// <summary>
    /// Cancels a scheduled email.
    /// </summary>
    Task<bool> CancelScheduledEmailAsync(int logId);

    /// <summary>
    /// Resends a failed email.
    /// </summary>
    Task<bool> ResendEmailAsync(int logId);

    #endregion

    #region Email Processing

    /// <summary>
    /// Schedules review request emails for an order (called when order status changes).
    /// </summary>
    Task ScheduleReviewRequestsAsync(int orderId, string triggerType);

    /// <summary>
    /// Processes scheduled emails (called by background service).
    /// </summary>
    Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a single review request email.
    /// </summary>
    Task<bool> SendReviewRequestEmailAsync(int logId);

    #endregion

    #region Tracking

    /// <summary>
    /// Records email open event.
    /// </summary>
    Task RecordEmailOpenAsync(string trackingToken);

    /// <summary>
    /// Records email click event.
    /// </summary>
    Task RecordEmailClickAsync(string trackingToken);

    /// <summary>
    /// Records review submission from email.
    /// </summary>
    Task RecordReviewSubmissionAsync(string trackingToken, int reviewId);

    /// <summary>
    /// Generates tracking pixel URL.
    /// </summary>
    string GenerateTrackingPixelUrl(string trackingToken);

    /// <summary>
    /// Generates review submission URL with tracking.
    /// </summary>
    string GenerateReviewSubmissionUrl(string trackingToken, long productId);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets email automation analytics.
    /// </summary>
    Task<EmailAutomationAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion
}

/// <summary>
/// DTO for email automation analytics.
/// </summary>
public class EmailAutomationAnalyticsDto
{
    public int TotalEmailsSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalReviewsCollected { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ClickRate { get; set; }
    public decimal ConversionRate { get; set; }
    public List<AutomationPerformanceDto> AutomationPerformance { get; set; } = new();
    public List<DailyEmailStatsDto> DailyStats { get; set; } = new();
}

/// <summary>
/// DTO for individual automation performance.
/// </summary>
public class AutomationPerformanceDto
{
    public int AutomationId { get; set; }
    public string AutomationName { get; set; } = string.Empty;
    public int Sent { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
    public int ReviewsCollected { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ClickRate { get; set; }
    public decimal ConversionRate { get; set; }
}

/// <summary>
/// DTO for daily email statistics.
/// </summary>
public class DailyEmailStatsDto
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
    public int ReviewsCollected { get; set; }
}
