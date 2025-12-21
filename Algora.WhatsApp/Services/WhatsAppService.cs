using Algora.WhatsApp.Configuration;
using Algora.WhatsApp.Data;
using Algora.WhatsApp.DTOs;
using Algora.WhatsApp.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Algora.WhatsApp.Services;

/// <summary>
/// Complete WhatsApp Business API service implementation using Facebook Graph API.
/// </summary>
public partial class WhatsAppService : IWhatsAppService
{
    private readonly WhatsAppDbContext _db;
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
        WhatsAppDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppService> logger)
    {
        _db = db;
        _http = httpFactory.CreateClient("WhatsApp");
        _options = options.Value;
        _logger = logger;
        _baseUrl = $"{_options.BaseUrl}/{_options.ApiVersion}";

        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }

        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
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

        // Also delete from Meta if it has an external ID
        if (!string.IsNullOrEmpty(template.ExternalTemplateId))
        {
            try
            {
                var url = $"{_baseUrl}/{_options.BusinessAccountId}/message_templates?name={template.Name}";
                await _http.DeleteAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete template from Meta: {TemplateId}", templateId);
            }
        }

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
            if (!string.IsNullOrEmpty(template.HeaderType) && template.HeaderType != "none")
            {
                var headerComponent = new Dictionary<string, object>
                {
                    ["type"] = "HEADER",
                    ["format"] = template.HeaderType.ToUpperInvariant()
                };

                if (template.HeaderType == "text" && !string.IsNullOrEmpty(template.HeaderContent))
                {
                    headerComponent["text"] = template.HeaderContent;
                }

                components.Add(headerComponent);
            }

            // Body component
            components.Add(new { type = "BODY", text = template.Body });

            // Footer component
            if (!string.IsNullOrEmpty(template.Footer))
            {
                components.Add(new { type = "FOOTER", text = template.Footer });
            }

            // Buttons component
            if (!string.IsNullOrEmpty(template.Buttons))
            {
                var buttons = JsonSerializer.Deserialize<List<object>>(template.Buttons);
                if (buttons?.Count > 0)
                {
                    components.Add(new { type = "BUTTONS", buttons });
                }
            }

            var payload = new
            {
                name = NormalizeTemplateName(template.Name),
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
                template.Status = json.RootElement.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString()?.ToLowerInvariant() ?? "submitted"
                    : "submitted";
                template.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Submitted template {Id} for approval, Meta ID: {ExternalId}", templateId, template.ExternalTemplateId);
                return true;
            }

            _logger.LogWarning("Failed to submit template {Id}: {Result}", templateId, result);

            // Parse error from Meta
            try
            {
                var errorJson = JsonDocument.Parse(result);
                if (errorJson.RootElement.TryGetProperty("error", out var error))
                {
                    template.RejectionReason = error.TryGetProperty("message", out var msg)
                        ? msg.GetString()
                        : result;
                }
            }
            catch { template.RejectionReason = result; }

            template.Status = "rejected";
            template.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting template {Id} for approval", templateId);
            template.Status = "rejected";
            template.RejectionReason = ex.Message;
            template.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return false;
        }
    }

    public async Task<int> SyncTemplatesFromMetaAsync(string shopDomain)
    {
        try
        {
            var url = $"{_baseUrl}/{_options.BusinessAccountId}/message_templates";
            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch templates from Meta: {Result}", result);
                return 0;
            }

            var json = JsonDocument.Parse(result);
            if (!json.RootElement.TryGetProperty("data", out var data)) return 0;

            var count = 0;
            foreach (var templateData in data.EnumerateArray())
            {
                var externalId = templateData.GetProperty("id").GetString();
                var name = templateData.GetProperty("name").GetString();
                var status = templateData.GetProperty("status").GetString()?.ToLowerInvariant();

                var template = await _db.WhatsAppTemplates
                    .FirstOrDefaultAsync(t => t.ShopDomain == shopDomain && t.ExternalTemplateId == externalId);

                if (template is not null)
                {
                    if (template.Status != status)
                    {
                        template.Status = status ?? template.Status;
                        if (status == "approved") template.ApprovedAt = DateTime.UtcNow;
                        template.UpdatedAt = DateTime.UtcNow;
                        count++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Synced {Count} template status updates from Meta", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing templates from Meta");
            return 0;
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
                name = NormalizeTemplateName(template.Name),
                language = new { code = template.Language },
                components = components.Count > 0 ? components : null
            }
        };

        return await SendMessageAsync(shopDomain, phoneNumber, "template", template.Body, dto.CustomerId, dto.OrderId, dto.TemplateId, payload);
    }

    public async Task<WhatsAppMessageDto> SendTextMessageAsync(string shopDomain, SendWhatsAppTextMessageDto dto)
    {
        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new { preview_url = dto.PreviewUrl, body = dto.Content }
        };

        return await SendMessageAsync(shopDomain, phoneNumber, "text", dto.Content, dto.CustomerId, dto.OrderId, null, payload);
    }

    public async Task<WhatsAppMessageDto> SendMediaMessageAsync(string shopDomain, SendWhatsAppMediaMessageDto dto)
    {
        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);

        object mediaContent = dto.MediaType.ToLowerInvariant() switch
        {
            "image" => new { link = dto.MediaUrl, caption = dto.Caption },
            "video" => new { link = dto.MediaUrl, caption = dto.Caption },
            "audio" => new { link = dto.MediaUrl },
            "document" => new { link = dto.MediaUrl, caption = dto.Caption, filename = dto.Filename },
            _ => throw new ArgumentException($"Invalid media type: {dto.MediaType}")
        };

        var payload = new Dictionary<string, object>
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = phoneNumber,
            ["type"] = dto.MediaType.ToLowerInvariant(),
            [dto.MediaType.ToLowerInvariant()] = mediaContent
        };

        var message = await SendMessageAsync(shopDomain, phoneNumber, dto.MediaType.ToLowerInvariant(), dto.Caption, dto.CustomerId, dto.OrderId, null, payload);

        // Update media fields
        var entity = await _db.WhatsAppMessages.FindAsync(message.Id);
        if (entity is not null)
        {
            entity.MediaUrl = dto.MediaUrl;
            entity.MediaCaption = dto.Caption;
            await _db.SaveChangesAsync();
        }

        return message with { MediaUrl = dto.MediaUrl, MediaCaption = dto.Caption };
    }

    public async Task<WhatsAppMessageDto> SendInteractiveMessageAsync(string shopDomain, SendWhatsAppInteractiveMessageDto dto)
    {
        var phoneNumber = NormalizePhoneNumber(dto.PhoneNumber);

        object interactive = dto.InteractiveType.ToLowerInvariant() switch
        {
            "button" => new
            {
                type = "button",
                header = dto.HeaderText is not null ? new { type = "text", text = dto.HeaderText } : null,
                body = new { text = dto.BodyText },
                footer = dto.FooterText is not null ? new { text = dto.FooterText } : null,
                action = new
                {
                    buttons = dto.Buttons?.Select(b => new
                    {
                        type = "reply",
                        reply = new { id = b.Id, title = b.Title }
                    }).ToList()
                }
            },
            "list" => new
            {
                type = "list",
                header = dto.HeaderText is not null ? new { type = "text", text = dto.HeaderText } : null,
                body = new { text = dto.BodyText },
                footer = dto.FooterText is not null ? new { text = dto.FooterText } : null,
                action = new
                {
                    button = "Options",
                    sections = dto.Sections?.Select(s => new
                    {
                        title = s.Title,
                        rows = s.Rows.Select(r => new { id = r.Id, title = r.Title, description = r.Description }).ToList()
                    }).ToList()
                }
            },
            _ => throw new ArgumentException($"Invalid interactive type: {dto.InteractiveType}")
        };

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "interactive",
            interactive
        };

        return await SendMessageAsync(shopDomain, phoneNumber, "interactive", dto.BodyText, dto.CustomerId, dto.OrderId, null, payload);
    }

    private async Task<WhatsAppMessageDto> SendMessageAsync(
        string shopDomain, string phoneNumber, string messageType, string? content,
        int? customerId, int? orderId, int? templateId, object payload)
    {
        var message = new WhatsAppMessage
        {
            ShopDomain = shopDomain,
            CustomerId = customerId,
            OrderId = orderId,
            PhoneNumber = phoneNumber,
            Direction = "outbound",
            MessageType = messageType,
            TemplateId = templateId,
            Content = content,
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
                _logger.LogInformation("Sent WhatsApp {Type} message to {Phone}", messageType, phoneNumber);
            }
            else
            {
                message.Status = "failed";
                ParseErrorResponse(result, message);
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
        await UpdateOrCreateConversationAsync(shopDomain, phoneNumber, message);

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

    public async Task<bool> MarkMessageAsReadAsync(string externalMessageId)
    {
        try
        {
            var url = $"{_baseUrl}/{_options.PhoneNumberId}/messages";
            var payload = new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = externalMessageId
            };

            var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId}", externalMessageId);
            return false;
        }
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

    public async Task<PaginatedResult<WhatsAppConversationDto>> GetConversationsAsync(string shopDomain, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _db.WhatsAppConversations.AsNoTracking().Where(c => c.ShopDomain == shopDomain);
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
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
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages.Select(MapToDto).Reverse();
    }

    public async Task<WhatsAppConversationDto> UpdateConversationAsync(int conversationId, UpdateConversationDto dto)
    {
        var conversation = await _db.WhatsAppConversations.FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found");

        if (dto.Status is not null) conversation.Status = dto.Status;
        if (dto.AssignedTo is not null) conversation.AssignedTo = dto.AssignedTo;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(conversation);
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

    public async Task<bool> IsWindowOpenAsync(int conversationId)
    {
        var conversation = await _db.WhatsAppConversations.FindAsync(conversationId);
        return conversation?.WindowExpiresAt > DateTime.UtcNow;
    }

    private async Task UpdateOrCreateConversationAsync(string shopDomain, string phoneNumber, WhatsAppMessage message)
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
            await _db.SaveChangesAsync();
        }

        message.ConversationId = conversation.Id;
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.LastMessagePreview = message.Content?.Length > 100
            ? message.Content[..100] + "..."
            : message.Content;

        if (message.Direction == "inbound")
        {
            conversation.UnreadCount++;
            conversation.WindowExpiresAt = DateTime.UtcNow.AddHours(_options.MessageWindowHours);
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

    public async Task<IEnumerable<WhatsAppCampaignDto>> GetCampaignsAsync(string shopDomain, string? status = null)
    {
        var query = _db.WhatsAppCampaigns.AsNoTracking()
            .Include(c => c.Template)
            .Where(c => c.ShopDomain == shopDomain);
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        return await query.OrderByDescending(c => c.CreatedAt).Select(c => MapToDto(c)).ToListAsync();
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
            ScheduledAt = dto.ScheduledAt,
            Status = dto.ScheduledAt.HasValue ? "scheduled" : "draft"
        };

        _db.WhatsAppCampaigns.Add(campaign);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created WhatsApp campaign {Name} for {Shop}", dto.Name, shopDomain);
        return MapToDto(campaign, template);
    }

    public async Task<WhatsAppCampaignDto> UpdateCampaignAsync(int campaignId, UpdateWhatsAppCampaignDto dto)
    {
        var campaign = await _db.WhatsAppCampaigns.Include(c => c.Template).FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        if (campaign.Status is "sending" or "sent")
            throw new InvalidOperationException("Cannot update a campaign that is sending or already sent");

        if (dto.Name is not null) campaign.Name = dto.Name;
        if (dto.TemplateId.HasValue) campaign.TemplateId = dto.TemplateId.Value;
        if (dto.SegmentId.HasValue) campaign.SegmentId = dto.SegmentId.Value;
        if (dto.ScheduledAt.HasValue) campaign.ScheduledAt = dto.ScheduledAt.Value;
        if (dto.Status is not null) campaign.Status = dto.Status;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(campaign);
    }

    public async Task<bool> DeleteCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns.FindAsync(campaignId);
        if (campaign is null) return false;

        if (campaign.Status is "sending" or "sent")
            throw new InvalidOperationException("Cannot delete a campaign that is sending or already sent");

        _db.WhatsAppCampaigns.Remove(campaign);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns.Include(c => c.Template).FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        if (campaign.Template is null || !campaign.Template.IsActive || campaign.Template.Status != "approved")
            throw new InvalidOperationException("Campaign template is not active or approved");

        campaign.Status = "sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Note: Actual recipient fetching should be done through a provider interface
        // This is a placeholder - the main app should inject recipients
        _logger.LogWarning("Campaign {Id} started but recipient fetching requires external integration", campaignId);

        campaign.Status = "sent";
        campaign.SentAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PauseCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns.FindAsync(campaignId);
        if (campaign is null || campaign.Status != "sending") return false;

        campaign.Status = "paused";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResumeCampaignAsync(int campaignId)
    {
        var campaign = await _db.WhatsAppCampaigns.FindAsync(campaignId);
        if (campaign is null || campaign.Status != "paused") return false;

        campaign.Status = "sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Webhooks

    public bool VerifyWebhook(string mode, string token, string challenge, out string response)
    {
        if (mode == "subscribe" && token == _options.WebhookVerifyToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            response = challenge;
            return true;
        }

        _logger.LogWarning("Webhook verification failed: mode={Mode}, token mismatch", mode);
        response = "Verification failed";
        return false;
    }

    public async Task ProcessWebhookAsync(string shopDomain, WhatsAppWebhookPayload payload)
    {
        if (payload.Object != "whatsapp_business_account") return;

        foreach (var entry in payload.Entry)
        {
            foreach (var change in entry.Changes)
            {
                if (change.Field != "messages") continue;

                var value = change.Value;

                // Process incoming messages
                if (value.Messages is not null)
                {
                    foreach (var msg in value.Messages)
                    {
                        await HandleIncomingMessageAsync(shopDomain, msg, value.Contacts?.FirstOrDefault());
                    }
                }

                // Process status updates
                if (value.Statuses is not null)
                {
                    foreach (var status in value.Statuses)
                    {
                        await HandleStatusUpdateAsync(status);
                    }
                }
            }
        }
    }

    public bool VerifySignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_options.AppSecret)) return true; // Skip if not configured

        try
        {
            var expectedSignature = "sha256=" + ComputeHmacSha256(payload, _options.AppSecret);
            return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    private async Task HandleIncomingMessageAsync(string shopDomain, WebhookMessage msg, WebhookContact? contact)
    {
        var phoneNumber = NormalizePhoneNumber(msg.From);

        var content = msg.Type switch
        {
            "text" => msg.Text?.Body,
            "image" => msg.Image?.Caption ?? "[Image]",
            "video" => msg.Video?.Caption ?? "[Video]",
            "audio" => "[Audio]",
            "document" => msg.Document?.Filename ?? "[Document]",
            "interactive" => msg.Interactive?.ButtonReply?.Title ?? msg.Interactive?.ListReply?.Title,
            "button" => msg.Button?.Title,
            _ => $"[{msg.Type}]"
        };

        var message = new WhatsAppMessage
        {
            ShopDomain = shopDomain,
            ExternalMessageId = msg.Id,
            PhoneNumber = phoneNumber,
            Direction = "inbound",
            MessageType = msg.Type,
            Content = content,
            Status = "delivered",
            SentAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(msg.Timestamp)).UtcDateTime,
            DeliveredAt = DateTime.UtcNow
        };

        // Set media info if available
        if (msg.Image is not null) message.MediaMimeType = msg.Image.MimeType;
        if (msg.Video is not null) message.MediaMimeType = msg.Video.MimeType;
        if (msg.Audio is not null) message.MediaMimeType = msg.Audio.MimeType;
        if (msg.Document is not null)
        {
            message.MediaMimeType = msg.Document.MimeType;
            message.MediaCaption = msg.Document.Filename;
        }

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync();

        await UpdateOrCreateConversationAsync(shopDomain, phoneNumber, message);

        // Update conversation with customer name from contact
        if (contact?.Profile?.Name is not null)
        {
            var conversation = await _db.WhatsAppConversations
                .FirstOrDefaultAsync(c => c.Id == message.ConversationId);
            if (conversation is not null && string.IsNullOrEmpty(conversation.CustomerName))
            {
                conversation.CustomerName = contact.Profile.Name;
                await _db.SaveChangesAsync();
            }
        }

        // Auto mark as read if configured
        if (_options.AutoMarkAsRead)
        {
            await MarkMessageAsReadAsync(msg.Id);
        }

        _logger.LogInformation("Received WhatsApp message from {Phone}: {Type}", phoneNumber, msg.Type);
    }

    private async Task HandleStatusUpdateAsync(WebhookStatus status)
    {
        var message = await _db.WhatsAppMessages
            .FirstOrDefaultAsync(m => m.ExternalMessageId == status.Id);

        if (message is null)
        {
            _logger.LogDebug("Message not found for status update: {MessageId}", status.Id);
            return;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(status.Timestamp)).UtcDateTime;

        message.Status = status.Status.ToLowerInvariant();

        switch (status.Status.ToLowerInvariant())
        {
            case "sent":
                message.SentAt ??= timestamp;
                break;
            case "delivered":
                message.DeliveredAt = timestamp;
                break;
            case "read":
                message.ReadAt = timestamp;
                // Mark conversation messages as read
                if (message.ConversationId.HasValue)
                {
                    var conversation = await _db.WhatsAppConversations.FindAsync(message.ConversationId.Value);
                    if (conversation is not null)
                    {
                        conversation.UnreadCount = 0;
                        await _db.SaveChangesAsync();
                    }
                }
                break;
            case "failed":
                if (status.Errors?.Count > 0)
                {
                    var error = status.Errors[0];
                    message.ErrorCode = error.Code.ToString();
                    message.ErrorMessage = error.Message ?? error.Title;
                }
                break;
        }

        // Update conversation window from status
        if (status.Conversation?.ExpirationTimestamp is not null)
        {
            var conversation = await _db.WhatsAppConversations.FindAsync(message.ConversationId);
            if (conversation is not null)
            {
                conversation.ExternalConversationId = status.Conversation.Id;
                conversation.WindowExpiresAt = DateTimeOffset.FromUnixTimeSeconds(
                    long.Parse(status.Conversation.ExpirationTimestamp.ExpirationTimestamp)).UtcDateTime;
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogDebug("Updated message {Id} status to {Status}", message.Id, status.Status);
    }

    #endregion

    #region Helpers

    private static string NormalizePhoneNumber(string phone)
    {
        var normalized = PhoneRegex().Replace(phone, "");
        if (!normalized.StartsWith('+') && !normalized.StartsWith('0'))
            normalized = "+" + normalized;
        return normalized.TrimStart('0');
    }

    private static string NormalizeTemplateName(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    private static void ParseErrorResponse(string result, WhatsAppMessage message)
    {
        try
        {
            var json = JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                message.ErrorCode = error.TryGetProperty("code", out var code) ? code.ToString() : null;
                message.ErrorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : result;
            }
            else
            {
                message.ErrorMessage = result;
            }
        }
        catch
        {
            message.ErrorMessage = result;
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
        HeaderType = t.HeaderType,
        HeaderContent = t.HeaderContent,
        Body = t.Body,
        Footer = t.Footer,
        Buttons = t.Buttons,
        Status = t.Status,
        RejectionReason = t.RejectionReason,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        ApprovedAt = t.ApprovedAt
    };

    private static WhatsAppMessageDto MapToDto(WhatsAppMessage m) => new()
    {
        Id = m.Id,
        ShopDomain = m.ShopDomain,
        ExternalMessageId = m.ExternalMessageId,
        CustomerId = m.CustomerId,
        OrderId = m.OrderId,
        ConversationId = m.ConversationId,
        PhoneNumber = m.PhoneNumber,
        Direction = m.Direction,
        MessageType = m.MessageType,
        TemplateId = m.TemplateId,
        Content = m.Content,
        MediaUrl = m.MediaUrl,
        MediaMimeType = m.MediaMimeType,
        MediaCaption = m.MediaCaption,
        Status = m.Status,
        ErrorCode = m.ErrorCode,
        ErrorMessage = m.ErrorMessage,
        SentAt = m.SentAt,
        DeliveredAt = m.DeliveredAt,
        ReadAt = m.ReadAt,
        CreatedAt = m.CreatedAt
    };

    private static WhatsAppConversationDto MapToDto(WhatsAppConversation c) => new()
    {
        Id = c.Id,
        ShopDomain = c.ShopDomain,
        ExternalConversationId = c.ExternalConversationId,
        CustomerId = c.CustomerId,
        PhoneNumber = c.PhoneNumber,
        CustomerName = c.CustomerName,
        Status = c.Status,
        AssignedTo = c.AssignedTo,
        LastMessageAt = c.LastMessageAt,
        LastMessagePreview = c.LastMessagePreview,
        UnreadCount = c.UnreadCount,
        IsBusinessInitiated = c.IsBusinessInitiated,
        WindowExpiresAt = c.WindowExpiresAt,
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
        ScheduledAt = c.ScheduledAt,
        SentAt = c.SentAt,
        TotalRecipients = c.TotalRecipients,
        TotalSent = c.TotalSent,
        TotalDelivered = c.TotalDelivered,
        TotalRead = c.TotalRead,
        TotalFailed = c.TotalFailed,
        CreatedAt = c.CreatedAt
    };

    #endregion
}
