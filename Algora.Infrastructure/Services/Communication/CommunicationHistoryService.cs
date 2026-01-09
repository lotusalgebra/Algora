using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Service for unified communication history across all channels.
/// </summary>
public class CommunicationHistoryService : ICommunicationHistoryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CommunicationHistoryService> _logger;

    public CommunicationHistoryService(AppDbContext db, ILogger<CommunicationHistoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CommunicationHistoryResultDto> GetHistoryAsync(string shopDomain, CommunicationHistoryFilterDto filter)
    {
        var items = new List<CommunicationHistoryItemDto>();

        // Get Email Campaign Recipients
        if (filter.Channel == null || filter.Channel == "all" || filter.Channel == "email")
        {
            var emailQuery = _db.EmailCampaignRecipients
                .Include(r => r.EmailCampaign)
                .Where(r => r.EmailCampaign.ShopDomain == shopDomain);

            if (filter.FromDate.HasValue)
                emailQuery = emailQuery.Where(r => r.CreatedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                emailQuery = emailQuery.Where(r => r.CreatedAt <= filter.ToDate.Value);
            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "all")
                emailQuery = emailQuery.Where(r => r.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Search))
                emailQuery = emailQuery.Where(r => r.Email.Contains(filter.Search) || r.EmailCampaign.Subject.Contains(filter.Search));

            var emailItems = await emailQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(500)
                .Select(r => new CommunicationHistoryItemDto
                {
                    Id = r.Id,
                    Channel = "email",
                    Type = "campaign",
                    Direction = "outbound",
                    RecipientEmail = r.Email,
                    Subject = r.EmailCampaign.Subject,
                    Preview = r.EmailCampaign.PreviewText,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    SentAt = r.SentAt,
                    DeliveredAt = r.DeliveredAt,
                    OpenedAt = r.OpenedAt,
                    ClickedAt = r.ClickedAt,
                    CampaignName = r.EmailCampaign.Name,
                    ErrorMessage = r.ErrorMessage,
                    RelatedId = r.EmailCampaignId
                })
                .ToListAsync();

            items.AddRange(emailItems);
        }

        // Get SMS Messages
        if (filter.Channel == null || filter.Channel == "all" || filter.Channel == "sms")
        {
            var smsQuery = _db.SmsMessages
                .Include(s => s.Template)
                .Where(s => s.ShopDomain == shopDomain);

            if (filter.FromDate.HasValue)
                smsQuery = smsQuery.Where(s => s.CreatedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                smsQuery = smsQuery.Where(s => s.CreatedAt <= filter.ToDate.Value);
            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "all")
                smsQuery = smsQuery.Where(s => s.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Search))
                smsQuery = smsQuery.Where(s => s.PhoneNumber.Contains(filter.Search) || s.Body.Contains(filter.Search));

            var smsItems = await smsQuery
                .OrderByDescending(s => s.CreatedAt)
                .Take(500)
                .Select(s => new CommunicationHistoryItemDto
                {
                    Id = s.Id,
                    Channel = "sms",
                    Type = s.TemplateId.HasValue ? "template" : "direct",
                    Direction = "outbound",
                    RecipientPhone = s.PhoneNumber,
                    Preview = s.Body.Length > 100 ? s.Body.Substring(0, 100) + "..." : s.Body,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    SentAt = s.SentAt,
                    DeliveredAt = s.DeliveredAt,
                    TemplateName = s.Template != null ? s.Template.Name : null,
                    ErrorMessage = s.ErrorMessage
                })
                .ToListAsync();

            items.AddRange(smsItems);
        }

        // Get Conversation Messages (WhatsApp and other channels)
        if (filter.Channel == null || filter.Channel == "all" || filter.Channel == "whatsapp")
        {
            var convQuery = _db.ConversationMessages
                .Include(m => m.ConversationThread)
                .Where(m => m.ConversationThread.ShopDomain == shopDomain && m.Channel == "whatsapp");

            if (filter.FromDate.HasValue)
                convQuery = convQuery.Where(m => m.SentAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                convQuery = convQuery.Where(m => m.SentAt <= filter.ToDate.Value);
            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "all")
                convQuery = convQuery.Where(m => m.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Search))
                convQuery = convQuery.Where(m => m.Content.Contains(filter.Search) || m.SenderName!.Contains(filter.Search));

            var whatsappItems = await convQuery
                .OrderByDescending(m => m.SentAt)
                .Take(500)
                .Select(m => new CommunicationHistoryItemDto
                {
                    Id = m.Id,
                    Channel = "whatsapp",
                    Type = m.SenderType == "agent" ? "reply" : "direct",
                    Direction = m.Direction,
                    RecipientName = m.ConversationThread.CustomerName,
                    RecipientPhone = m.ConversationThread.CustomerPhone,
                    Preview = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content,
                    Status = m.Status,
                    CreatedAt = m.SentAt,
                    SentAt = m.SentAt,
                    DeliveredAt = m.DeliveredAt
                })
                .ToListAsync();

            items.AddRange(whatsappItems);
        }

        // Apply sorting
        items = filter.SortDescending
            ? items.OrderByDescending(i => i.CreatedAt).ToList()
            : items.OrderBy(i => i.CreatedAt).ToList();

        var totalCount = items.Count;

        // Apply pagination
        var pagedItems = items
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new CommunicationHistoryResultDto
        {
            Items = pagedItems,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<CommunicationStatsDto> GetStatsAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var stats = new CommunicationStatsDto();

        // Email stats
        var emailQuery = _db.EmailCampaignRecipients
            .Include(r => r.EmailCampaign)
            .Where(r => r.EmailCampaign.ShopDomain == shopDomain);

        if (fromDate.HasValue)
            emailQuery = emailQuery.Where(r => r.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            emailQuery = emailQuery.Where(r => r.CreatedAt <= toDate.Value);

        var emailStats = await emailQuery
            .GroupBy(r => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Sent = g.Count(r => r.SentAt != null),
                Delivered = g.Count(r => r.DeliveredAt != null),
                Opened = g.Count(r => r.OpenedAt != null),
                Clicked = g.Count(r => r.ClickedAt != null),
                Failed = g.Count(r => r.Status == "failed" || r.Status == "bounced")
            })
            .FirstOrDefaultAsync();

        if (emailStats != null)
        {
            stats.EmailCount = emailStats.Count;
            stats.TotalSent += emailStats.Sent;
            stats.TotalDelivered += emailStats.Delivered;
            stats.TotalOpened += emailStats.Opened;
            stats.TotalClicked += emailStats.Clicked;
            stats.TotalFailed += emailStats.Failed;
        }

        // SMS stats
        var smsQuery = _db.SmsMessages.Where(s => s.ShopDomain == shopDomain);

        if (fromDate.HasValue)
            smsQuery = smsQuery.Where(s => s.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            smsQuery = smsQuery.Where(s => s.CreatedAt <= toDate.Value);

        var smsStats = await smsQuery
            .GroupBy(s => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Sent = g.Count(s => s.SentAt != null),
                Delivered = g.Count(s => s.DeliveredAt != null),
                Failed = g.Count(s => s.Status == "failed")
            })
            .FirstOrDefaultAsync();

        if (smsStats != null)
        {
            stats.SmsCount = smsStats.Count;
            stats.TotalSent += smsStats.Sent;
            stats.TotalDelivered += smsStats.Delivered;
            stats.TotalFailed += smsStats.Failed;
        }

        // WhatsApp stats
        var whatsappQuery = _db.ConversationMessages
            .Include(m => m.ConversationThread)
            .Where(m => m.ConversationThread.ShopDomain == shopDomain && m.Channel == "whatsapp" && m.Direction == "outbound");

        if (fromDate.HasValue)
            whatsappQuery = whatsappQuery.Where(m => m.SentAt >= fromDate.Value);
        if (toDate.HasValue)
            whatsappQuery = whatsappQuery.Where(m => m.SentAt <= toDate.Value);

        var whatsappStats = await whatsappQuery
            .GroupBy(m => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Delivered = g.Count(m => m.DeliveredAt != null),
                Read = g.Count(m => m.ReadAt != null),
                Failed = g.Count(m => m.Status == "failed")
            })
            .FirstOrDefaultAsync();

        if (whatsappStats != null)
        {
            stats.WhatsAppCount = whatsappStats.Count;
            stats.TotalSent += whatsappStats.Count;
            stats.TotalDelivered += whatsappStats.Delivered;
            stats.TotalOpened += whatsappStats.Read; // Read = Opened for WhatsApp
            stats.TotalFailed += whatsappStats.Failed;
        }

        return stats;
    }

    public async Task<CommunicationHistoryItemDto?> GetByIdAsync(string shopDomain, int id, string channel)
    {
        switch (channel.ToLower())
        {
            case "email":
                var email = await _db.EmailCampaignRecipients
                    .Include(r => r.EmailCampaign)
                    .Where(r => r.Id == id && r.EmailCampaign.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (email == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = email.Id,
                    Channel = "email",
                    Type = "campaign",
                    Direction = "outbound",
                    RecipientEmail = email.Email,
                    Subject = email.EmailCampaign.Subject,
                    Preview = email.EmailCampaign.PreviewText,
                    Status = email.Status,
                    CreatedAt = email.CreatedAt,
                    SentAt = email.SentAt,
                    DeliveredAt = email.DeliveredAt,
                    OpenedAt = email.OpenedAt,
                    ClickedAt = email.ClickedAt,
                    CampaignName = email.EmailCampaign.Name,
                    ErrorMessage = email.ErrorMessage,
                    RelatedId = email.EmailCampaignId
                };

            case "sms":
                var sms = await _db.SmsMessages
                    .Include(s => s.Template)
                    .Where(s => s.Id == id && s.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (sms == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = sms.Id,
                    Channel = "sms",
                    Type = sms.TemplateId.HasValue ? "template" : "direct",
                    Direction = "outbound",
                    RecipientPhone = sms.PhoneNumber,
                    Preview = sms.Body,
                    Status = sms.Status,
                    CreatedAt = sms.CreatedAt,
                    SentAt = sms.SentAt,
                    DeliveredAt = sms.DeliveredAt,
                    TemplateName = sms.Template?.Name,
                    ErrorMessage = sms.ErrorMessage
                };

            case "whatsapp":
                var whatsapp = await _db.ConversationMessages
                    .Include(m => m.ConversationThread)
                    .Where(m => m.Id == id && m.ConversationThread.ShopDomain == shopDomain && m.Channel == "whatsapp")
                    .FirstOrDefaultAsync();

                if (whatsapp == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = whatsapp.Id,
                    Channel = "whatsapp",
                    Type = whatsapp.SenderType == "agent" ? "reply" : "direct",
                    Direction = whatsapp.Direction,
                    RecipientName = whatsapp.ConversationThread.CustomerName,
                    RecipientPhone = whatsapp.ConversationThread.CustomerPhone,
                    Preview = whatsapp.Content,
                    Status = whatsapp.Status,
                    CreatedAt = whatsapp.SentAt,
                    SentAt = whatsapp.SentAt,
                    DeliveredAt = whatsapp.DeliveredAt
                };

            default:
                return null;
        }
    }

    public async Task<CommunicationHistoryItemDto?> GetMessageDetailsAsync(string shopDomain, string channel, int id)
    {
        switch (channel.ToLower())
        {
            case "email":
                var email = await _db.EmailCampaignRecipients
                    .Include(r => r.EmailCampaign)
                    .Where(r => r.Id == id && r.EmailCampaign.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (email == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = email.Id,
                    Channel = "email",
                    Type = "campaign",
                    Direction = "outbound",
                    RecipientEmail = email.Email,
                    Subject = email.EmailCampaign.Subject,
                    Preview = email.EmailCampaign.PreviewText,
                    Body = email.EmailCampaign.Body,
                    Status = email.Status,
                    CreatedAt = email.CreatedAt,
                    SentAt = email.SentAt,
                    DeliveredAt = email.DeliveredAt,
                    OpenedAt = email.OpenedAt,
                    ClickedAt = email.ClickedAt,
                    CampaignName = email.EmailCampaign.Name,
                    ErrorMessage = email.ErrorMessage,
                    RelatedId = email.EmailCampaignId
                };

            case "sms":
                var sms = await _db.SmsMessages
                    .Include(s => s.Template)
                    .Where(s => s.Id == id && s.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (sms == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = sms.Id,
                    Channel = "sms",
                    Type = sms.TemplateId.HasValue ? "template" : "direct",
                    Direction = "outbound",
                    RecipientPhone = sms.PhoneNumber,
                    Preview = sms.Body.Length > 100 ? sms.Body.Substring(0, 100) + "..." : sms.Body,
                    Body = sms.Body,
                    Status = sms.Status,
                    CreatedAt = sms.CreatedAt,
                    SentAt = sms.SentAt,
                    DeliveredAt = sms.DeliveredAt,
                    TemplateName = sms.Template?.Name,
                    ErrorMessage = sms.ErrorMessage
                };

            case "whatsapp":
                var whatsapp = await _db.ConversationMessages
                    .Include(m => m.ConversationThread)
                    .Where(m => m.Id == id && m.ConversationThread.ShopDomain == shopDomain && m.Channel == "whatsapp")
                    .FirstOrDefaultAsync();

                if (whatsapp == null) return null;

                return new CommunicationHistoryItemDto
                {
                    Id = whatsapp.Id,
                    Channel = "whatsapp",
                    Type = whatsapp.SenderType == "agent" ? "reply" : "direct",
                    Direction = whatsapp.Direction,
                    RecipientName = whatsapp.ConversationThread.CustomerName,
                    RecipientPhone = whatsapp.ConversationThread.CustomerPhone,
                    Preview = whatsapp.Content.Length > 100 ? whatsapp.Content.Substring(0, 100) + "..." : whatsapp.Content,
                    Body = whatsapp.Content,
                    Status = whatsapp.Status,
                    CreatedAt = whatsapp.SentAt,
                    SentAt = whatsapp.SentAt,
                    DeliveredAt = whatsapp.DeliveredAt
                };

            default:
                return null;
        }
    }

    public async Task ResendMessageAsync(string shopDomain, string channel, int id)
    {
        switch (channel.ToLower())
        {
            case "email":
                var email = await _db.EmailCampaignRecipients
                    .Include(r => r.EmailCampaign)
                    .Where(r => r.Id == id && r.EmailCampaign.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (email == null)
                    throw new InvalidOperationException("Email message not found");

                // Reset status to pending for resend
                email.Status = "pending";
                email.SentAt = null;
                email.DeliveredAt = null;
                email.ErrorMessage = null;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Email {Id} queued for resend to {Email}", id, email.Email);
                break;

            case "sms":
                var sms = await _db.SmsMessages
                    .Where(s => s.Id == id && s.ShopDomain == shopDomain)
                    .FirstOrDefaultAsync();

                if (sms == null)
                    throw new InvalidOperationException("SMS message not found");

                // Reset status to pending for resend
                sms.Status = "pending";
                sms.SentAt = null;
                sms.DeliveredAt = null;
                sms.ErrorMessage = null;
                await _db.SaveChangesAsync();
                _logger.LogInformation("SMS {Id} queued for resend to {Phone}", id, sms.PhoneNumber);
                break;

            case "whatsapp":
                var whatsapp = await _db.ConversationMessages
                    .Include(m => m.ConversationThread)
                    .Where(m => m.Id == id && m.ConversationThread.ShopDomain == shopDomain && m.Channel == "whatsapp")
                    .FirstOrDefaultAsync();

                if (whatsapp == null)
                    throw new InvalidOperationException("WhatsApp message not found");

                // Reset status to pending for resend
                whatsapp.Status = "pending";
                whatsapp.DeliveredAt = null;
                await _db.SaveChangesAsync();
                _logger.LogInformation("WhatsApp message {Id} queued for resend", id);
                break;

            default:
                throw new InvalidOperationException($"Unknown channel: {channel}");
        }
    }
}
