using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing portal return requests from the admin application
/// </summary>
public interface IPortalReturnAdminService
{
    /// <summary>
    /// Gets paginated list of portal return requests for a shop
    /// </summary>
    Task<PortalReturnPaginatedResult> GetReturnRequestsAsync(
        string shopDomain,
        string? status = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 25);

    /// <summary>
    /// Gets a specific return request by ID
    /// </summary>
    Task<PortalReturnDetailDto?> GetReturnRequestByIdAsync(string shopDomain, int requestId);

    /// <summary>
    /// Updates the status and details of a return request
    /// </summary>
    Task<bool> UpdateReturnRequestAsync(string shopDomain, int requestId, UpdatePortalReturnDto dto);

    /// <summary>
    /// Approves a return request
    /// </summary>
    Task<bool> ApproveReturnRequestAsync(string shopDomain, int requestId, string? adminNotes = null, string? returnLabelUrl = null);

    /// <summary>
    /// Rejects a return request
    /// </summary>
    Task<bool> RejectReturnRequestAsync(string shopDomain, int requestId, string reason);

    /// <summary>
    /// Marks a return request as completed with refund amount
    /// </summary>
    Task<bool> CompleteReturnRequestAsync(string shopDomain, int requestId, decimal refundAmount, string? adminNotes = null);

    /// <summary>
    /// Gets return request statistics for the dashboard
    /// </summary>
    Task<PortalReturnStatsDto> GetReturnStatsAsync(string shopDomain);
}

/// <summary>
/// DTO for portal return statistics
/// </summary>
public record PortalReturnStatsDto(
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int ProcessingRequests,
    int CompletedRequests,
    int RejectedRequests,
    decimal TotalRefundedAmount,
    decimal PendingRefundValue
);
