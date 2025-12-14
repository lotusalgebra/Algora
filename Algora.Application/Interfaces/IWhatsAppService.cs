using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing WhatsApp communications.
/// </summary>
public interface IWhatsAppService
{
    // Templates
    Task<WhatsAppTemplateDto?> GetTemplateAsync(int templateId);
    Task<IEnumerable<WhatsAppTemplateDto>> GetTemplatesAsync(string shopDomain, string? status = null);
    Task<WhatsAppTemplateDto> CreateTemplateAsync(string shopDomain, CreateWhatsAppTemplateDto dto);
    Task<WhatsAppTemplateDto> UpdateTemplateAsync(int templateId, UpdateWhatsAppTemplateDto dto);
    Task<bool> DeleteTemplateAsync(int templateId);
    Task<bool> SubmitTemplateForApprovalAsync(int templateId);

    // Messages
    Task<WhatsAppMessageDto> SendTemplateMessageAsync(string shopDomain, SendWhatsAppTemplateMessageDto dto);
    Task<WhatsAppMessageDto> SendTextMessageAsync(string shopDomain, SendWhatsAppTextMessageDto dto);
    Task<WhatsAppMessageDto?> GetMessageAsync(int messageId);
    Task<PaginatedResult<WhatsAppMessageDto>> GetMessagesAsync(string shopDomain, int page = 1, int pageSize = 50);

    // Conversations
    Task<WhatsAppConversationDto?> GetConversationAsync(int conversationId);
    Task<WhatsAppConversationDto?> GetConversationByPhoneAsync(string shopDomain, string phoneNumber);
    Task<PaginatedResult<WhatsAppConversationDto>> GetConversationsAsync(string shopDomain, int page = 1, int pageSize = 20);
    Task<IEnumerable<WhatsAppMessageDto>> GetConversationMessagesAsync(int conversationId, int limit = 50);
    Task<bool> CloseConversationAsync(int conversationId);

    // Campaigns
    Task<WhatsAppCampaignDto?> GetCampaignAsync(int campaignId);
    Task<WhatsAppCampaignDto> CreateCampaignAsync(string shopDomain, CreateWhatsAppCampaignDto dto);
    Task<bool> SendCampaignAsync(int campaignId);

    // Webhooks
    Task HandleIncomingMessageAsync(string shopDomain, WhatsAppWebhookPayload payload);
    Task HandleStatusUpdateAsync(string shopDomain, WhatsAppStatusPayload payload);
}
