using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Returns;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing return requests and the return portal.
/// </summary>
public interface IReturnService
{
    // Return Requests

    /// <summary>
    /// Create a new return request.
    /// </summary>
    Task<ReturnRequestDto> CreateReturnRequestAsync(string shopDomain, CreateReturnRequestDto dto);

    /// <summary>
    /// Get a return request by ID.
    /// </summary>
    Task<ReturnRequestDto?> GetReturnRequestAsync(int returnRequestId);

    /// <summary>
    /// Get a return request by request number.
    /// </summary>
    Task<ReturnRequestDto?> GetReturnRequestByNumberAsync(string shopDomain, string requestNumber);

    /// <summary>
    /// Get paginated return requests.
    /// </summary>
    Task<PaginatedResult<ReturnRequestDto>> GetReturnRequestsAsync(
        string shopDomain,
        string? status = null,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get return summary for dashboard.
    /// </summary>
    Task<ReturnSummaryDto> GetReturnSummaryAsync(string shopDomain);

    // Status Management

    /// <summary>
    /// Approve a return request and generate label.
    /// </summary>
    Task<ReturnRequestDto> ApproveReturnAsync(int returnRequestId, string? note = null);

    /// <summary>
    /// Reject a return request.
    /// </summary>
    Task<ReturnRequestDto> RejectReturnAsync(int returnRequestId, string reason);

    /// <summary>
    /// Mark a return as shipped by the customer.
    /// </summary>
    Task<ReturnRequestDto> MarkAsShippedAsync(int returnRequestId, string? trackingNumber = null, string? carrier = null);

    /// <summary>
    /// Mark a return as received at the warehouse.
    /// </summary>
    Task<ReturnRequestDto> MarkAsReceivedAsync(int returnRequestId, List<ReturnItemConditionDto>? itemConditions = null);

    /// <summary>
    /// Process the refund for a return.
    /// </summary>
    Task<ReturnRequestDto> ProcessRefundAsync(int returnRequestId);

    /// <summary>
    /// Cancel a return request.
    /// </summary>
    Task<ReturnRequestDto> CancelReturnAsync(int returnRequestId, string reason);

    // Customer Portal

    /// <summary>
    /// Check if an order is eligible for return.
    /// </summary>
    Task<CustomerReturnEligibilityDto> CheckReturnEligibilityAsync(string shopDomain, int orderId);

    /// <summary>
    /// Check eligibility by order number and email.
    /// </summary>
    Task<CustomerReturnEligibilityDto?> CheckReturnEligibilityByOrderNumberAsync(
        string shopDomain, string orderNumber, string email);

    // Return Reasons

    /// <summary>
    /// Get active return reasons for customers.
    /// </summary>
    Task<List<ReturnReasonDto>> GetActiveReasonsAsync(string shopDomain);

    /// <summary>
    /// Get all return reasons for admin.
    /// </summary>
    Task<List<ReturnReasonDto>> GetAllReasonsAsync(string shopDomain);

    /// <summary>
    /// Create a new return reason.
    /// </summary>
    Task<ReturnReasonDto> CreateReasonAsync(string shopDomain, CreateReturnReasonDto dto);

    /// <summary>
    /// Update a return reason.
    /// </summary>
    Task<ReturnReasonDto> UpdateReasonAsync(int reasonId, CreateReturnReasonDto dto);

    /// <summary>
    /// Delete a return reason.
    /// </summary>
    Task DeleteReasonAsync(int reasonId);

    // Settings

    /// <summary>
    /// Get return settings for a shop.
    /// </summary>
    Task<ReturnSettingsDto> GetSettingsAsync(string shopDomain);

    /// <summary>
    /// Update return settings for a shop.
    /// </summary>
    Task<ReturnSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateReturnSettingsDto dto);

    // Analytics

    /// <summary>
    /// Get return analytics for a date range.
    /// </summary>
    Task<ReturnAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);

    // Auto-approval

    /// <summary>
    /// Evaluate if a return request should be auto-approved.
    /// </summary>
    Task<bool> EvaluateAutoApprovalAsync(int returnRequestId);

    /// <summary>
    /// Seed default return reasons for a new shop.
    /// </summary>
    Task SeedDefaultReasonsAsync(string shopDomain);
}

/// <summary>
/// DTO for item condition when marking return as received.
/// </summary>
public record ReturnItemConditionDto
{
    public int ReturnItemId { get; init; }
    public string Condition { get; init; } = string.Empty;
    public string? ConditionNote { get; init; }
    public bool Restock { get; init; } = true;
}
