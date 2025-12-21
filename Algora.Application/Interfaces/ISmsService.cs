using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing SMS communications.
/// </summary>
public interface ISmsService
{
    Task<SmsTemplateDto?> GetTemplateAsync(int templateId);
    Task<IEnumerable<SmsTemplateDto>> GetTemplatesAsync(string shopDomain);
    Task<SmsTemplateDto> CreateTemplateAsync(string shopDomain, CreateSmsTemplateDto dto);
    Task<SmsTemplateDto> UpdateTemplateAsync(int templateId, UpdateSmsTemplateDto dto);
    Task<bool> DeleteTemplateAsync(int templateId);

    Task<SmsMessageDto> SendMessageAsync(string shopDomain, SendSmsMessageDto dto);
    Task<SmsMessageDto?> GetMessageAsync(int messageId);
    Task<PaginatedResult<SmsMessageDto>> GetMessagesAsync(string shopDomain, int page = 1, int pageSize = 50);
    Task<int> SendBulkMessagesAsync(string shopDomain, SendBulkSmsDto dto);

    Task HandleDeliveryStatusAsync(string shopDomain, SmsDeliveryStatusPayload payload);
}