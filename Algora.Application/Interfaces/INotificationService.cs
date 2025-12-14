using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Unified notification service for email, SMS, and WhatsApp.
/// </summary>
public interface INotificationService
{
    Task<NotificationDto> SendEmailAsync(string shopDomain, SendEmailNotificationDto dto);
    Task<NotificationDto> SendSmsAsync(string shopDomain, SendSmsNotificationDto dto);
    Task<NotificationDto> SendWhatsAppAsync(string shopDomain, SendWhatsAppNotificationDto dto);
    Task<NotificationDto?> GetNotificationAsync(int notificationId);
    Task<PaginatedResult<NotificationDto>> GetNotificationsAsync(string shopDomain, int page = 1, int pageSize = 50);

    // Order notifications
    Task SendOrderConfirmationAsync(string shopDomain, int orderId);
    Task SendShippingNotificationAsync(string shopDomain, int orderId, string trackingNumber, string? trackingUrl = null);
    Task SendDeliveryConfirmationAsync(string shopDomain, int orderId);
    Task SendInvoiceAsync(string shopDomain, int invoiceId);

    // Customer notifications
    Task SendWelcomeEmailAsync(string shopDomain, int customerId);
    Task SendPasswordResetAsync(string shopDomain, string email, string resetToken);
}