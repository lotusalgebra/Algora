using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing per-shop communication settings stored in the database.
/// </summary>
public interface ICommunicationSettingsService
{
    Task<CommunicationSettingsDto?> GetSettingsAsync(string shopDomain);
    Task<CommunicationSettingsDto> GetOrCreateSettingsAsync(string shopDomain);
    Task<CommunicationSettingsDto> SaveSettingsAsync(string shopDomain, UpdateCommunicationSettingsDto dto);

    // Get typed settings for services
    Task<EmailSettingsDto?> GetEmailSettingsAsync(string shopDomain);
    Task<SmsSettingsDto?> GetSmsSettingsAsync(string shopDomain);
    Task<WhatsAppSettingsDto?> GetWhatsAppSettingsAsync(string shopDomain);

    // Connection testing
    Task<bool> TestEmailConnectionAsync(string shopDomain);
    Task<bool> TestSmsConnectionAsync(string shopDomain);
    Task<bool> TestWhatsAppConnectionAsync(string shopDomain);
}