using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing inventory alerts and notifications.
/// </summary>
public class InventoryAlertService : IInventoryAlertService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InventoryAlertService> _logger;

    public InventoryAlertService(
        AppDbContext db,
        INotificationService notificationService,
        ILogger<InventoryAlertService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> GenerateAlertsAsync(string shopDomain)
    {
        _logger.LogInformation("Generating inventory alerts for {Shop}", shopDomain);

        var settings = await GetOrCreateSettingsAsync(shopDomain);
        if (!settings.AlertsEnabled)
        {
            _logger.LogInformation("Alerts disabled for {Shop}", shopDomain);
            return 0;
        }

        // Get all predictions that need alerts
        var predictions = await _db.InventoryPredictions
            .Where(p => p.ShopDomain == shopDomain)
            .Where(p => p.Status != "ok")
            .ToListAsync();

        var alertsGenerated = 0;

        foreach (var prediction in predictions)
        {
            try
            {
                // Check if we already have a recent active alert for this prediction
                var existingAlert = await _db.InventoryAlerts
                    .Where(a => a.InventoryPredictionId == prediction.Id)
                    .Where(a => a.Status == "active")
                    .Where(a => a.CreatedAt > DateTime.UtcNow.AddHours(-settings.MinHoursBetweenAlerts))
                    .FirstOrDefaultAsync();

                if (existingAlert != null)
                    continue;

                var alert = CreateAlertFromPrediction(prediction, settings);
                _db.InventoryAlerts.Add(alert);
                alertsGenerated++;

                _logger.LogInformation(
                    "Created {AlertType} alert for product {ProductTitle} ({Sku})",
                    alert.AlertType, alert.ProductTitle, alert.Sku);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert for prediction {PredictionId}", prediction.Id);
            }
        }

        if (alertsGenerated > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} alerts for {Shop}", alertsGenerated, shopDomain);
        return alertsGenerated;
    }

    public async Task<int> SendPendingNotificationsAsync(string shopDomain)
    {
        _logger.LogInformation("Sending pending notifications for {Shop}", shopDomain);

        var settings = await GetOrCreateSettingsAsync(shopDomain);
        if (!settings.AlertsEnabled)
            return 0;

        // Get alerts that need notifications sent
        var pendingAlerts = await _db.InventoryAlerts
            .Where(a => a.ShopDomain == shopDomain)
            .Where(a => a.Status == "active")
            .Where(a => !a.EmailSent && settings.EmailNotificationsEnabled)
            .ToListAsync();

        var notificationsSent = 0;

        foreach (var alert in pendingAlerts)
        {
            try
            {
                // Send email notification
                if (settings.EmailNotificationsEnabled && !string.IsNullOrEmpty(settings.NotificationEmail) && !alert.EmailSent)
                {
                    await SendEmailAlertAsync(settings.NotificationEmail, alert, shopDomain);
                    alert.EmailSent = true;
                    alert.EmailSentAt = DateTime.UtcNow;
                    notificationsSent++;
                }

                alert.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for alert {AlertId}", alert.Id);
            }
        }

        if (notificationsSent > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation("Sent {Count} notifications for {Shop}", notificationsSent, shopDomain);
        return notificationsSent;
    }

    public async Task<PaginatedResult<InventoryAlertDto>> GetAlertsAsync(
        string shopDomain,
        string? status = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.InventoryAlerts
            .Where(a => a.ShopDomain == shopDomain)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<InventoryAlertDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Dictionary<string, int>> GetAlertCountsBySeverityAsync(string shopDomain)
    {
        var counts = await _db.InventoryAlerts
            .Where(a => a.ShopDomain == shopDomain)
            .Where(a => a.Status == "active")
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count);

        return counts;
    }

    public async Task AcknowledgeAlertAsync(int alertId)
    {
        var alert = await _db.InventoryAlerts.FindAsync(alertId);
        if (alert == null)
            throw new InvalidOperationException($"Alert {alertId} not found");

        alert.Status = "acknowledged";
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Alert {AlertId} acknowledged", alertId);
    }

    public async Task DismissAlertAsync(int alertId, string? reason = null)
    {
        var alert = await _db.InventoryAlerts.FindAsync(alertId);
        if (alert == null)
            throw new InvalidOperationException($"Alert {alertId} not found");

        alert.Status = "dismissed";
        alert.DismissReason = reason;
        alert.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Alert {AlertId} dismissed: {Reason}", alertId, reason ?? "No reason");
    }

    public async Task ResolveAlertAsync(int alertId)
    {
        var alert = await _db.InventoryAlerts.FindAsync(alertId);
        if (alert == null)
            throw new InvalidOperationException($"Alert {alertId} not found");

        alert.Status = "resolved";
        alert.ResolvedAt = DateTime.UtcNow;
        alert.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Alert {AlertId} resolved", alertId);
    }

    public async Task<InventoryAlertSettingsDto> GetSettingsAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        return MapSettingsToDto(settings);
    }

    public async Task<InventoryAlertSettingsDto> UpdateSettingsAsync(
        string shopDomain,
        UpdateInventoryAlertSettingsDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        settings.AlertsEnabled = dto.AlertsEnabled;
        settings.LowStockDaysThreshold = dto.LowStockDaysThreshold;
        settings.CriticalStockDaysThreshold = dto.CriticalStockDaysThreshold;
        settings.DefaultLowStockQuantity = dto.DefaultLowStockQuantity;
        settings.DefaultCriticalStockQuantity = dto.DefaultCriticalStockQuantity;
        settings.DefaultLeadTimeDays = dto.DefaultLeadTimeDays;
        settings.DefaultSafetyStockDays = dto.DefaultSafetyStockDays;
        settings.EmailNotificationsEnabled = dto.EmailNotificationsEnabled;
        settings.NotificationEmail = dto.NotificationEmail;
        settings.SmsNotificationsEnabled = dto.SmsNotificationsEnabled;
        settings.NotificationPhone = dto.NotificationPhone;
        settings.WhatsAppNotificationsEnabled = dto.WhatsAppNotificationsEnabled;
        settings.WhatsAppPhone = dto.WhatsAppPhone;
        settings.MinHoursBetweenAlerts = dto.MinHoursBetweenAlerts;
        settings.DailyDigestEnabled = dto.DailyDigestEnabled;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated inventory alert settings for {Shop}", shopDomain);

        return MapSettingsToDto(settings);
    }

    private InventoryAlert CreateAlertFromPrediction(InventoryPrediction prediction, InventoryAlertSettings settings)
    {
        var alertType = prediction.Status switch
        {
            "out_of_stock" => "out_of_stock",
            "critical" => "critical_stock",
            "low_stock" => "low_stock",
            _ => "stockout_warning"
        };

        var severity = prediction.Status switch
        {
            "out_of_stock" => "critical",
            "critical" => "high",
            "low_stock" => "medium",
            _ => "low"
        };

        var message = prediction.Status switch
        {
            "out_of_stock" => $"{prediction.ProductTitle} is out of stock!",
            "critical" => $"{prediction.ProductTitle} has only {prediction.DaysUntilStockout} days of stock remaining.",
            "low_stock" => $"{prediction.ProductTitle} is running low - {prediction.DaysUntilStockout} days until stockout.",
            _ => $"{prediction.ProductTitle} may need attention."
        };

        return new InventoryAlert
        {
            ShopDomain = prediction.ShopDomain,
            InventoryPredictionId = prediction.Id,
            PlatformProductId = prediction.PlatformProductId,
            PlatformVariantId = prediction.PlatformVariantId,
            ProductTitle = prediction.ProductTitle,
            VariantTitle = prediction.VariantTitle,
            Sku = prediction.Sku,
            AlertType = alertType,
            Severity = severity,
            Message = message,
            CurrentQuantity = prediction.CurrentQuantity,
            ThresholdQuantity = prediction.Status == "critical"
                ? settings.CriticalStockDaysThreshold
                : settings.LowStockDaysThreshold,
            DaysUntilStockout = prediction.DaysUntilStockout == int.MaxValue ? null : prediction.DaysUntilStockout,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task SendEmailAlertAsync(string email, InventoryAlert alert, string shopDomain)
    {
        var subject = alert.Severity switch
        {
            "critical" => $"[CRITICAL] Inventory Alert: {alert.ProductTitle}",
            "high" => $"[URGENT] Inventory Alert: {alert.ProductTitle}",
            _ => $"Inventory Alert: {alert.ProductTitle}"
        };

        var body = $@"
<h2>Inventory Alert</h2>
<p><strong>Product:</strong> {alert.ProductTitle}</p>
{(alert.VariantTitle != null ? $"<p><strong>Variant:</strong> {alert.VariantTitle}</p>" : "")}
{(alert.Sku != null ? $"<p><strong>SKU:</strong> {alert.Sku}</p>" : "")}
<p><strong>Alert Type:</strong> {alert.AlertType.Replace("_", " ").ToUpper()}</p>
<p><strong>Current Quantity:</strong> {alert.CurrentQuantity}</p>
{(alert.DaysUntilStockout.HasValue ? $"<p><strong>Days Until Stockout:</strong> {alert.DaysUntilStockout}</p>" : "")}
<p>{alert.Message}</p>
<hr/>
<p><small>This alert was generated by the Smart Inventory Predictor.</small></p>
";

        try
        {
            await _notificationService.SendEmailAsync(shopDomain, new Application.DTOs.Communication.SendEmailNotificationDto
            {
                ToEmail = email,
                Subject = subject,
                Body = body
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email alert to {Email}", email);
            throw;
        }
    }

    private async Task<InventoryAlertSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.InventoryAlertSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings == null)
        {
            settings = new InventoryAlertSettings
            {
                ShopDomain = shopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.InventoryAlertSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return settings;
    }

    private static InventoryAlertDto MapToDto(InventoryAlert a) => new()
    {
        Id = a.Id,
        ShopDomain = a.ShopDomain,
        PlatformProductId = a.PlatformProductId,
        PlatformVariantId = a.PlatformVariantId,
        ProductTitle = a.ProductTitle,
        VariantTitle = a.VariantTitle,
        Sku = a.Sku,
        AlertType = a.AlertType,
        Severity = a.Severity,
        Message = a.Message,
        CurrentQuantity = a.CurrentQuantity,
        ThresholdQuantity = a.ThresholdQuantity,
        DaysUntilStockout = a.DaysUntilStockout,
        Status = a.Status,
        EmailSent = a.EmailSent,
        SmsSent = a.SmsSent,
        WhatsAppSent = a.WhatsAppSent,
        CreatedAt = a.CreatedAt,
        AcknowledgedAt = a.AcknowledgedAt,
        ResolvedAt = a.ResolvedAt
    };

    private static InventoryAlertSettingsDto MapSettingsToDto(InventoryAlertSettings s) => new()
    {
        Id = s.Id,
        ShopDomain = s.ShopDomain,
        AlertsEnabled = s.AlertsEnabled,
        LowStockDaysThreshold = s.LowStockDaysThreshold,
        CriticalStockDaysThreshold = s.CriticalStockDaysThreshold,
        DefaultLowStockQuantity = s.DefaultLowStockQuantity,
        DefaultCriticalStockQuantity = s.DefaultCriticalStockQuantity,
        DefaultLeadTimeDays = s.DefaultLeadTimeDays,
        DefaultSafetyStockDays = s.DefaultSafetyStockDays,
        EmailNotificationsEnabled = s.EmailNotificationsEnabled,
        NotificationEmail = s.NotificationEmail,
        SmsNotificationsEnabled = s.SmsNotificationsEnabled,
        NotificationPhone = s.NotificationPhone,
        WhatsAppNotificationsEnabled = s.WhatsAppNotificationsEnabled,
        WhatsAppPhone = s.WhatsAppPhone,
        MinHoursBetweenAlerts = s.MinHoursBetweenAlerts,
        DailyDigestEnabled = s.DailyDigestEnabled
    };
}
