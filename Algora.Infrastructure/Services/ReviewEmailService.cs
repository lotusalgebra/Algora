using System.Text.Json;
using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing review email automations
/// </summary>
public class ReviewEmailService : IReviewEmailService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReviewEmailService> _logger;
    private readonly string _baseUrl;

    public ReviewEmailService(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<ReviewEmailService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _baseUrl = "https://app.algora.com"; // TODO: Get from configuration
    }

    #region Email Automations

    public async Task<List<ReviewEmailAutomationListDto>> GetAutomationsAsync(string shopDomain)
    {
        var automations = await _context.ReviewEmailAutomations
            .Where(a => a.ShopDomain == shopDomain)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ReviewEmailAutomationListDto
            {
                Id = a.Id,
                Name = a.Name,
                IsActive = a.IsActive,
                TriggerType = a.TriggerType,
                DelayDays = a.DelayDays,
                TotalSent = a.TotalSent,
                TotalReviewsCollected = a.TotalReviewsCollected,
                ConversionRate = a.TotalSent > 0 ? (decimal)a.TotalReviewsCollected / a.TotalSent * 100 : 0,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return automations;
    }

    public async Task<ReviewEmailAutomationDto?> GetAutomationByIdAsync(int automationId)
    {
        var automation = await _context.ReviewEmailAutomations
            .FirstOrDefaultAsync(a => a.Id == automationId);

        return automation == null ? null : MapToDto(automation);
    }

    public async Task<ReviewEmailAutomationDto> CreateAutomationAsync(string shopDomain, CreateReviewEmailAutomationDto dto)
    {
        var automation = new ReviewEmailAutomation
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            IsActive = dto.IsActive,
            TriggerType = dto.TriggerType,
            DelayDays = dto.DelayDays,
            DelayHours = dto.DelayHours,
            MinOrderValue = dto.MinOrderValue,
            ProductIds = dto.ProductIds != null ? JsonSerializer.Serialize(dto.ProductIds) : null,
            ExcludedProductIds = dto.ExcludedProductIds != null ? JsonSerializer.Serialize(dto.ExcludedProductIds) : null,
            CustomerTags = dto.CustomerTags != null ? JsonSerializer.Serialize(dto.CustomerTags) : null,
            ExcludedCustomerTags = dto.ExcludedCustomerTags != null ? JsonSerializer.Serialize(dto.ExcludedCustomerTags) : null,
            ExcludeRepeatedCustomers = dto.ExcludeRepeatedCustomers,
            RepeatedCustomerExclusionDays = dto.RepeatedCustomerExclusionDays,
            Subject = dto.Subject,
            Body = dto.Body,
            EmailTemplateId = dto.EmailTemplateId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewEmailAutomations.Add(automation);
        await _context.SaveChangesAsync();

        return MapToDto(automation);
    }

    public async Task<ReviewEmailAutomationDto?> UpdateAutomationAsync(int automationId, UpdateReviewEmailAutomationDto dto)
    {
        var automation = await _context.ReviewEmailAutomations.FindAsync(automationId);
        if (automation == null)
            return null;

        automation.Name = dto.Name ?? automation.Name;
        automation.IsActive = dto.IsActive ?? automation.IsActive;
        automation.TriggerType = dto.TriggerType ?? automation.TriggerType;
        automation.DelayDays = dto.DelayDays ?? automation.DelayDays;
        automation.DelayHours = dto.DelayHours ?? automation.DelayHours;
        automation.MinOrderValue = dto.MinOrderValue;
        automation.ProductIds = dto.ProductIds != null ? JsonSerializer.Serialize(dto.ProductIds) : automation.ProductIds;
        automation.ExcludedProductIds = dto.ExcludedProductIds != null ? JsonSerializer.Serialize(dto.ExcludedProductIds) : automation.ExcludedProductIds;
        automation.CustomerTags = dto.CustomerTags != null ? JsonSerializer.Serialize(dto.CustomerTags) : automation.CustomerTags;
        automation.ExcludedCustomerTags = dto.ExcludedCustomerTags != null ? JsonSerializer.Serialize(dto.ExcludedCustomerTags) : automation.ExcludedCustomerTags;
        automation.ExcludeRepeatedCustomers = dto.ExcludeRepeatedCustomers ?? automation.ExcludeRepeatedCustomers;
        automation.RepeatedCustomerExclusionDays = dto.RepeatedCustomerExclusionDays ?? automation.RepeatedCustomerExclusionDays;
        automation.Subject = dto.Subject ?? automation.Subject;
        automation.Body = dto.Body ?? automation.Body;
        automation.EmailTemplateId = dto.EmailTemplateId;
        automation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(automation);
    }

    public async Task<bool> DeleteAutomationAsync(int automationId)
    {
        var automation = await _context.ReviewEmailAutomations.FindAsync(automationId);
        if (automation == null)
            return false;

        _context.ReviewEmailAutomations.Remove(automation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleAutomationActiveAsync(int automationId)
    {
        var automation = await _context.ReviewEmailAutomations.FindAsync(automationId);
        if (automation == null)
            return false;

        automation.IsActive = !automation.IsActive;
        automation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ReviewEmailAutomationDto?> DuplicateAutomationAsync(int automationId)
    {
        var source = await _context.ReviewEmailAutomations.FindAsync(automationId);
        if (source == null)
            return null;

        var copy = new ReviewEmailAutomation
        {
            ShopDomain = source.ShopDomain,
            Name = $"{source.Name} (Copy)",
            IsActive = false,
            TriggerType = source.TriggerType,
            DelayDays = source.DelayDays,
            DelayHours = source.DelayHours,
            MinOrderValue = source.MinOrderValue,
            ProductIds = source.ProductIds,
            ExcludedProductIds = source.ExcludedProductIds,
            CustomerTags = source.CustomerTags,
            ExcludedCustomerTags = source.ExcludedCustomerTags,
            ExcludeRepeatedCustomers = source.ExcludeRepeatedCustomers,
            RepeatedCustomerExclusionDays = source.RepeatedCustomerExclusionDays,
            Subject = source.Subject,
            Body = source.Body,
            EmailTemplateId = source.EmailTemplateId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewEmailAutomations.Add(copy);
        await _context.SaveChangesAsync();

        return MapToDto(copy);
    }

    #endregion

    #region Email Logs

    public async Task<PaginatedResult<ReviewEmailLogDto>> GetEmailLogsAsync(
        string shopDomain,
        int? automationId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ReviewEmailLogs
            .Where(l => l.ShopDomain == shopDomain);

        if (automationId.HasValue)
        {
            query = query.Where(l => l.AutomationId == automationId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ReviewEmailLogDto
            {
                Id = l.Id,
                AutomationId = l.AutomationId,
                OrderId = l.OrderId,
                CustomerId = l.CustomerId,
                CustomerEmail = l.CustomerEmail,
                Status = l.Status,
                ScheduledAt = l.ScheduledAt,
                SentAt = l.SentAt,
                OpenedAt = l.OpenedAt,
                ClickedAt = l.ClickedAt,
                ReviewSubmittedAt = l.ReviewSubmittedAt,
                ReviewId = l.ReviewId,
                ErrorMessage = l.ErrorMessage,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return PaginatedResult<ReviewEmailLogDto>.Create(logs, totalCount, page, pageSize);
    }

    public async Task<ReviewEmailLogDto?> GetEmailLogByIdAsync(int logId)
    {
        var log = await _context.ReviewEmailLogs.FindAsync(logId);
        return log == null ? null : MapLogToDto(log);
    }

    public async Task<bool> CancelScheduledEmailAsync(int logId)
    {
        var log = await _context.ReviewEmailLogs.FindAsync(logId);
        if (log == null || log.Status != "scheduled")
            return false;

        log.Status = "cancelled";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendEmailAsync(int logId)
    {
        var log = await _context.ReviewEmailLogs.FindAsync(logId);
        if (log == null || (log.Status != "failed" && log.Status != "sent"))
            return false;

        log.Status = "scheduled";
        log.ScheduledAt = DateTime.UtcNow;
        log.ErrorMessage = null;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Email Processing

    public async Task ScheduleReviewRequestsAsync(int orderId, string triggerType)
    {
        var order = await _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order not found for email scheduling: {OrderId}", orderId);
            return;
        }

        var automations = await _context.ReviewEmailAutomations
            .Where(a => a.ShopDomain == order.ShopDomain && a.IsActive && a.TriggerType == triggerType)
            .ToListAsync();

        foreach (var automation in automations)
        {
            if (await ShouldScheduleEmailAsync(automation, order))
            {
                await ScheduleEmailAsync(automation, order);
            }
        }
    }

    public async Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var pendingEmails = await _context.ReviewEmailLogs
            .Where(l => l.Status == "scheduled" && l.ScheduledAt <= now)
            .OrderBy(l => l.ScheduledAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var emailLog in pendingEmails)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await SendReviewRequestEmailAsync(emailLog.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send review email {EmailLogId}", emailLog.Id);
            }
        }
    }

    public async Task<bool> SendReviewRequestEmailAsync(int logId)
    {
        var log = await _context.ReviewEmailLogs.FindAsync(logId);
        if (log == null)
            return false;

        var automation = await _context.ReviewEmailAutomations.FindAsync(log.AutomationId);
        if (automation == null || !automation.IsActive)
        {
            log.Status = "cancelled";
            log.ErrorMessage = "Automation is disabled or deleted";
            await _context.SaveChangesAsync();
            return false;
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == log.OrderId);

        if (order == null)
        {
            log.Status = "cancelled";
            log.ErrorMessage = "Order not found";
            await _context.SaveChangesAsync();
            return false;
        }

        var subject = PersonalizeContent(automation.Subject, order, log.TrackingToken!);
        var body = PersonalizeContent(automation.Body, order, log.TrackingToken!);

        try
        {
            await _notificationService.SendEmailAsync(log.ShopDomain, new SendEmailNotificationDto
            {
                ToEmail = log.CustomerEmail,
                ToName = order.Customer?.FirstName,
                Subject = subject,
                Body = body,
                IsHtml = true,
                CustomerId = order.CustomerId,
                OrderId = order.Id
            });

            log.Status = "sent";
            log.SentAt = DateTime.UtcNow;

            automation.TotalSent++;
            automation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sent review email to {Email} for order {OrderId}", log.CustomerEmail, log.OrderId);
            return true;
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            log.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    #endregion

    #region Tracking

    public async Task RecordEmailOpenAsync(string trackingToken)
    {
        var log = await _context.ReviewEmailLogs
            .FirstOrDefaultAsync(l => l.TrackingToken == trackingToken);

        if (log == null) return;

        if (log.OpenedAt == null)
        {
            log.OpenedAt = DateTime.UtcNow;

            var automation = await _context.ReviewEmailAutomations.FindAsync(log.AutomationId);
            if (automation != null)
            {
                automation.TotalOpened++;
                automation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task RecordEmailClickAsync(string trackingToken)
    {
        var log = await _context.ReviewEmailLogs
            .FirstOrDefaultAsync(l => l.TrackingToken == trackingToken);

        if (log == null) return;

        if (log.ClickedAt == null)
        {
            log.ClickedAt = DateTime.UtcNow;

            var automation = await _context.ReviewEmailAutomations.FindAsync(log.AutomationId);
            if (automation != null)
            {
                automation.TotalClicked++;
                automation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task RecordReviewSubmissionAsync(string trackingToken, int reviewId)
    {
        var log = await _context.ReviewEmailLogs
            .FirstOrDefaultAsync(l => l.TrackingToken == trackingToken);

        if (log == null) return;

        if (log.ReviewSubmittedAt == null)
        {
            log.ReviewSubmittedAt = DateTime.UtcNow;
            log.ReviewId = reviewId;

            var automation = await _context.ReviewEmailAutomations.FindAsync(log.AutomationId);
            if (automation != null)
            {
                automation.TotalReviewsCollected++;
                automation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }

    public string GenerateTrackingPixelUrl(string trackingToken)
    {
        return $"{_baseUrl}/api/reviews/track/open/{trackingToken}";
    }

    public string GenerateReviewSubmissionUrl(string trackingToken, long productId)
    {
        return $"{_baseUrl}/review?token={trackingToken}&product={productId}";
    }

    #endregion

    #region Analytics

    public async Task<EmailAutomationAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var automations = await _context.ReviewEmailAutomations
            .Where(a => a.ShopDomain == shopDomain)
            .ToListAsync();

        var logs = await _context.ReviewEmailLogs
            .Where(l => l.ShopDomain == shopDomain && l.CreatedAt >= from && l.CreatedAt <= to)
            .ToListAsync();

        var totalSent = logs.Count(l => l.SentAt.HasValue);
        var totalOpened = logs.Count(l => l.OpenedAt.HasValue);
        var totalClicked = logs.Count(l => l.ClickedAt.HasValue);
        var totalReviewsCollected = logs.Count(l => l.ReviewSubmittedAt.HasValue);

        var automationPerformance = automations.Select(a => new AutomationPerformanceDto
        {
            AutomationId = a.Id,
            AutomationName = a.Name,
            Sent = a.TotalSent,
            Opened = a.TotalOpened,
            Clicked = a.TotalClicked,
            ReviewsCollected = a.TotalReviewsCollected,
            OpenRate = a.TotalSent > 0 ? (decimal)a.TotalOpened / a.TotalSent * 100 : 0,
            ClickRate = a.TotalSent > 0 ? (decimal)a.TotalClicked / a.TotalSent * 100 : 0,
            ConversionRate = a.TotalSent > 0 ? (decimal)a.TotalReviewsCollected / a.TotalSent * 100 : 0
        }).ToList();

        var dailyStats = logs
            .GroupBy(l => l.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyEmailStatsDto
            {
                Date = g.Key,
                Sent = g.Count(l => l.SentAt.HasValue),
                Opened = g.Count(l => l.OpenedAt.HasValue),
                Clicked = g.Count(l => l.ClickedAt.HasValue),
                ReviewsCollected = g.Count(l => l.ReviewSubmittedAt.HasValue)
            })
            .ToList();

        return new EmailAutomationAnalyticsDto
        {
            TotalEmailsSent = totalSent,
            TotalOpened = totalOpened,
            TotalClicked = totalClicked,
            TotalReviewsCollected = totalReviewsCollected,
            OpenRate = totalSent > 0 ? (decimal)totalOpened / totalSent * 100 : 0,
            ClickRate = totalSent > 0 ? (decimal)totalClicked / totalSent * 100 : 0,
            ConversionRate = totalSent > 0 ? (decimal)totalReviewsCollected / totalSent * 100 : 0,
            AutomationPerformance = automationPerformance,
            DailyStats = dailyStats
        };
    }

    #endregion

    #region Private Helpers

    private async Task<bool> ShouldScheduleEmailAsync(ReviewEmailAutomation automation, Order order)
    {
        // Check minimum order value
        if (automation.MinOrderValue.HasValue && order.GrandTotal < automation.MinOrderValue.Value)
        {
            return false;
        }

        // Check product filters
        if (!string.IsNullOrEmpty(automation.ProductIds))
        {
            var allowedIds = JsonSerializer.Deserialize<List<long>>(automation.ProductIds) ?? [];
            var orderProductIds = order.Lines?
                .Where(li => li.PlatformProductId.HasValue)
                .Select(li => li.PlatformProductId!.Value)
                .ToList() ?? [];
            if (!allowedIds.Intersect(orderProductIds).Any())
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(automation.ExcludedProductIds))
        {
            var excludedIds = JsonSerializer.Deserialize<List<long>>(automation.ExcludedProductIds) ?? [];
            var orderProductIds = order.Lines?
                .Where(li => li.PlatformProductId.HasValue)
                .Select(li => li.PlatformProductId!.Value)
                .ToList() ?? [];
            if (excludedIds.Intersect(orderProductIds).Any())
            {
                return false;
            }
        }

        // Check if already scheduled
        var existingLog = await _context.ReviewEmailLogs
            .AnyAsync(l => l.AutomationId == automation.Id && l.OrderId == order.Id);
        if (existingLog)
        {
            return false;
        }

        // Check repeated customer exclusion
        if (automation.ExcludeRepeatedCustomers && order.CustomerId.HasValue)
        {
            var exclusionDays = automation.RepeatedCustomerExclusionDays ?? 30;
            var cutoffDate = DateTime.UtcNow.AddDays(-exclusionDays);

            var recentEmail = await _context.ReviewEmailLogs
                .AnyAsync(l => l.ShopDomain == automation.ShopDomain &&
                    l.CustomerId == order.CustomerId &&
                    l.SentAt > cutoffDate);

            if (recentEmail)
            {
                return false;
            }
        }

        return true;
    }

    private async Task ScheduleEmailAsync(ReviewEmailAutomation automation, Order order)
    {
        // For after_delivery trigger, use order date if fulfillment status is "fulfilled"
        var baseDate = automation.TriggerType == "after_delivery" && order.FulfillmentStatus == "fulfilled"
            ? order.UpdatedAt ?? order.OrderDate
            : order.OrderDate;

        var scheduledAt = baseDate
            .AddDays(automation.DelayDays)
            .AddHours(automation.DelayHours);

        var trackingToken = GenerateTrackingToken();

        var log = new ReviewEmailLog
        {
            ShopDomain = automation.ShopDomain,
            AutomationId = automation.Id,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerEmail = order.Customer?.Email ?? order.CustomerEmail ?? "unknown@example.com",
            Status = "scheduled",
            ScheduledAt = scheduledAt,
            TrackingToken = trackingToken,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewEmailLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Scheduled review email for order {OrderId}, automation {AutomationId}, at {ScheduledAt}",
            order.Id, automation.Id, scheduledAt);
    }

    private static string PersonalizeContent(string content, Order order, string trackingToken)
    {
        return content
            .Replace("{{customer_name}}", order.Customer?.FirstName ?? "Customer")
            .Replace("{{customer_first_name}}", order.Customer?.FirstName ?? "Customer")
            .Replace("{{customer_last_name}}", order.Customer?.LastName ?? "")
            .Replace("{{order_number}}", order.OrderNumber)
            .Replace("{{order_date}}", order.OrderDate.ToString("MMMM dd, yyyy"))
            .Replace("{{tracking_token}}", trackingToken)
            .Replace("{{review_link}}", $"/review?token={trackingToken}");
    }

    private static string GenerateTrackingToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "")
            .Substring(0, 22);
    }

    private static ReviewEmailAutomationDto MapToDto(ReviewEmailAutomation automation)
    {
        return new ReviewEmailAutomationDto
        {
            Id = automation.Id,
            ShopDomain = automation.ShopDomain,
            Name = automation.Name,
            IsActive = automation.IsActive,
            TriggerType = automation.TriggerType,
            DelayDays = automation.DelayDays,
            DelayHours = automation.DelayHours,
            MinOrderValue = automation.MinOrderValue,
            ProductIds = !string.IsNullOrEmpty(automation.ProductIds)
                ? JsonSerializer.Deserialize<List<long>>(automation.ProductIds)
                : null,
            ExcludedProductIds = !string.IsNullOrEmpty(automation.ExcludedProductIds)
                ? JsonSerializer.Deserialize<List<long>>(automation.ExcludedProductIds)
                : null,
            CustomerTags = !string.IsNullOrEmpty(automation.CustomerTags)
                ? JsonSerializer.Deserialize<List<string>>(automation.CustomerTags)
                : null,
            ExcludedCustomerTags = !string.IsNullOrEmpty(automation.ExcludedCustomerTags)
                ? JsonSerializer.Deserialize<List<string>>(automation.ExcludedCustomerTags)
                : null,
            ExcludeRepeatedCustomers = automation.ExcludeRepeatedCustomers,
            RepeatedCustomerExclusionDays = automation.RepeatedCustomerExclusionDays,
            Subject = automation.Subject,
            Body = automation.Body,
            EmailTemplateId = automation.EmailTemplateId,
            TotalSent = automation.TotalSent,
            TotalOpened = automation.TotalOpened,
            TotalClicked = automation.TotalClicked,
            TotalReviewsCollected = automation.TotalReviewsCollected,
            OpenRate = automation.TotalSent > 0 ? (decimal)automation.TotalOpened / automation.TotalSent * 100 : 0,
            ClickRate = automation.TotalSent > 0 ? (decimal)automation.TotalClicked / automation.TotalSent * 100 : 0,
            ConversionRate = automation.TotalSent > 0 ? (decimal)automation.TotalReviewsCollected / automation.TotalSent * 100 : 0,
            CreatedAt = automation.CreatedAt,
            UpdatedAt = automation.UpdatedAt
        };
    }

    private static ReviewEmailLogDto MapLogToDto(ReviewEmailLog log)
    {
        return new ReviewEmailLogDto
        {
            Id = log.Id,
            AutomationId = log.AutomationId,
            OrderId = log.OrderId,
            CustomerId = log.CustomerId,
            CustomerEmail = log.CustomerEmail,
            Status = log.Status,
            ScheduledAt = log.ScheduledAt,
            SentAt = log.SentAt,
            OpenedAt = log.OpenedAt,
            ClickedAt = log.ClickedAt,
            ReviewSubmittedAt = log.ReviewSubmittedAt,
            ReviewId = log.ReviewId,
            ErrorMessage = log.ErrorMessage,
            CreatedAt = log.CreatedAt
        };
    }

    #endregion
}
