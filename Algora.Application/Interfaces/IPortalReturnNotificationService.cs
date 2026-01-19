using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for sending email notifications related to portal return requests
/// </summary>
public interface IPortalReturnNotificationService
{
    /// <summary>
    /// Sends notification when a return request is approved
    /// </summary>
    Task SendReturnApprovedNotificationAsync(string shopDomain, PortalReturnDetailDto returnRequest, string? returnLabelUrl = null);

    /// <summary>
    /// Sends notification when a return request is rejected
    /// </summary>
    Task SendReturnRejectedNotificationAsync(string shopDomain, PortalReturnDetailDto returnRequest, string rejectionReason);

    /// <summary>
    /// Sends notification when a return is completed and refund issued
    /// </summary>
    Task SendReturnCompletedNotificationAsync(string shopDomain, PortalReturnDetailDto returnRequest, decimal refundAmount);

    /// <summary>
    /// Sends notification when return tracking is received
    /// </summary>
    Task SendReturnReceivedNotificationAsync(string shopDomain, PortalReturnDetailDto returnRequest);

    /// <summary>
    /// Sends confirmation when a return request is submitted
    /// </summary>
    Task SendReturnSubmittedNotificationAsync(string shopDomain, PortalReturnDetailDto returnRequest);
}
