using System.Text.Json;
using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Core service for marketing automation orchestration.
/// </summary>
public class MarketingAutomationService : IMarketingAutomationService
{
    private readonly AppDbContext _db;
    private readonly IPersonalizationService _personalization;
    private readonly IABTestService _abTestService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MarketingAutomationService> _logger;

    public MarketingAutomationService(
        AppDbContext db,
        IPersonalizationService personalization,
        IABTestService abTestService,
        INotificationService notificationService,
        ILogger<MarketingAutomationService> logger)
    {
        _db = db;
        _personalization = personalization;
        _abTestService = abTestService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ==================== TRIGGER PROCESSING ====================

    public async Task ProcessAbandonedCartTriggerAsync(string shopDomain, AbandonedCartTriggerDto trigger)
    {
        _logger.LogInformation("Processing abandoned cart trigger for {Email} in {ShopDomain}",
            trigger.Email, shopDomain);

        // Find active abandoned_cart automations for this shop
        var automations = await _db.EmailAutomations
            .Where(a => a.ShopDomain == shopDomain
                && a.TriggerType == "abandoned_cart"
                && a.IsActive)
            .ToListAsync();

        foreach (var automation in automations)
        {
            // Check if already enrolled for this checkout
            var existingEnrollment = await _db.EmailAutomationEnrollments
                .AnyAsync(e => e.AutomationId == automation.Id
                    && e.AbandonedCheckoutId == trigger.CheckoutId
                    && e.Status == "active");

            if (existingEnrollment)
            {
                _logger.LogDebug("Customer already enrolled for checkout {CheckoutId}", trigger.CheckoutId);
                continue;
            }

            var context = new EnrollmentContext(
                CustomerId: null, // Will be looked up
                SubscriberId: null,
                Email: trigger.Email,
                AbandonedCheckoutId: trigger.CheckoutId,
                Metadata: JsonSerializer.Serialize(new
                {
                    CartTotal = trigger.CartTotal,
                    Currency = trigger.Currency,
                    RecoveryUrl = trigger.RecoveryUrl,
                    ItemCount = trigger.LineItems.Count
                })
            );

            await EnrollInAutomationAsync(automation.Id, context);
        }
    }

    public async Task ProcessPostPurchaseTriggerAsync(string shopDomain, PostPurchaseTriggerDto trigger)
    {
        _logger.LogInformation("Processing post-purchase trigger for order {OrderNumber} in {ShopDomain}",
            trigger.OrderNumber, shopDomain);

        // Exit any active abandoned cart enrollments for this email
        var abandonedCartEnrollments = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Where(e => e.Automation.ShopDomain == shopDomain
                && e.Automation.TriggerType == "abandoned_cart"
                && e.Email == trigger.Email
                && e.Status == "active")
            .ToListAsync();

        foreach (var enrollment in abandonedCartEnrollments)
        {
            await ExitAutomationAsync(enrollment.Id, "Customer completed purchase");
        }

        // Find active post_purchase automations
        var automations = await _db.EmailAutomations
            .Where(a => a.ShopDomain == shopDomain
                && a.TriggerType == "post_purchase"
                && a.IsActive)
            .ToListAsync();

        foreach (var automation in automations)
        {
            var context = new EnrollmentContext(
                CustomerId: trigger.CustomerId,
                SubscriberId: null,
                Email: trigger.Email,
                OrderId: trigger.OrderId,
                Metadata: JsonSerializer.Serialize(new
                {
                    OrderNumber = trigger.OrderNumber,
                    OrderTotal = trigger.OrderTotal,
                    Currency = trigger.Currency
                })
            );

            await EnrollInAutomationAsync(automation.Id, context);
        }
    }

    public async Task ProcessWelcomeTriggerAsync(string shopDomain, WelcomeTriggerDto trigger)
    {
        _logger.LogInformation("Processing welcome trigger for customer {Email} in {ShopDomain}",
            trigger.Email, shopDomain);

        var automations = await _db.EmailAutomations
            .Where(a => a.ShopDomain == shopDomain
                && a.TriggerType == "welcome"
                && a.IsActive)
            .ToListAsync();

        foreach (var automation in automations)
        {
            var context = new EnrollmentContext(
                CustomerId: trigger.CustomerId,
                SubscriberId: null,
                Email: trigger.Email
            );

            await EnrollInAutomationAsync(automation.Id, context);
        }
    }

    public async Task<int> ProcessWinbackTriggersAsync(string shopDomain, CancellationToken cancellationToken = default)
    {
        var rules = await _db.WinbackRules
            .Include(r => r.Automation)
            .Where(r => r.ShopDomain == shopDomain && r.IsActive)
            .ToListAsync(cancellationToken);

        var totalEnrolled = 0;

        foreach (var rule in rules)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var ruleDto = MapToWinbackRuleDto(rule);
            var inactiveCustomers = await DetectInactiveCustomersAsync(shopDomain, ruleDto);

            foreach (var customer in inactiveCustomers)
            {
                // Check if already enrolled
                var alreadyEnrolled = await _db.EmailAutomationEnrollments
                    .AnyAsync(e => e.AutomationId == rule.AutomationId
                        && e.CustomerId == customer.CustomerId
                        && (e.Status == "active" || e.CompletedAt > DateTime.UtcNow.AddDays(-30)),
                        cancellationToken);

                if (!alreadyEnrolled)
                {
                    var context = new EnrollmentContext(
                        CustomerId: customer.CustomerId,
                        SubscriberId: null,
                        Email: customer.Email
                    );

                    await EnrollInAutomationAsync(rule.AutomationId, context);
                    totalEnrolled++;
                }
            }

            rule.LastRunAt = DateTime.UtcNow;
            rule.CustomersEnrolledLastRun = inactiveCustomers.Count;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Win-back triggers processed for {ShopDomain}: {Count} customers enrolled",
            shopDomain, totalEnrolled);

        return totalEnrolled;
    }

    // ==================== ENROLLMENT MANAGEMENT ====================

    public async Task<int?> EnrollInAutomationAsync(int automationId, EnrollmentContext context)
    {
        var automation = await _db.EmailAutomations
            .Include(a => a.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(a => a.Id == automationId);

        if (automation == null || !automation.IsActive)
            return null;

        var firstStep = automation.Steps.FirstOrDefault();
        if (firstStep == null)
        {
            _logger.LogWarning("Automation {AutomationId} has no steps", automationId);
            return null;
        }

        // Look up customer if not provided
        int? customerId = context.CustomerId;
        if (!customerId.HasValue && !string.IsNullOrEmpty(context.Email))
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.ShopDomain == automation.ShopDomain && c.Email == context.Email);
            customerId = customer?.Id;
        }

        var enrollment = new EmailAutomationEnrollment
        {
            AutomationId = automationId,
            CustomerId = customerId,
            SubscriberId = context.SubscriberId,
            Email = context.Email,
            CurrentStepId = firstStep.Id,
            Status = "active",
            NextStepAt = DateTime.UtcNow.AddMinutes(firstStep.DelayMinutes),
            EnrolledAt = DateTime.UtcNow,
            AbandonedCheckoutId = context.AbandonedCheckoutId,
            OrderId = context.OrderId,
            Metadata = context.Metadata
        };

        _db.EmailAutomationEnrollments.Add(enrollment);

        // Update automation stats
        automation.TotalEnrolled++;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Enrolled {Email} in automation {AutomationName} (ID: {EnrollmentId})",
            context.Email, automation.Name, enrollment.Id);

        return enrollment.Id;
    }

    public async Task<bool> ExitAutomationAsync(int enrollmentId, string reason)
    {
        var enrollment = await _db.EmailAutomationEnrollments.FindAsync(enrollmentId);
        if (enrollment == null || enrollment.Status != "active")
            return false;

        enrollment.Status = "exited";
        enrollment.ExitedAt = DateTime.UtcNow;
        enrollment.ExitReason = reason;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Exited enrollment {EnrollmentId}: {Reason}", enrollmentId, reason);
        return true;
    }

    public async Task<AutomationEnrollmentDto?> GetEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Include(e => e.Customer)
            .Include(e => e.Order)
            .Include(e => e.StepLogs)
                .ThenInclude(sl => sl.Step)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
            return null;

        var currentStep = await _db.EmailAutomationSteps
            .FirstOrDefaultAsync(s => s.Id == enrollment.CurrentStepId);

        return MapToEnrollmentDto(enrollment, currentStep);
    }

    public async Task<(List<AutomationEnrollmentDto> Enrollments, int TotalCount)> GetEnrollmentsAsync(
        int automationId,
        string? statusFilter = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Include(e => e.Customer)
            .Include(e => e.Order)
            .Include(e => e.StepLogs)
                .ThenInclude(sl => sl.Step)
            .Where(e => e.AutomationId == automationId);

        if (!string.IsNullOrEmpty(statusFilter))
            query = query.Where(e => e.Status == statusFilter);

        var totalCount = await query.CountAsync();

        var enrollments = await query
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var stepIds = enrollments.Select(e => e.CurrentStepId).Distinct().ToList();
        var steps = await _db.EmailAutomationSteps
            .Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var dtos = enrollments.Select(e =>
        {
            steps.TryGetValue(e.CurrentStepId, out var step);
            return MapToEnrollmentDto(e, step);
        }).ToList();

        return (dtos, totalCount);
    }

    // ==================== STEP EXECUTION ====================

    public async Task<int> ProcessPendingStepsAsync(string shopDomain, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var processed = 0;

        var pendingEnrollments = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Where(e => e.Automation.ShopDomain == shopDomain
                && e.Status == "active"
                && e.NextStepAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var enrollment in pendingEnrollments)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var success = await ExecuteStepAsync(enrollment.Id, enrollment.CurrentStepId);
                if (success)
                {
                    await AdvanceToNextStepAsync(enrollment.Id);
                    processed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step for enrollment {EnrollmentId}", enrollment.Id);
            }
        }

        return processed;
    }

    public async Task<bool> ExecuteStepAsync(int enrollmentId, int stepId)
    {
        var enrollment = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null || enrollment.Status != "active")
            return false;

        var step = await _db.EmailAutomationSteps
            .Include(s => s.EmailTemplate)
            .FirstOrDefaultAsync(s => s.Id == stepId);

        if (step == null || !step.IsActive)
            return false;

        var log = new AutomationStepLog
        {
            EnrollmentId = enrollmentId,
            StepId = stepId,
            Status = "pending",
            ScheduledAt = enrollment.NextStepAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.AutomationStepLogs.Add(log);

        try
        {
            switch (step.StepType.ToLowerInvariant())
            {
                case "email":
                    await ExecuteEmailStepAsync(enrollment, step, log);
                    break;

                case "sms":
                    await ExecuteSmsStepAsync(enrollment, step, log);
                    break;

                case "delay":
                    // Delay steps just wait - nothing to execute
                    log.Status = "sent";
                    log.ExecutedAt = DateTime.UtcNow;
                    break;

                case "condition":
                    // Evaluate condition and potentially skip
                    var shouldContinue = await EvaluateConditionAsync(enrollment, step);
                    log.Status = shouldContinue ? "sent" : "skipped";
                    log.ExecutedAt = DateTime.UtcNow;
                    break;

                default:
                    _logger.LogWarning("Unknown step type: {StepType}", step.StepType);
                    log.Status = "failed";
                    log.ErrorMessage = $"Unknown step type: {step.StepType}";
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error executing step {StepId} for enrollment {EnrollmentId}",
                stepId, enrollmentId);
        }

        await _db.SaveChangesAsync();
        return log.Status == "sent";
    }

    private async Task ExecuteEmailStepAsync(
        EmailAutomationEnrollment enrollment,
        EmailAutomationStep step,
        AutomationStepLog log)
    {
        log.Channel = "email";

        var context = await _personalization.BuildContextForEnrollmentAsync(enrollment.Id);

        string subject;
        string body;

        // Check for A/B testing
        if (step.IsABTestEnabled)
        {
            var variant = await _abTestService.AssignVariantAsync(
                enrollment.Id, step.AutomationId, step.Id);

            if (variant != null)
            {
                subject = variant.Subject ?? step.Subject ?? "";
                body = variant.Body ?? step.Body ?? "";
                enrollment.ABTestVariantId = variant.Id;
            }
            else
            {
                subject = step.Subject ?? "";
                body = step.Body ?? "";
            }
        }
        else
        {
            subject = step.Subject ?? "";
            body = step.EmailTemplate?.Body ?? step.Body ?? "";
        }

        // Personalize content
        subject = await _personalization.PersonalizeContentAsync(subject, context);
        body = await _personalization.PersonalizeContentAsync(body, context);

        // Send email via notification service
        var emailDto = new SendEmailNotificationDto
        {
            ToEmail = enrollment.Email,
            Subject = subject,
            Body = body,
            IsHtml = true,
            CustomerId = enrollment.CustomerId,
            OrderId = enrollment.OrderId
        };

        var notification = await _notificationService.SendEmailAsync(enrollment.Automation.ShopDomain, emailDto);

        log.Status = notification.Status == "sent" ? "sent" : "failed";
        log.ExecutedAt = DateTime.UtcNow;
        log.ExternalMessageId = notification.Id.ToString();

        if (notification.Status != "sent")
        {
            log.ErrorMessage = notification.ErrorMessage;
        }
    }

    private async Task ExecuteSmsStepAsync(
        EmailAutomationEnrollment enrollment,
        EmailAutomationStep step,
        AutomationStepLog log)
    {
        log.Channel = "sms";

        // Get customer phone number
        var customer = enrollment.Customer ?? await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == enrollment.CustomerId);

        if (customer?.Phone == null)
        {
            log.Status = "skipped";
            log.ErrorMessage = "No phone number available";
            return;
        }

        var context = await _personalization.BuildContextForEnrollmentAsync(enrollment.Id);
        var body = await _personalization.PersonalizeContentAsync(step.SmsBody ?? "", context);

        // Send SMS via notification service
        var smsDto = new SendSmsNotificationDto
        {
            PhoneNumber = customer.Phone,
            Body = body,
            CustomerId = enrollment.CustomerId,
            OrderId = enrollment.OrderId
        };

        var notification = await _notificationService.SendSmsAsync(enrollment.Automation.ShopDomain, smsDto);

        log.Status = notification.Status == "sent" ? "sent" : "failed";
        log.ExecutedAt = DateTime.UtcNow;
        log.ExternalMessageId = notification.Id.ToString();

        if (notification.Status != "sent")
        {
            log.ErrorMessage = notification.ErrorMessage;
        }
    }

    private Task<bool> EvaluateConditionAsync(EmailAutomationEnrollment enrollment, EmailAutomationStep step)
    {
        // Basic condition evaluation - can be extended
        if (string.IsNullOrEmpty(step.Conditions))
            return Task.FromResult(true);

        // For now, always continue. Real implementation would parse and evaluate conditions
        return Task.FromResult(true);
    }

    public async Task<bool> AdvanceToNextStepAsync(int enrollmentId)
    {
        var enrollment = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
                .ThenInclude(a => a.Steps)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null || enrollment.Status != "active")
            return false;

        var steps = enrollment.Automation.Steps.OrderBy(s => s.StepOrder).ToList();
        var currentStepIndex = steps.FindIndex(s => s.Id == enrollment.CurrentStepId);

        if (currentStepIndex < 0 || currentStepIndex >= steps.Count - 1)
        {
            // No more steps - mark as completed
            enrollment.Status = "completed";
            enrollment.CompletedAt = DateTime.UtcNow;

            enrollment.Automation.TotalCompleted++;

            _logger.LogInformation("Enrollment {EnrollmentId} completed automation {AutomationName}",
                enrollmentId, enrollment.Automation.Name);
        }
        else
        {
            // Move to next step
            var nextStep = steps[currentStepIndex + 1];
            enrollment.CurrentStepId = nextStep.Id;
            enrollment.NextStepAt = DateTime.UtcNow.AddMinutes(nextStep.DelayMinutes);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ==================== ANALYTICS ====================

    public async Task<AutomationAnalyticsDto?> GetAutomationAnalyticsAsync(int automationId)
    {
        var automation = await _db.EmailAutomations
            .FirstOrDefaultAsync(a => a.Id == automationId);

        if (automation == null)
            return null;

        var enrollmentStats = await _db.EmailAutomationEnrollments
            .Where(e => e.AutomationId == automationId)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var stepLogs = await _db.AutomationStepLogs
            .Include(sl => sl.Step)
            .Where(sl => sl.Enrollment.AutomationId == automationId)
            .ToListAsync();

        var emailLogs = stepLogs.Where(sl => sl.Channel == "email").ToList();
        var smsLogs = stepLogs.Where(sl => sl.Channel == "sms").ToList();

        var stepAnalytics = await GetStepAnalyticsAsync(automationId);

        return new AutomationAnalyticsDto(
            AutomationId: automation.Id,
            AutomationName: automation.Name,
            TriggerType: automation.TriggerType,
            TotalEnrolled: automation.TotalEnrolled,
            ActiveEnrollments: enrollmentStats.FirstOrDefault(s => s.Status == "active")?.Count ?? 0,
            CompletedEnrollments: enrollmentStats.FirstOrDefault(s => s.Status == "completed")?.Count ?? 0,
            ExitedEnrollments: enrollmentStats.FirstOrDefault(s => s.Status == "exited")?.Count ?? 0,
            Revenue: automation.Revenue ?? 0,
            RevenuePerEnrollment: automation.TotalEnrolled > 0 ? (automation.Revenue ?? 0) / automation.TotalEnrolled : 0,
            TotalEmailsSent: emailLogs.Count(sl => sl.Status == "sent"),
            TotalSMSSent: smsLogs.Count(sl => sl.Status == "sent"),
            TotalWhatsAppSent: stepLogs.Count(sl => sl.Channel == "whatsapp" && sl.Status == "sent"),
            OpenRate: CalculateRate(emailLogs.Count(sl => sl.OpenedAt != null), emailLogs.Count(sl => sl.Status == "sent")),
            ClickRate: CalculateRate(emailLogs.Count(sl => sl.ClickedAt != null), emailLogs.Count(sl => sl.OpenedAt != null)),
            ConversionRate: CalculateRate(automation.TotalCompleted, automation.TotalEnrolled),
            StepAnalytics: stepAnalytics
        );
    }

    public async Task<List<StepAnalyticsDto>> GetStepAnalyticsAsync(int automationId)
    {
        var steps = await _db.EmailAutomationSteps
            .Where(s => s.AutomationId == automationId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        var stepLogs = await _db.AutomationStepLogs
            .Where(sl => sl.Step.AutomationId == automationId)
            .ToListAsync();

        return steps.Select(step =>
        {
            var logs = stepLogs.Where(sl => sl.StepId == step.Id).ToList();
            var sent = logs.Count(l => l.Status == "sent");
            var delivered = logs.Count(l => l.DeliveredAt != null);
            var opens = logs.Count(l => l.OpenedAt != null);
            var clicks = logs.Count(l => l.ClickedAt != null);
            var bounced = logs.Count(l => l.BouncedAt != null);
            var unsubscribed = logs.Count(l => l.UnsubscribedAt != null);

            return new StepAnalyticsDto(
                StepId: step.Id,
                StepOrder: step.StepOrder,
                StepType: step.StepType,
                Subject: step.Subject,
                Sent: sent,
                Delivered: delivered,
                Opens: opens,
                Clicks: clicks,
                Bounced: bounced,
                Unsubscribed: unsubscribed,
                OpenRate: CalculateRate(opens, sent),
                ClickRate: CalculateRate(clicks, opens),
                BounceRate: CalculateRate(bounced, sent)
            );
        }).ToList();
    }

    private static decimal CalculateRate(int numerator, int denominator)
    {
        return denominator > 0 ? Math.Round((decimal)numerator / denominator * 100, 2) : 0;
    }

    // ==================== WIN-BACK RULES ====================

    public async Task<List<WinbackRuleDto>> GetWinbackRulesAsync(string shopDomain)
    {
        var rules = await _db.WinbackRules
            .Include(r => r.Automation)
            .Where(r => r.ShopDomain == shopDomain)
            .ToListAsync();

        return rules.Select(MapToWinbackRuleDto).ToList();
    }

    public async Task<WinbackRuleDto?> CreateWinbackRuleAsync(string shopDomain, CreateWinbackRuleDto dto)
    {
        var rule = new WinbackRule
        {
            ShopDomain = shopDomain,
            AutomationId = dto.AutomationId,
            Name = dto.Name,
            DaysInactive = dto.DaysInactive,
            MinimumLifetimeValue = dto.MinimumLifetimeValue,
            MinimumOrders = dto.MinimumOrders,
            MaximumOrders = dto.MaximumOrders,
            ExcludeRecentSubscribers = dto.ExcludeRecentSubscribers,
            ExcludeSubscribedWithinDays = dto.ExcludeSubscribedWithinDays,
            CustomerTags = dto.CustomerTags != null ? JsonSerializer.Serialize(dto.CustomerTags) : null,
            ExcludeTags = dto.ExcludeTags != null ? JsonSerializer.Serialize(dto.ExcludeTags) : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.WinbackRules.Add(rule);
        await _db.SaveChangesAsync();

        rule.Automation = await _db.EmailAutomations.FindAsync(dto.AutomationId);
        return MapToWinbackRuleDto(rule);
    }

    public async Task<bool> UpdateWinbackRuleAsync(int ruleId, CreateWinbackRuleDto dto)
    {
        var rule = await _db.WinbackRules.FindAsync(ruleId);
        if (rule == null)
            return false;

        rule.AutomationId = dto.AutomationId;
        rule.Name = dto.Name;
        rule.DaysInactive = dto.DaysInactive;
        rule.MinimumLifetimeValue = dto.MinimumLifetimeValue;
        rule.MinimumOrders = dto.MinimumOrders;
        rule.MaximumOrders = dto.MaximumOrders;
        rule.ExcludeRecentSubscribers = dto.ExcludeRecentSubscribers;
        rule.ExcludeSubscribedWithinDays = dto.ExcludeSubscribedWithinDays;
        rule.CustomerTags = dto.CustomerTags != null ? JsonSerializer.Serialize(dto.CustomerTags) : null;
        rule.ExcludeTags = dto.ExcludeTags != null ? JsonSerializer.Serialize(dto.ExcludeTags) : null;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWinbackRuleAsync(int ruleId)
    {
        var rule = await _db.WinbackRules.FindAsync(ruleId);
        if (rule == null)
            return false;

        _db.WinbackRules.Remove(rule);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<InactiveCustomerDto>> DetectInactiveCustomersAsync(string shopDomain, WinbackRuleDto rule)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-rule.DaysInactive);

        // Get customers with their order stats
        var customerStats = await _db.Customers
            .Where(c => c.ShopDomain == shopDomain && c.Email != null)
            .Select(c => new
            {
                Customer = c,
                OrderCount = c.Orders.Count,
                TotalSpent = c.Orders.Sum(o => o.GrandTotal),
                LastOrderDate = c.Orders.Any() ? c.Orders.Max(o => o.OrderDate) : (DateTime?)null
            })
            .Where(cs => cs.OrderCount > 0) // Has at least one order
            .ToListAsync();

        var result = new List<InactiveCustomerDto>();

        foreach (var cs in customerStats)
        {
            // Apply filters
            if (rule.MinimumLifetimeValue.HasValue && cs.TotalSpent < rule.MinimumLifetimeValue)
                continue;

            if (rule.MinimumOrders.HasValue && cs.OrderCount < rule.MinimumOrders)
                continue;

            if (rule.MaximumOrders.HasValue && cs.OrderCount > rule.MaximumOrders)
                continue;

            // Check if inactive
            if (cs.LastOrderDate.HasValue && cs.LastOrderDate.Value < cutoffDate)
            {
                var daysSinceLastOrder = (int)(DateTime.UtcNow - cs.LastOrderDate.Value).TotalDays;

                result.Add(new InactiveCustomerDto(
                    CustomerId: cs.Customer.Id,
                    Email: cs.Customer.Email!,
                    FirstName: cs.Customer.FirstName,
                    LastName: cs.Customer.LastName,
                    DaysSinceLastOrder: daysSinceLastOrder,
                    TotalOrders: cs.OrderCount,
                    TotalSpent: cs.TotalSpent,
                    LastOrderDate: cs.LastOrderDate
                ));
            }
        }

        return result;
    }

    // ==================== WEBHOOK EVENT TRACKING ====================

    public async Task TrackEmailOpenedAsync(int stepLogId)
    {
        var log = await _db.AutomationStepLogs
            .Include(sl => sl.Enrollment)
            .FirstOrDefaultAsync(sl => sl.Id == stepLogId);

        if (log != null && log.OpenedAt == null)
        {
            log.OpenedAt = DateTime.UtcNow;

            // Update A/B test tracking if applicable
            if (log.Enrollment.ABTestVariantId.HasValue)
            {
                await _abTestService.RecordOpenAsync(log.EnrollmentId, log.Enrollment.ABTestVariantId.Value);
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task TrackEmailClickedAsync(int stepLogId)
    {
        var log = await _db.AutomationStepLogs
            .Include(sl => sl.Enrollment)
            .FirstOrDefaultAsync(sl => sl.Id == stepLogId);

        if (log != null && log.ClickedAt == null)
        {
            log.ClickedAt = DateTime.UtcNow;

            if (log.Enrollment.ABTestVariantId.HasValue)
            {
                await _abTestService.RecordClickAsync(log.EnrollmentId, log.Enrollment.ABTestVariantId.Value);
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task TrackEmailDeliveredAsync(string externalMessageId)
    {
        var log = await _db.AutomationStepLogs
            .FirstOrDefaultAsync(sl => sl.ExternalMessageId == externalMessageId);

        if (log != null && log.DeliveredAt == null)
        {
            log.DeliveredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task TrackEmailBouncedAsync(string externalMessageId)
    {
        var log = await _db.AutomationStepLogs
            .FirstOrDefaultAsync(sl => sl.ExternalMessageId == externalMessageId);

        if (log != null && log.BouncedAt == null)
        {
            log.BouncedAt = DateTime.UtcNow;
            log.Status = "bounced";
            await _db.SaveChangesAsync();
        }
    }

    public async Task TrackConversionAsync(int enrollmentId, decimal conversionValue)
    {
        var enrollment = await _db.EmailAutomationEnrollments
            .Include(e => e.Automation)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment != null)
        {
            enrollment.Automation.Revenue = (enrollment.Automation.Revenue ?? 0) + conversionValue;

            if (enrollment.ABTestVariantId.HasValue)
            {
                await _abTestService.RecordConversionAsync(
                    enrollmentId, enrollment.ABTestVariantId.Value, conversionValue);
            }

            await _db.SaveChangesAsync();
        }
    }

    // ==================== MAPPING HELPERS ====================

    private WinbackRuleDto MapToWinbackRuleDto(WinbackRule rule)
    {
        return new WinbackRuleDto(
            Id: rule.Id,
            ShopDomain: rule.ShopDomain,
            AutomationId: rule.AutomationId,
            AutomationName: rule.Automation?.Name ?? "",
            Name: rule.Name,
            DaysInactive: rule.DaysInactive,
            MinimumLifetimeValue: rule.MinimumLifetimeValue,
            MinimumOrders: rule.MinimumOrders,
            MaximumOrders: rule.MaximumOrders,
            ExcludeRecentSubscribers: rule.ExcludeRecentSubscribers,
            ExcludeSubscribedWithinDays: rule.ExcludeSubscribedWithinDays,
            CustomerTags: string.IsNullOrEmpty(rule.CustomerTags) ? null : JsonSerializer.Deserialize<List<string>>(rule.CustomerTags),
            ExcludeTags: string.IsNullOrEmpty(rule.ExcludeTags) ? null : JsonSerializer.Deserialize<List<string>>(rule.ExcludeTags),
            IsActive: rule.IsActive,
            LastRunAt: rule.LastRunAt,
            CustomersEnrolledLastRun: rule.CustomersEnrolledLastRun
        );
    }

    private AutomationEnrollmentDto MapToEnrollmentDto(EmailAutomationEnrollment enrollment, EmailAutomationStep? currentStep)
    {
        return new AutomationEnrollmentDto(
            Id: enrollment.Id,
            AutomationId: enrollment.AutomationId,
            AutomationName: enrollment.Automation.Name,
            CustomerId: enrollment.CustomerId,
            CustomerName: enrollment.Customer != null
                ? $"{enrollment.Customer.FirstName} {enrollment.Customer.LastName}".Trim()
                : null,
            Email: enrollment.Email,
            CurrentStepId: enrollment.CurrentStepId,
            CurrentStepName: currentStep?.Subject ?? currentStep?.StepType ?? "Unknown",
            Status: enrollment.Status,
            NextStepAt: enrollment.NextStepAt,
            EnrolledAt: enrollment.EnrolledAt,
            CompletedAt: enrollment.CompletedAt,
            ExitedAt: enrollment.ExitedAt,
            ExitReason: enrollment.ExitReason,
            AbandonedCheckoutId: enrollment.AbandonedCheckoutId,
            OrderId: enrollment.OrderId,
            OrderNumber: enrollment.Order?.OrderNumber,
            StepLogs: enrollment.StepLogs.Select(sl => new AutomationStepLogDto(
                Id: sl.Id,
                EnrollmentId: sl.EnrollmentId,
                StepId: sl.StepId,
                StepName: sl.Step?.Subject ?? sl.Step?.StepType ?? "Unknown",
                Status: sl.Status,
                Channel: sl.Channel,
                ExternalMessageId: sl.ExternalMessageId,
                ErrorMessage: sl.ErrorMessage,
                ScheduledAt: sl.ScheduledAt,
                ExecutedAt: sl.ExecutedAt,
                DeliveredAt: sl.DeliveredAt,
                OpenedAt: sl.OpenedAt,
                ClickedAt: sl.ClickedAt,
                BouncedAt: sl.BouncedAt,
                UnsubscribedAt: sl.UnsubscribedAt
            )).ToList()
        );
    }
}
