using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Configuration options for WhatsApp Business API.
/// </summary>
public class WhatsAppOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v20.0";
}

/// <summary>
/// Complete WhatsApp Business API service implementation.
/// </summary>
public partial class WhatsAppService : IWhatsAppService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public WhatsAppService(
        AppDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppService> logger)
    {
        _db = db;
        _http = httpFactory.CreateClient();
        _options = options.Value;
        _logger = logger;
        _baseUrl = $"https://graph.facebook.com/{_options.ApiVersion}";

        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }
    }

    #region Templates

    public async Task<WhatsAppTemplateDto?> GetTemplateAsync(int templateId)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(templateId);
        return template is null ? null : MapToDto(template);
    }

    public async Task<IEnumerable<WhatsAppTemplateDto>> GetTemplatesAsync(string shopDomain, string? status = null)
    {
        var query = _db.WhatsAppTemplates.AsNoTracking().Where(t => t.ShopDomain == shopDomain);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query.OrderBy(t => t.Name).Select(t => MapToDto(t)).ToListAsync();
    }

    public async Task<WhatsAppTemplateDto> CreateTemplateAsync(string shopDomain, CreateWhatsAppTemplateDto dto)
    {
        var template = new WhatsAppTemplate
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            Language = dto.Language,
            Category = dto.Category,
            HeaderType = dto.HeaderType,
            HeaderContent = dto.HeaderContent,
            Body = dto.Body,
            Footer = dto.Footer,
            Buttons = dto.Buttons,
            Status = "pending",
            IsActive = false
        };

        _db.WhatsAppTemplates.Add(template);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created WhatsApp template {Name} for {Shop}", dto.Name, shopDomain);
        return MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto> UpdateTemplateAsync(int templateId, UpdateWhatsAppTemplateDto dto)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        if (dto.Body is not null) template.Body = dto.Body;
        if (dto.Footer is not null) template.Footer = dto.Footer;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(templateId);
        if (template is null) return false;

        _db.WhatsAppTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitTemplateForApprovalAsync(int templateId)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(templateId)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        try
        {
            var url = $"{_baseUrl}/{_options.BusinessAccountId}/message_templates";

            var components = new List<object>();

            // Header component
            if (!string.IsNullOrEmpty(template.HeaderType))
            {
                components.Add(new
                {
                    type = "HEADER",
                    format = template.HeaderType.ToUpperInvariant(),
                    text = template.HeaderContent
                });
            }

            // Body component
            components.Add(new
            {
                type = "BODY",
                text = template.Body
            });

            // Footer component
            if (!string.IsNullOrEmpty(template.Footer))
            {
                components.Add(new { type = "FOOTER", text = template.Footer });
            }

            var payload = new
            {
                name = template.Name.ToLowerInvariant().Replace(" ", "_"),
                language = template.Language,
                category = template.Category,
                components
            };

            var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(result);
                template.ExternalTemplateId = json.RootElement.GetProperty("id").GetString();
                template.Status = "submitted";
                template.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Submitted template {Id} for approval", templateId);
                return true;
            }

            _logger.LogWarning("Failed to submit template {Id}: {Result}", templateId, result);
            template.Status = "rejected";
            template.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting template {Id} for approval", templateId);
            return false;
        }
    }

    #endregion

    #region Messages

    public async Task<WhatsAppMessageDto> SendTemplateMessageAsync(string shopDomain, SendWhatsAppTemplateMessageDto dto)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(dto.TemplateId)
            ?? throw new InvalidOperationException($"Template {dto.TemplateId} not found");

        if (!template.IsActive || template.Status != "approved")
            throw new InvalidOperationException("Template is not approved or active");

        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);

        // Build template components with variables
        var components = new List<object>();
        if (dto.Variables?.Count > 0)
        {
            var parameters = dto.Variables.Select(v => new { type = "text", text = v.Value }).ToList();
            components.Add(new { type = "body", parameters });
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "template",
            template = new
            {
                name = template.Name.ToLowerInvariant().Replace(" ", "_"),
                language = new { code = template.Language },
                components = components.Count > 0 ? components : null
            }
        };

        var message = new WhatsAppMessage
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            PhoneNumber = phoneNumber,
            Direction = "outbound",
            MessageType = "template",
            TemplateId = dto.TemplateId,
            Content = template.Body,
            Status = "pending"
        };

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync();

        try
        {
            var url = $"{_baseUrl}/{_options.PhoneNumberId}/messages";
            var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(result);
                message.ExternalMessageId = json.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
                message.Status = "sent";
                message.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Sent WhatsApp template message to {Phone}", phoneNumber);
            }
            else
            {
                message.Status = "failed";
                message.ErrorMessage = result;
                _logger.LogWarning("Failed to send WhatsApp message to {Phone}: {Error}", phoneNumber, result);
            }
        }
        catch (Exception ex)
        {
            message.Status = "failed";
            message.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phoneNumber);
        }

        await _db.SaveChangesAsync();
        await UpdateConversationAsync(shopDomain, phoneNumber, message);

        return MapToDto(message);
    }

    public async Task<WhatsAppMessageDto> SendTextMessageAsync(string shopDomain, SendWhatsAppTextMessageDto dto)
    {
        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new { body = dto.Content }
        };

        var message = new WhatsAppMessage
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            PhoneNumber = phoneNumber,
            Direction = "outbound",
            MessageType = "text",
            Content = dto.Content,
            Status = "pending"
        };

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync();

        try
        {
            var url = $"{_baseUrl}/{_options.PhoneNumberId}/messages";
            var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(result);
                message.ExternalMessageId = json.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
                message.Status = "sent";
                message.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Sent WhatsApp text message to {Phone}", phoneNumber);
            }
            else
            {
                message.Status = "failed";
                message.ErrorMessage = result;
                _logger.LogWarning("Failed to send WhatsApp message to {Phone}: {Error}", phoneNumber, result);
            }
        }
        catch (Exception ex)
        {
            message.Status = "failed";
            message.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phoneNumber);
        }

        await _db.SaveChangesAsync();
        await UpdateConversationAsync(shopDomain, phoneNumber, message);

        return MapToDto(message);
    }

    public async Task<WhatsAppMessageDto?> GetMessageAsync(int messageId)
    {
        var message = await _db.WhatsAppMessages.FindAsync(messageId);
        return message is null ? null : MapToDto(message);
    }

    public async Task<PaginatedResult<WhatsAppMessageDto>> GetMessagesAsync(string shopDomain, int page = 1, int pageSize = 50)
    {
        var query = _db.WhatsAppMessages.AsNoTracking().Where(m => m.ShopDomain == shopDomain);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResult<WhatsAppMessageDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    #endregion

    #region Conversations

    public async Task<WhatsAppConversationDto?> GetConversationAsync(int conversationId)
    {
        var conversation = await _db.WhatsAppConversations.FindAsync(conversationId);
        return conversation is null ? null : MapToDto(conversation);
    }

    public async Task<WhatsAppConversationDto?> GetConversationByPhoneAsync(string shopDomain, string phoneNumber)
    {
        var normalized = NormalizePhoneNumber(phoneNumber);
        var conversation = await _db.WhatsAppConversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.PhoneNumber == normalized);
        return conversation is null ? null : MapToDto(conversation);
    }

    public async Task<PaginatedResult<WhatsAppConversationDto>> GetConversationsAsync(string shopDomain, int page = 1, int pageSize = 20)
    {
        var query = _db.WhatsAppConversations.AsNoTracking().Where(c => c.ShopDomain == shopDomain);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResult<WhatsAppConversationDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<IEnumerable<WhatsAppMessageDto>> GetConversationMessagesAsync(int conversationId, int limit = 50)
    {
        var conversation = await _db.WhatsAppConversations.FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found");

        var messages = await _db.WhatsAppMessages.AsNoTracking()
            .Where(m => m.ShopDomain == conversation.ShopDomain && m.PhoneNumber == conversation.PhoneNumber)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages.Select(MapToDto).Reverse();
    }

    public async Task<bool> CloseConversationAsync(int conversationId)
    {
        var conversation = await _db.WhatsAppConversations.FindAsync(conversationId);
        if (conversation is null) return false;

        conversation.Status = "closed";
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task UpdateConversationAsync(string shopDomain, string phoneNumber, WhatsAppMessage message)
    {
        var conversation = await _db.WhatsAppConversations
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.PhoneNumber == phoneNumber);

        if (conversation is null)
        {
            conversation = new WhatsAppConversation
            {
                ShopDomain = shopDomain,
                PhoneNumber = phoneNumber,
                CustomerId = message.CustomerId,
                Status = "open",
                IsBusinessInitiated = message.Direction == "outbound"
            };
            _db.WhatsAppConversations.Add(conversation);
        }

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.LastMessagePreview = message.Content?.Length > 100
            ? message.Content[..100] + "..."
            : message.Content;

        if (message.Direction == "inbound")
        {
            conversation.UnreadCount++;
            conversation.WindowExpiresAt = DateTime.UtcNow.AddHours(24);
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    #endregion

    #region Campaigns

    public async Task<WhatsAppCampaignDto?> GetCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns.Include(c => c.Template).FirstOrDefaultAsync(c => c.Id == campaignId);
        return campaign is null ? null : MapToDto(campaign);
    }

    public async Task<WhatsAppCampaignDto> CreateCampaignAsync(string shopDomain, CreateWhatsAppCampaignDto dto)
    {
        var template = await _db.WhatsAppTemplates.FindAsync(dto.TemplateId)
            ?? throw new InvalidOperationException($"Template {dto.TemplateId} not found");

        var campaign = new WhatsAppCampaign
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            TemplateId = dto.TemplateId,
            SegmentId = dto.SegmentId,
            Status = "draft"
        };

        _db.WhatsAppCampaigns.Add(campaign);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created WhatsApp campaign {Name} for {Shop}", dto.Name, shopDomain);
        return MapToDto(campaign, template);
    }

    public async Task<bool> SendCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns
            .Include(c => c.Template)
            .Include(c => c.Segment)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        if (campaign.Template is null || !campaign.Template.IsActive)
            throw new InvalidOperationException("Campaign template is not active");

        campaign.Status = "sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Get recipients from segment or all subscribers with WhatsApp opt-in
        var recipients = campaign.SegmentId.HasValue
            ? await GetSegmentRecipientsAsync(campaign.ShopDomain, campaign.SegmentId.Value)
            : await GetAllWhatsAppSubscribersAsync(campaign.ShopDomain);

        campaign.TotalRecipients = recipients.Count;
        var sent = 0;
        var delivered = 0;

        foreach (var recipient in recipients)
        {
            try
            {
                var message = await SendTemplateMessageAsync(campaign.ShopDomain, new SendWhatsAppTemplateMessageDto
                {
                    PhoneNumber = recipient.Phone!,
                    TemplateId = campaign.TemplateId,
                    CustomerId = recipient.CustomerId
                });

                if (message.Status == "sent") sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send campaign message to {Phone}", recipient.Phone);
            }

            // Rate limiting - WhatsApp has limits on messages per second
            await Task.Delay(100);
        }

        campaign.TotalSent = sent;
        campaign.TotalDelivered = delivered;
        campaign.Status = "sent";
        campaign.SentAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Completed WhatsApp campaign {Id}: {Sent}/{Total} sent", campaignId, sent, recipients.Count);
        return true;
    }

    private async Task<List<(string? Phone, int? CustomerId)>> GetSegmentRecipientsAsync(string shopDomain, int segmentId)
    {
        var members = await _db.CustomerSegmentMembers
            .Where(m => m.SegmentId == segmentId)
            .Select(m => new { m.CustomerId, m.SubscriberId })
            .ToListAsync();

        var recipients = new List<(string? Phone, int? CustomerId)>();

        foreach (var member in members)
        {
            if (member.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(member.CustomerId.Value);
                if (customer?.Phone is not null)
                    recipients.Add((customer.Phone, customer.Id));
            }
            else if (member.SubscriberId.HasValue)
            {
                var subscriber = await _db.EmailSubscribers.FindAsync(member.SubscriberId.Value);
                if (subscriber?.Phone is not null && subscriber.WhatsAppOptIn)
                    recipients.Add((subscriber.Phone, subscriber.CustomerId));
            }
        }

        return recipients;
    }

    private async Task<List<(string? Phone, int? CustomerId)>> GetAllWhatsAppSubscribersAsync(string shopDomain)
    {
        return await _db.EmailSubscribers
            .Where(s => s.ShopDomain == shopDomain && s.WhatsAppOptIn && s.Phone != null)
            .Select(s => new ValueTuple<string?, int?>(s.Phone, s.CustomerId))
            .ToListAsync();
    }

    #endregion

    #region Webhooks

    public async Task HandleIncomingMessageAsync(string shopDomain, WhatsAppWebhookPayload payload)
    {
        var phoneNumber = NormalizePhoneNumber(payload.From);

        var message = new WhatsAppMessage
        {
            ShopDomain = shopDomain,
            ExternalMessageId = payload.MessageId,
            PhoneNumber = phoneNumber,
            Direction = "inbound",
            MessageType = payload.Type ?? "text",
            Content = payload.Text,
            Status = "delivered",
            SentAt = payload.Timestamp,
            DeliveredAt = DateTime.UtcNow
        };

        // Try to find associated customer
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Phone == phoneNumber);
        if (customer is not null)
            message.CustomerId = customer.Id;

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync();

        await UpdateConversationAsync(shopDomain, phoneNumber, message);

        _logger.LogInformation("Received WhatsApp message from {Phone}", phoneNumber);
    }

    public async Task HandleStatusUpdateAsync(string shopDomain, WhatsAppStatusPayload payload)
    {
        var message = await _db.WhatsAppMessages
            .FirstOrDefaultAsync(m => m.ExternalMessageId == payload.MessageId);

        if (message is null)
        {
            _logger.LogWarning("Message not found for status update: {MessageId}", payload.MessageId);
            return;
        }

        message.Status = payload.Status.ToLowerInvariant();

        switch (payload.Status.ToLowerInvariant())
        {
            case "delivered":
                message.DeliveredAt = payload.Timestamp;
                break;
            case "read":
                message.ReadAt = payload.Timestamp;
                break;
            case "failed":
                message.ErrorCode = payload.ErrorCode;
                break;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated message {Id} status to {Status}", message.Id, payload.Status);
    }

    #endregion

    #region Helpers

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters except leading +
        var normalized = PhoneRegex().Replace(phone, "");
        // Ensure it starts with country code
        if (!normalized.StartsWith('+') && !normalized.StartsWith('0'))
            normalized = "+" + normalized;
        return normalized.TrimStart('0');
    }

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex PhoneRegex();

    #endregion

    #region Mappers

    private static WhatsAppTemplateDto MapToDto(WhatsAppTemplate t) => new()
    {
        Id = t.Id,
        ShopDomain = t.ShopDomain,
        Name = t.Name,
        ExternalTemplateId = t.ExternalTemplateId,
        Language = t.Language,
        Category = t.Category,
        Body = t.Body,
        Footer = t.Footer,
        Status = t.Status,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };

    private static WhatsAppMessageDto MapToDto(WhatsAppMessage m) => new()
    {
        Id = m.Id,
        ShopDomain = m.ShopDomain,
        ExternalMessageId = m.ExternalMessageId,
        CustomerId = m.CustomerId,
        PhoneNumber = m.PhoneNumber,
        Direction = m.Direction,
        MessageType = m.MessageType,
        Content = m.Content,
        Status = m.Status,
        SentAt = m.SentAt,
        DeliveredAt = m.DeliveredAt,
        ReadAt = m.ReadAt,
        CreatedAt = m.CreatedAt
    };

    private static WhatsAppConversationDto MapToDto(WhatsAppConversation c) => new()
    {
        Id = c.Id,
        ShopDomain = c.ShopDomain,
        CustomerId = c.CustomerId,
        PhoneNumber = c.PhoneNumber,
        CustomerName = c.CustomerName,
        Status = c.Status,
        AssignedTo = c.AssignedTo,
        LastMessageAt = c.LastMessageAt,
        LastMessagePreview = c.LastMessagePreview,
        UnreadCount = c.UnreadCount,
        CreatedAt = c.CreatedAt
    };

    private static WhatsAppCampaignDto MapToDto(WhatsAppCampaign c, WhatsAppTemplate? template = null) => new()
    {
        Id = c.Id,
        ShopDomain = c.ShopDomain,
        Name = c.Name,
        TemplateId = c.TemplateId,
        TemplateName = template?.Name ?? c.Template?.Name,
        SegmentId = c.SegmentId,
        Status = c.Status,
        SentAt = c.SentAt,
        TotalRecipients = c.TotalRecipients,
        TotalSent = c.TotalSent,
        TotalDelivered = c.TotalDelivered,
        CreatedAt = c.CreatedAt
    };

    #endregion
}