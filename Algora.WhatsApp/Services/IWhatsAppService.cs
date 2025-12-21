using Algora.WhatsApp.DTOs;

namespace Algora.WhatsApp.Services;

/// <summary>
/// Complete WhatsApp Business API service interface for Facebook integration.
/// </summary>
public interface IWhatsAppService
{
    #region Templates

    /// <summary>
    /// Get a template by ID.
    /// </summary>
    Task<WhatsAppTemplateDto?> GetTemplateAsync(int templateId);

    /// <summary>
    /// Get all templates for a shop.
    /// </summary>
    Task<IEnumerable<WhatsAppTemplateDto>> GetTemplatesAsync(string shopDomain, string? status = null);

    /// <summary>
    /// Create a new template (locally, not submitted to Meta yet).
    /// </summary>
    Task<WhatsAppTemplateDto> CreateTemplateAsync(string shopDomain, CreateWhatsAppTemplateDto dto);

    /// <summary>
    /// Update an existing template.
    /// </summary>
    Task<WhatsAppTemplateDto> UpdateTemplateAsync(int templateId, UpdateWhatsAppTemplateDto dto);

    /// <summary>
    /// Delete a template.
    /// </summary>
    Task<bool> DeleteTemplateAsync(int templateId);

    /// <summary>
    /// Submit a template to Meta for approval.
    /// </summary>
    Task<bool> SubmitTemplateForApprovalAsync(int templateId);

    /// <summary>
    /// Sync templates from Meta to local database.
    /// </summary>
    Task<int> SyncTemplatesFromMetaAsync(string shopDomain);

    #endregion

    #region Messages

    /// <summary>
    /// Send a template message.
    /// </summary>
    Task<WhatsAppMessageDto> SendTemplateMessageAsync(string shopDomain, SendWhatsAppTemplateMessageDto dto);

    /// <summary>
    /// Send a text message (only within 24-hour window).
    /// </summary>
    Task<WhatsAppMessageDto> SendTextMessageAsync(string shopDomain, SendWhatsAppTextMessageDto dto);

    /// <summary>
    /// Send a media message (image, video, audio, document).
    /// </summary>
    Task<WhatsAppMessageDto> SendMediaMessageAsync(string shopDomain, SendWhatsAppMediaMessageDto dto);

    /// <summary>
    /// Send an interactive message with buttons or list.
    /// </summary>
    Task<WhatsAppMessageDto> SendInteractiveMessageAsync(string shopDomain, SendWhatsAppInteractiveMessageDto dto);

    /// <summary>
    /// Get a message by ID.
    /// </summary>
    Task<WhatsAppMessageDto?> GetMessageAsync(int messageId);

    /// <summary>
    /// Get paginated messages for a shop.
    /// </summary>
    Task<PaginatedResult<WhatsAppMessageDto>> GetMessagesAsync(string shopDomain, int page = 1, int pageSize = 50);

    /// <summary>
    /// Mark a message as read.
    /// </summary>
    Task<bool> MarkMessageAsReadAsync(string externalMessageId);

    #endregion

    #region Conversations

    /// <summary>
    /// Get a conversation by ID.
    /// </summary>
    Task<WhatsAppConversationDto?> GetConversationAsync(int conversationId);

    /// <summary>
    /// Get a conversation by phone number.
    /// </summary>
    Task<WhatsAppConversationDto?> GetConversationByPhoneAsync(string shopDomain, string phoneNumber);

    /// <summary>
    /// Get paginated conversations for a shop.
    /// </summary>
    Task<PaginatedResult<WhatsAppConversationDto>> GetConversationsAsync(string shopDomain, string? status = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get messages in a conversation.
    /// </summary>
    Task<IEnumerable<WhatsAppMessageDto>> GetConversationMessagesAsync(int conversationId, int limit = 50);

    /// <summary>
    /// Update a conversation (assign, change status).
    /// </summary>
    Task<WhatsAppConversationDto> UpdateConversationAsync(int conversationId, UpdateConversationDto dto);

    /// <summary>
    /// Close a conversation.
    /// </summary>
    Task<bool> CloseConversationAsync(int conversationId);

    /// <summary>
    /// Check if the 24-hour messaging window is open for a conversation.
    /// </summary>
    Task<bool> IsWindowOpenAsync(int conversationId);

    #endregion

    #region Campaigns

    /// <summary>
    /// Get a campaign by ID.
    /// </summary>
    Task<WhatsAppCampaignDto?> GetCampaignAsync(int campaignId);

    /// <summary>
    /// Get all campaigns for a shop.
    /// </summary>
    Task<IEnumerable<WhatsAppCampaignDto>> GetCampaignsAsync(string shopDomain, string? status = null);

    /// <summary>
    /// Create a new campaign.
    /// </summary>
    Task<WhatsAppCampaignDto> CreateCampaignAsync(string shopDomain, CreateWhatsAppCampaignDto dto);

    /// <summary>
    /// Update a campaign.
    /// </summary>
    Task<WhatsAppCampaignDto> UpdateCampaignAsync(int campaignId, UpdateWhatsAppCampaignDto dto);

    /// <summary>
    /// Delete a campaign.
    /// </summary>
    Task<bool> DeleteCampaignAsync(int campaignId);

    /// <summary>
    /// Send/execute a campaign.
    /// </summary>
    Task<bool> SendCampaignAsync(int campaignId);

    /// <summary>
    /// Pause a running campaign.
    /// </summary>
    Task<bool> PauseCampaignAsync(int campaignId);

    /// <summary>
    /// Resume a paused campaign.
    /// </summary>
    Task<bool> ResumeCampaignAsync(int campaignId);

    #endregion

    #region Webhooks

    /// <summary>
    /// Verify webhook subscription from Meta.
    /// </summary>
    bool VerifyWebhook(string mode, string token, string challenge, out string response);

    /// <summary>
    /// Process incoming webhook payload from Meta.
    /// </summary>
    Task ProcessWebhookAsync(string shopDomain, WhatsAppWebhookPayload payload);

    /// <summary>
    /// Verify webhook signature.
    /// </summary>
    bool VerifySignature(string payload, string signature);

    #endregion
}

/// <summary>
/// Paginated result wrapper.
/// </summary>
public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        return new PaginatedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
