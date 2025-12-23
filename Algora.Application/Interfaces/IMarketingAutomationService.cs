using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Core service for marketing automation orchestration.
/// Handles triggers, enrollments, step execution, and analytics.
/// </summary>
public interface IMarketingAutomationService
{
    // ==================== TRIGGER PROCESSING ====================

    /// <summary>
    /// Process an abandoned cart trigger - enrolls customer in abandoned cart automation.
    /// </summary>
    Task ProcessAbandonedCartTriggerAsync(string shopDomain, AbandonedCartTriggerDto trigger);

    /// <summary>
    /// Process a post-purchase trigger - enrolls customer in post-purchase automation.
    /// Also exits any active abandoned cart enrollments.
    /// </summary>
    Task ProcessPostPurchaseTriggerAsync(string shopDomain, PostPurchaseTriggerDto trigger);

    /// <summary>
    /// Process a welcome trigger - enrolls new customer in welcome automation.
    /// </summary>
    Task ProcessWelcomeTriggerAsync(string shopDomain, WelcomeTriggerDto trigger);

    /// <summary>
    /// Process win-back triggers - finds inactive customers and enrolls them.
    /// Typically called by a scheduled background job.
    /// </summary>
    Task<int> ProcessWinbackTriggersAsync(string shopDomain, CancellationToken cancellationToken = default);

    // ==================== ENROLLMENT MANAGEMENT ====================

    /// <summary>
    /// Enroll a customer/subscriber in an automation.
    /// </summary>
    Task<int?> EnrollInAutomationAsync(int automationId, EnrollmentContext context);

    /// <summary>
    /// Exit a customer from an automation with a reason.
    /// </summary>
    Task<bool> ExitAutomationAsync(int enrollmentId, string reason);

    /// <summary>
    /// Get enrollment details.
    /// </summary>
    Task<AutomationEnrollmentDto?> GetEnrollmentAsync(int enrollmentId);

    /// <summary>
    /// Get all enrollments for an automation with pagination.
    /// </summary>
    Task<(List<AutomationEnrollmentDto> Enrollments, int TotalCount)> GetEnrollmentsAsync(
        int automationId,
        string? statusFilter = null,
        int page = 1,
        int pageSize = 20);

    // ==================== STEP EXECUTION ====================

    /// <summary>
    /// Process all pending steps that are due for execution.
    /// Returns the number of steps processed.
    /// </summary>
    Task<int> ProcessPendingStepsAsync(string shopDomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a specific step for an enrollment.
    /// </summary>
    Task<bool> ExecuteStepAsync(int enrollmentId, int stepId);

    /// <summary>
    /// Advance an enrollment to the next step.
    /// </summary>
    Task<bool> AdvanceToNextStepAsync(int enrollmentId);

    // ==================== ANALYTICS ====================

    /// <summary>
    /// Get comprehensive analytics for an automation.
    /// </summary>
    Task<AutomationAnalyticsDto?> GetAutomationAnalyticsAsync(int automationId);

    /// <summary>
    /// Get step-by-step analytics for an automation.
    /// </summary>
    Task<List<StepAnalyticsDto>> GetStepAnalyticsAsync(int automationId);

    // ==================== WIN-BACK RULES ====================

    /// <summary>
    /// Get all win-back rules for a shop.
    /// </summary>
    Task<List<WinbackRuleDto>> GetWinbackRulesAsync(string shopDomain);

    /// <summary>
    /// Create a new win-back rule.
    /// </summary>
    Task<WinbackRuleDto?> CreateWinbackRuleAsync(string shopDomain, CreateWinbackRuleDto dto);

    /// <summary>
    /// Update a win-back rule.
    /// </summary>
    Task<bool> UpdateWinbackRuleAsync(int ruleId, CreateWinbackRuleDto dto);

    /// <summary>
    /// Delete a win-back rule.
    /// </summary>
    Task<bool> DeleteWinbackRuleAsync(int ruleId);

    /// <summary>
    /// Detect inactive customers based on a win-back rule.
    /// </summary>
    Task<List<InactiveCustomerDto>> DetectInactiveCustomersAsync(string shopDomain, WinbackRuleDto rule);

    // ==================== WEBHOOK EVENT TRACKING ====================

    /// <summary>
    /// Track email opened event.
    /// </summary>
    Task TrackEmailOpenedAsync(int stepLogId);

    /// <summary>
    /// Track email clicked event.
    /// </summary>
    Task TrackEmailClickedAsync(int stepLogId);

    /// <summary>
    /// Track email delivered event.
    /// </summary>
    Task TrackEmailDeliveredAsync(string externalMessageId);

    /// <summary>
    /// Track email bounced event.
    /// </summary>
    Task TrackEmailBouncedAsync(string externalMessageId);

    /// <summary>
    /// Track conversion event (order placed after automation email).
    /// </summary>
    Task TrackConversionAsync(int enrollmentId, decimal conversionValue);
}
