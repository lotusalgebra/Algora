using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

public class EmailMarketingService(AppDbContext db, ILogger<EmailMarketingService> logger) : IEmailMarketingService
{
    #region Subscribers

    public async Task<EmailSubscriberDto?> GetSubscriberAsync(string shopDomain, string email)
    {
        var entity = await db.EmailSubscribers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain && s.Email == email.ToLowerInvariant());
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<EmailSubscriberDto?> GetSubscriberByIdAsync(int subscriberId)
    {
        var entity = await db.EmailSubscribers.FindAsync(subscriberId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<PaginatedResult<EmailSubscriberDto>> GetSubscribersAsync(string shopDomain, int page = 1, int pageSize = 50, string? status = null)
    {
        var query = db.EmailSubscribers.AsNoTracking().Where(s => s.ShopDomain == shopDomain);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return PaginatedResult<EmailSubscriberDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<EmailSubscriberDto> CreateSubscriberAsync(string shopDomain, CreateEmailSubscriberDto dto)
    {
        var entity = new EmailSubscriber
        {
            ShopDomain = shopDomain,
            Email = dto.Email.ToLowerInvariant(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            CustomerId = dto.CustomerId,
            Source = dto.Source,
            Status = "subscribed",
            EmailOptIn = dto.EmailOptIn,
            SmsOptIn = dto.SmsOptIn,
            WhatsAppOptIn = dto.WhatsAppOptIn,
            Tags = dto.Tags,
            ConfirmedAt = DateTime.UtcNow
        };
        db.EmailSubscribers.Add(entity);
        await db.SaveChangesAsync();
        logger.LogInformation("Created subscriber {Email} for {Shop}", dto.Email, shopDomain);
        return MapToDto(entity);
    }

    public async Task<EmailSubscriberDto> UpdateSubscriberAsync(int subscriberId, UpdateEmailSubscriberDto dto)
    {
        var entity = await db.EmailSubscribers.FindAsync(subscriberId) ?? throw new InvalidOperationException($"Subscriber {subscriberId} not found");
        if (dto.FirstName is not null) entity.FirstName = dto.FirstName;
        if (dto.LastName is not null) entity.LastName = dto.LastName;
        if (dto.Phone is not null) entity.Phone = dto.Phone;
        if (dto.EmailOptIn.HasValue) entity.EmailOptIn = dto.EmailOptIn.Value;
        if (dto.SmsOptIn.HasValue) entity.SmsOptIn = dto.SmsOptIn.Value;
        if (dto.WhatsAppOptIn.HasValue) entity.WhatsAppOptIn = dto.WhatsAppOptIn.Value;
        if (dto.Tags is not null) entity.Tags = dto.Tags;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> UnsubscribeAsync(string shopDomain, string email, string? reason = null)
    {
        var entity = await db.EmailSubscribers.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain && s.Email == email.ToLowerInvariant());
        if (entity is null) return false;
        entity.Status = "unsubscribed";
        entity.UnsubscribedAt = DateTime.UtcNow;
        entity.UnsubscribeReason = reason;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation("Unsubscribed {Email} from {Shop}", email, shopDomain);
        return true;
    }

    public async Task<int> ImportSubscribersAsync(string shopDomain, IEnumerable<CreateEmailSubscriberDto> subscribers, int? listId = null)
    {
        var count = 0;
        foreach (var dto in subscribers)
        {
            try
            {
                if (!await db.EmailSubscribers.AnyAsync(s => s.ShopDomain == shopDomain && s.Email == dto.Email.ToLowerInvariant()))
                {
                    var sub = await CreateSubscriberAsync(shopDomain, dto);
                    if (listId.HasValue) await AddSubscriberToListAsync(listId.Value, sub.Id);
                    count++;
                }
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to import {Email}", dto.Email); }
        }
        return count;
    }

    #endregion

    #region Lists

    public async Task<EmailListDto?> GetListAsync(int listId)
    {
        var entity = await db.EmailLists.FindAsync(listId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<EmailListDto>> GetListsAsync(string shopDomain)
        => await db.EmailLists.AsNoTracking().Where(l => l.ShopDomain == shopDomain).OrderBy(l => l.Name).Select(l => MapToDto(l)).ToListAsync();

    public async Task<EmailListDto> CreateListAsync(string shopDomain, CreateEmailListDto dto)
    {
        var entity = new EmailList { ShopDomain = shopDomain, Name = dto.Name, Description = dto.Description, IsDefault = dto.IsDefault, DoubleOptIn = dto.DoubleOptIn, IsActive = true };
        db.EmailLists.Add(entity);
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<EmailListDto> UpdateListAsync(int listId, UpdateEmailListDto dto)
    {
        var entity = await db.EmailLists.FindAsync(listId) ?? throw new InvalidOperationException($"List {listId} not found");
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.DoubleOptIn.HasValue) entity.DoubleOptIn = dto.DoubleOptIn.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteListAsync(int listId)
    {
        var entity = await db.EmailLists.FindAsync(listId);
        if (entity is null) return false;
        db.EmailLists.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddSubscriberToListAsync(int listId, int subscriberId)
    {
        if (await db.EmailListSubscribers.AnyAsync(ls => ls.EmailListId == listId && ls.EmailSubscriberId == subscriberId)) return true;
        db.EmailListSubscribers.Add(new EmailListSubscriber { EmailListId = listId, EmailSubscriberId = subscriberId, Status = "subscribed" });
        var list = await db.EmailLists.FindAsync(listId);
        if (list is not null) list.SubscriberCount++;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveSubscriberFromListAsync(int listId, int subscriberId)
    {
        var entry = await db.EmailListSubscribers.FirstOrDefaultAsync(ls => ls.EmailListId == listId && ls.EmailSubscriberId == subscriberId);
        if (entry is null) return false;
        db.EmailListSubscribers.Remove(entry);
        var list = await db.EmailLists.FindAsync(listId);
        if (list is not null && list.SubscriberCount > 0) list.SubscriberCount--;
        await db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Segments

    public async Task<CustomerSegmentDto?> GetSegmentAsync(int segmentId)
    {
        var entity = await db.CustomerSegments.FindAsync(segmentId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<CustomerSegmentDto>> GetSegmentsAsync(string shopDomain)
        => await db.CustomerSegments.AsNoTracking().Where(s => s.ShopDomain == shopDomain).OrderBy(s => s.Name).Select(s => MapToDto(s)).ToListAsync();

    public async Task<CustomerSegmentDto> CreateSegmentAsync(string shopDomain, CreateCustomerSegmentDto dto)
    {
        var entity = new CustomerSegment { ShopDomain = shopDomain, Name = dto.Name, Description = dto.Description, SegmentType = dto.SegmentType, FilterCriteria = dto.FilterCriteria, IsActive = true };
        db.CustomerSegments.Add(entity);
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<CustomerSegmentDto> UpdateSegmentAsync(int segmentId, UpdateCustomerSegmentDto dto)
    {
        var entity = await db.CustomerSegments.FindAsync(segmentId) ?? throw new InvalidOperationException($"Segment {segmentId} not found");
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.FilterCriteria is not null) entity.FilterCriteria = dto.FilterCriteria;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteSegmentAsync(int segmentId)
    {
        var entity = await db.CustomerSegments.FindAsync(segmentId);
        if (entity is null) return false;
        db.CustomerSegments.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> RecalculateSegmentAsync(int segmentId)
    {
        var entity = await db.CustomerSegments.FindAsync(segmentId) ?? throw new InvalidOperationException($"Segment {segmentId} not found");
        var existing = await db.CustomerSegmentMembers.Where(m => m.SegmentId == segmentId).ToListAsync();
        db.CustomerSegmentMembers.RemoveRange(existing);
        // TODO: Implement dynamic calculation based on FilterCriteria
        entity.LastCalculatedAt = DateTime.UtcNow;
        entity.MemberCount = 0;
        await db.SaveChangesAsync();
        return entity.MemberCount;
    }

    #endregion

    #region Campaigns

    public async Task<EmailCampaignDto?> GetCampaignAsync(int campaignId)
    {
        var entity = await db.EmailCampaigns.Include(c => c.Segment).FirstOrDefaultAsync(c => c.Id == campaignId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<PaginatedResult<EmailCampaignDto>> GetCampaignsAsync(string shopDomain, int page = 1, int pageSize = 20, string? status = null)
    {
        var query = db.EmailCampaigns.AsNoTracking().Include(c => c.Segment).Where(c => c.ShopDomain == shopDomain);
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return PaginatedResult<EmailCampaignDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<EmailCampaignDto> CreateCampaignAsync(string shopDomain, CreateEmailCampaignDto dto)
    {
        var entity = new EmailCampaign
        {
            ShopDomain = shopDomain, Name = dto.Name, Subject = dto.Subject, PreviewText = dto.PreviewText, Body = dto.Body,
            FromName = dto.FromName, FromEmail = dto.FromEmail, CampaignType = dto.CampaignType, EmailTemplateId = dto.EmailTemplateId, SegmentId = dto.SegmentId, Status = "draft"
        };
        db.EmailCampaigns.Add(entity);
        await db.SaveChangesAsync();
        logger.LogInformation("Created campaign {Name} for {Shop}", dto.Name, shopDomain);
        return MapToDto(entity);
    }

    public async Task<EmailCampaignDto> UpdateCampaignAsync(int campaignId, UpdateEmailCampaignDto dto)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId) ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        if (entity.Status != "draft") throw new InvalidOperationException("Can only update draft campaigns");
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Subject is not null) entity.Subject = dto.Subject;
        if (dto.PreviewText is not null) entity.PreviewText = dto.PreviewText;
        if (dto.Body is not null) entity.Body = dto.Body;
        if (dto.FromName is not null) entity.FromName = dto.FromName;
        if (dto.FromEmail is not null) entity.FromEmail = dto.FromEmail;
        if (dto.SegmentId.HasValue) entity.SegmentId = dto.SegmentId;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteCampaignAsync(int campaignId)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId);
        if (entity is null) return false;
        db.EmailCampaigns.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ScheduleCampaignAsync(int campaignId, DateTime scheduledAt)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId) ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        entity.ScheduledAt = scheduledAt;
        entity.Status = "scheduled";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendCampaignAsync(int campaignId)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId) ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        entity.Status = "sending";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        // TODO: Queue actual email sending
        logger.LogInformation("Started sending campaign {Id}", campaignId);
        return true;
    }

    public async Task<bool> PauseCampaignAsync(int campaignId)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId);
        if (entity is null) return false;
        entity.Status = "paused";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelCampaignAsync(int campaignId)
    {
        var entity = await db.EmailCampaigns.FindAsync(campaignId);
        if (entity is null) return false;
        entity.Status = "cancelled";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<EmailCampaignStatsDto> GetCampaignStatsAsync(int campaignId)
    {
        var c = await db.EmailCampaigns.FindAsync(campaignId) ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        return new EmailCampaignStatsDto
        {
            CampaignId = campaignId, TotalRecipients = c.TotalRecipients, TotalSent = c.TotalSent, TotalDelivered = c.TotalDelivered,
            TotalOpened = c.TotalOpened, TotalClicked = c.TotalClicked, TotalBounced = c.TotalBounced, TotalUnsubscribed = c.TotalUnsubscribed,
            OpenRate = c.TotalSent > 0 ? Math.Round((decimal)c.TotalOpened / c.TotalSent * 100, 2) : 0,
            ClickRate = c.TotalOpened > 0 ? Math.Round((decimal)c.TotalClicked / c.TotalOpened * 100, 2) : 0,
            BounceRate = c.TotalSent > 0 ? Math.Round((decimal)c.TotalBounced / c.TotalSent * 100, 2) : 0
        };
    }

    #endregion

    #region Automations

    public async Task<EmailAutomationDto?> GetAutomationAsync(int automationId)
    {
        var entity = await db.EmailAutomations.Include(a => a.Steps.OrderBy(s => s.StepOrder)).FirstOrDefaultAsync(a => a.Id == automationId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<EmailAutomationDto>> GetAutomationsAsync(string shopDomain)
        => await db.EmailAutomations.AsNoTracking().Include(a => a.Steps).Where(a => a.ShopDomain == shopDomain).OrderBy(a => a.Name).Select(a => MapToDto(a)).ToListAsync();

    public async Task<EmailAutomationDto> CreateAutomationAsync(string shopDomain, CreateEmailAutomationDto dto)
    {
        var entity = new EmailAutomation { ShopDomain = shopDomain, Name = dto.Name, Description = dto.Description, TriggerType = dto.TriggerType, TriggerConditions = dto.TriggerConditions, IsActive = false, Revenue = 0, TotalEnrolled = 0, TotalCompleted = 0 };
        foreach (var s in dto.Steps)
            entity.Steps.Add(new EmailAutomationStep { StepOrder = s.StepOrder, StepType = s.StepType, Subject = s.Subject, Body = s.Body, EmailTemplateId = s.EmailTemplateId, DelayMinutes = s.DelayMinutes, Conditions = s.Conditions, IsActive = true });
        db.EmailAutomations.Add(entity);
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<EmailAutomationDto> UpdateAutomationAsync(int automationId, UpdateEmailAutomationDto dto)
    {
        var entity = await db.EmailAutomations.Include(a => a.Steps).FirstOrDefaultAsync(a => a.Id == automationId) ?? throw new InvalidOperationException($"Automation {automationId} not found");
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.TriggerConditions is not null) entity.TriggerConditions = dto.TriggerConditions;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteAutomationAsync(int automationId)
    {
        var entity = await db.EmailAutomations.FindAsync(automationId);
        if (entity is null) return false;
        db.EmailAutomations.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAutomationAsync(int automationId)
    {
        var entity = await db.EmailAutomations.FindAsync(automationId);
        if (entity is null) return false;
        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAutomationAsync(int automationId)
    {
        var entity = await db.EmailAutomations.FindAsync(automationId);
        if (entity is null) return false;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EnrollInAutomationAsync(int automationId, string email, int? customerId = null)
    {
        var entity = await db.EmailAutomations.Include(a => a.Steps).FirstOrDefaultAsync(a => a.Id == automationId);
        if (entity is null || !entity.IsActive || entity.Steps.Count == 0) return false;
        var firstStep = entity.Steps.OrderBy(s => s.StepOrder).First();
        db.EmailAutomationEnrollments.Add(new EmailAutomationEnrollment
        {
            AutomationId = automationId, Email = email.ToLowerInvariant(), CustomerId = customerId,
            CurrentStepId = firstStep.Id, Status = "active", NextStepAt = DateTime.UtcNow.AddMinutes(firstStep.DelayMinutes)
        });
        entity.TotalEnrolled++;
        await db.SaveChangesAsync();
        logger.LogInformation("Enrolled {Email} in automation {Id}", email, automationId);
        return true;
    }

    #endregion

    #region Templates

    public async Task<EmailTemplateDto?> GetTemplateAsync(int templateId)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId);
        return entity is null ? null : MapTemplateToDto(entity);
    }

    public async Task<IEnumerable<EmailTemplateDto>> GetTemplatesAsync(string shopDomain)
        => await db.EmailTemplates.AsNoTracking()
            .Where(t => t.ShopDomain == shopDomain)
            .OrderBy(t => t.Name)
            .Select(t => MapTemplateToDto(t))
            .ToListAsync();

    public async Task<EmailTemplateDto> CreateTemplateAsync(string shopDomain, CreateEmailTemplateDto dto)
    {
        var entity = new EmailTemplate
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            Subject = dto.Subject,
            Body = dto.Body,
            TemplateType = dto.TemplateType,
            IsActive = true
        };
        db.EmailTemplates.Add(entity);
        await db.SaveChangesAsync();
        logger.LogInformation("Created email template {Name} for {Shop}", dto.Name, shopDomain);
        return MapTemplateToDto(entity);
    }

    public async Task<EmailTemplateDto> UpdateTemplateAsync(int templateId, UpdateEmailTemplateDto dto)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId) ?? throw new InvalidOperationException($"Template {templateId} not found");
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Subject is not null) entity.Subject = dto.Subject;
        if (dto.Body is not null) entity.Body = dto.Body;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapTemplateToDto(entity);
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId);
        if (entity is null) return false;
        db.EmailTemplates.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateTemplateAsync(int templateId)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId);
        if (entity is null) return false;
        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateTemplateAsync(int templateId)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId);
        if (entity is null) return false;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<EmailTemplateDto?> DuplicateTemplateAsync(int templateId)
    {
        var entity = await db.EmailTemplates.FindAsync(templateId);
        if (entity is null) return null;
        var duplicate = new EmailTemplate
        {
            ShopDomain = entity.ShopDomain,
            Name = $"{entity.Name} (Copy)",
            Subject = entity.Subject,
            Body = entity.Body,
            TemplateType = entity.TemplateType,
            IsActive = false
        };
        db.EmailTemplates.Add(duplicate);
        await db.SaveChangesAsync();
        return MapTemplateToDto(duplicate);
    }

    private static EmailTemplateDto MapTemplateToDto(EmailTemplate e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Subject = e.Subject,
        Body = e.Body,
        TemplateType = e.TemplateType,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };

    #endregion

    #region Mappers

    private static EmailSubscriberDto MapToDto(EmailSubscriber e) => new()
    {
        Id = e.Id, ShopDomain = e.ShopDomain, Email = e.Email, FirstName = e.FirstName, LastName = e.LastName, Phone = e.Phone,
        CustomerId = e.CustomerId, Status = e.Status, Source = e.Source, EmailOptIn = e.EmailOptIn, SmsOptIn = e.SmsOptIn,
        WhatsAppOptIn = e.WhatsAppOptIn, ConfirmedAt = e.ConfirmedAt, UnsubscribedAt = e.UnsubscribedAt, Tags = e.Tags, CreatedAt = e.CreatedAt
    };

    private static EmailListDto MapToDto(EmailList e) => new()
    {
        Id = e.Id, ShopDomain = e.ShopDomain, Name = e.Name, Description = e.Description, IsDefault = e.IsDefault,
        IsActive = e.IsActive, DoubleOptIn = e.DoubleOptIn, SubscriberCount = e.SubscriberCount, CreatedAt = e.CreatedAt
    };

    private static CustomerSegmentDto MapToDto(CustomerSegment e) => new()
    {
        Id = e.Id, ShopDomain = e.ShopDomain, Name = e.Name, Description = e.Description, SegmentType = e.SegmentType,
        FilterCriteria = e.FilterCriteria, MemberCount = e.MemberCount, IsActive = e.IsActive, LastCalculatedAt = e.LastCalculatedAt, CreatedAt = e.CreatedAt
    };

    private static EmailCampaignDto MapToDto(EmailCampaign e) => new()
    {
        Id = e.Id, ShopDomain = e.ShopDomain, Name = e.Name, Subject = e.Subject, PreviewText = e.PreviewText,
        FromName = e.FromName, FromEmail = e.FromEmail, CampaignType = e.CampaignType, Status = e.Status,
        SegmentId = e.SegmentId, SegmentName = e.Segment?.Name, ScheduledAt = e.ScheduledAt, SentAt = e.SentAt,
        TotalRecipients = e.TotalRecipients, TotalSent = e.TotalSent, TotalDelivered = e.TotalDelivered,
        TotalOpened = e.TotalOpened, TotalClicked = e.TotalClicked, CreatedAt = e.CreatedAt
    };

    private static EmailAutomationDto MapToDto(EmailAutomation e) => new()
    {
        Id = e.Id, ShopDomain = e.ShopDomain, Name = e.Name, Description = e.Description, TriggerType = e.TriggerType,
        TriggerConditions = e.TriggerConditions, IsActive = e.IsActive, TotalEnrolled = e.TotalEnrolled,
        TotalCompleted = e.TotalCompleted, Revenue = e.Revenue, CreatedAt = e.CreatedAt,
        Steps = e.Steps.Select(s => new EmailAutomationStepDto
        {
            Id = s.Id, StepOrder = s.StepOrder, StepType = s.StepType, Subject = s.Subject, Body = s.Body,
            EmailTemplateId = s.EmailTemplateId, DelayMinutes = s.DelayMinutes, Conditions = s.Conditions, IsActive = s.IsActive
        })
    };

    #endregion
}