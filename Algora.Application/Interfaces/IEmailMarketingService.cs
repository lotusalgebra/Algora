using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing email marketing operations including subscribers, lists, campaigns, and automations.
/// </summary>
public interface IEmailMarketingService
{
    // ===== Subscriber Management =====
    Task<EmailSubscriberDto?> GetSubscriberAsync(string shopDomain, string email);
    Task<EmailSubscriberDto?> GetSubscriberByIdAsync(int subscriberId);
    Task<PaginatedResult<EmailSubscriberDto>> GetSubscribersAsync(string shopDomain, int page = 1, int pageSize = 50, string? status = null);
    Task<EmailSubscriberDto> CreateSubscriberAsync(string shopDomain, CreateEmailSubscriberDto dto);
    Task<EmailSubscriberDto> UpdateSubscriberAsync(int subscriberId, UpdateEmailSubscriberDto dto);
    Task<bool> UnsubscribeAsync(string shopDomain, string email, string? reason = null);
    Task<int> ImportSubscribersAsync(string shopDomain, IEnumerable<CreateEmailSubscriberDto> subscribers, int? listId = null);

    // ===== List Management =====
    Task<EmailListDto?> GetListAsync(int listId);
    Task<IEnumerable<EmailListDto>> GetListsAsync(string shopDomain);
    Task<EmailListDto> CreateListAsync(string shopDomain, CreateEmailListDto dto);
    Task<EmailListDto> UpdateListAsync(int listId, UpdateEmailListDto dto);
    Task<bool> DeleteListAsync(int listId);
    Task<bool> AddSubscriberToListAsync(int listId, int subscriberId);
    Task<bool> RemoveSubscriberFromListAsync(int listId, int subscriberId);

    // ===== Segment Management =====
    Task<CustomerSegmentDto?> GetSegmentAsync(int segmentId);
    Task<IEnumerable<CustomerSegmentDto>> GetSegmentsAsync(string shopDomain);
    Task<CustomerSegmentDto> CreateSegmentAsync(string shopDomain, CreateCustomerSegmentDto dto);
    Task<CustomerSegmentDto> UpdateSegmentAsync(int segmentId, UpdateCustomerSegmentDto dto);
    Task<bool> DeleteSegmentAsync(int segmentId);
    Task<int> RecalculateSegmentAsync(int segmentId);

    // ===== Campaign Management =====
    Task<EmailCampaignDto?> GetCampaignAsync(int campaignId);
    Task<PaginatedResult<EmailCampaignDto>> GetCampaignsAsync(string shopDomain, int page = 1, int pageSize = 20, string? status = null);
    Task<EmailCampaignDto> CreateCampaignAsync(string shopDomain, CreateEmailCampaignDto dto);
    Task<EmailCampaignDto> UpdateCampaignAsync(int campaignId, UpdateEmailCampaignDto dto);
    Task<bool> DeleteCampaignAsync(int campaignId);
    Task<bool> ScheduleCampaignAsync(int campaignId, DateTime scheduledAt);
    Task<bool> SendCampaignAsync(int campaignId);
    Task<bool> PauseCampaignAsync(int campaignId);
    Task<bool> CancelCampaignAsync(int campaignId);
    Task<EmailCampaignStatsDto> GetCampaignStatsAsync(int campaignId);

    // ===== Automation Management =====
    Task<EmailAutomationDto?> GetAutomationAsync(int automationId);
    Task<IEnumerable<EmailAutomationDto>> GetAutomationsAsync(string shopDomain);
    Task<EmailAutomationDto> CreateAutomationAsync(string shopDomain, CreateEmailAutomationDto dto);
    Task<EmailAutomationDto> UpdateAutomationAsync(int automationId, UpdateEmailAutomationDto dto);
    Task<bool> DeleteAutomationAsync(int automationId);
    Task<bool> ActivateAutomationAsync(int automationId);
    Task<bool> DeactivateAutomationAsync(int automationId);
    Task<bool> EnrollInAutomationAsync(int automationId, string email, int? customerId = null);

    // ===== Template Management =====
    Task<EmailTemplateDto?> GetTemplateAsync(int templateId);
    Task<IEnumerable<EmailTemplateDto>> GetTemplatesAsync(string shopDomain);
    Task<EmailTemplateDto> CreateTemplateAsync(string shopDomain, CreateEmailTemplateDto dto);
    Task<EmailTemplateDto> UpdateTemplateAsync(int templateId, UpdateEmailTemplateDto dto);
    Task<bool> DeleteTemplateAsync(int templateId);
    Task<bool> ActivateTemplateAsync(int templateId);
    Task<bool> DeactivateTemplateAsync(int templateId);
    Task<EmailTemplateDto?> DuplicateTemplateAsync(int templateId);
}
