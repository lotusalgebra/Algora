using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Service for managing per-shop communication settings stored in the database.
/// </summary>
public class CommunicationSettingsService : ICommunicationSettingsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CommunicationSettingsService> _logger;

    public CommunicationSettingsService(AppDbContext db, ILogger<CommunicationSettingsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CommunicationSettingsDto?> GetSettingsAsync(string shopDomain)
    {
        var settings = await _db.CommunicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        return settings is null ? null : MapToDto(settings);
    }

    public async Task<CommunicationSettingsDto> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.CommunicationSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings is null)
        {
            settings = new CommunicationSettings
            {
                ShopDomain = shopDomain,
                EmailProvider = "smtp",
                SmsProvider = "twilio",
                WhatsAppProvider = "meta",
                WhatsAppApiVersion = "v20.0",
                SmsRateLimitPerSecond = 10
            };

            _db.CommunicationSettings.Add(settings);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created default communication settings for {ShopDomain}", shopDomain);
        }

        return MapToDto(settings);
    }

    public async Task<CommunicationSettingsDto> SaveSettingsAsync(string shopDomain, UpdateCommunicationSettingsDto dto)
    {
        var settings = await _db.CommunicationSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings is null)
        {
            settings = new CommunicationSettings { ShopDomain = shopDomain };
            _db.CommunicationSettings.Add(settings);
        }

        // Update Email settings
        if (dto.EmailProvider is not null) settings.EmailProvider = dto.EmailProvider;
        if (dto.EmailApiKey is not null) settings.EmailApiKey = dto.EmailApiKey;
        if (dto.SmtpHost is not null) settings.SmtpHost = dto.SmtpHost;
        if (dto.SmtpPort.HasValue) settings.SmtpPort = dto.SmtpPort.Value;
        if (dto.SmtpUsername is not null) settings.SmtpUsername = dto.SmtpUsername;
        if (dto.SmtpPassword is not null) settings.SmtpPassword = dto.SmtpPassword; // TODO: Encrypt
        if (dto.SmtpUseSsl.HasValue) settings.SmtpUseSsl = dto.SmtpUseSsl.Value;
        if (dto.DefaultFromName is not null) settings.DefaultFromName = dto.DefaultFromName;
        if (dto.DefaultFromEmail is not null) settings.DefaultFromEmail = dto.DefaultFromEmail;
        if (dto.DefaultReplyTo is not null) settings.DefaultReplyTo = dto.DefaultReplyTo;
        if (dto.EmailEnabled.HasValue) settings.EmailEnabled = dto.EmailEnabled.Value;

        // Update WhatsApp settings
        if (dto.WhatsAppProvider is not null) settings.WhatsAppProvider = dto.WhatsAppProvider;
        if (dto.WhatsAppAccessToken is not null) settings.WhatsAppAccessToken = dto.WhatsAppAccessToken; // TODO: Encrypt
        if (dto.WhatsAppPhoneNumberId is not null) settings.WhatsAppPhoneNumberId = dto.WhatsAppPhoneNumberId;
        if (dto.WhatsAppBusinessAccountId is not null) settings.WhatsAppBusinessAccountId = dto.WhatsAppBusinessAccountId;
        if (dto.WhatsAppWebhookVerifyToken is not null) settings.WhatsAppWebhookVerifyToken = dto.WhatsAppWebhookVerifyToken;
        if (dto.WhatsAppApiVersion is not null) settings.WhatsAppApiVersion = dto.WhatsAppApiVersion;
        if (dto.WhatsAppEnabled.HasValue) settings.WhatsAppEnabled = dto.WhatsAppEnabled.Value;

        // Update SMS settings
        if (dto.SmsProvider is not null) settings.SmsProvider = dto.SmsProvider;
        if (dto.SmsAccountSid is not null) settings.SmsAccountSid = dto.SmsAccountSid;
        if (dto.SmsAuthToken is not null) settings.SmsAuthToken = dto.SmsAuthToken; // TODO: Encrypt
        if (dto.SmsFromNumber is not null) settings.SmsFromNumber = dto.SmsFromNumber;
        if (dto.SmsWebhookUrl is not null) settings.SmsWebhookUrl = dto.SmsWebhookUrl;
        if (dto.SmsRateLimitPerSecond.HasValue) settings.SmsRateLimitPerSecond = dto.SmsRateLimitPerSecond.Value;
        if (dto.SmsEnabled.HasValue) settings.SmsEnabled = dto.SmsEnabled.Value;

        // Update General settings
        if (dto.DoubleOptInRequired.HasValue) settings.DoubleOptInRequired = dto.DoubleOptInRequired.Value;
        if (dto.UnsubscribePageUrl is not null) settings.UnsubscribePageUrl = dto.UnsubscribePageUrl;

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated communication settings for {ShopDomain}", shopDomain);
        return MapToDto(settings);
    }

    public async Task<EmailSettingsDto?> GetEmailSettingsAsync(string shopDomain)
    {
        var settings = await _db.CommunicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings is null) return null;

        var isConfigured = !string.IsNullOrEmpty(settings.DefaultFromEmail) &&
            (settings.EmailProvider == "smtp"
                ? !string.IsNullOrEmpty(settings.SmtpHost)
                : !string.IsNullOrEmpty(settings.EmailApiKey));

        return new EmailSettingsDto
        {
            Provider = settings.EmailProvider ?? "smtp",
            ApiKey = settings.EmailApiKey,
            SmtpHost = settings.SmtpHost,
            SmtpPort = settings.SmtpPort ?? 587,
            SmtpUsername = settings.SmtpUsername,
            SmtpPassword = settings.SmtpPassword,
            SmtpUseSsl = settings.SmtpUseSsl,
            DefaultFromEmail = settings.DefaultFromEmail ?? "",
            DefaultFromName = settings.DefaultFromName ?? "",
            DefaultReplyTo = settings.DefaultReplyTo,
            IsEnabled = settings.EmailEnabled,
            IsConfigured = isConfigured
        };
    }

    public async Task<SmsSettingsDto?> GetSmsSettingsAsync(string shopDomain)
    {
        var settings = await _db.CommunicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings is null) return null;

        var isConfigured = !string.IsNullOrEmpty(settings.SmsAccountSid) &&
                           !string.IsNullOrEmpty(settings.SmsAuthToken) &&
                           !string.IsNullOrEmpty(settings.SmsFromNumber);

        return new SmsSettingsDto
        {
            Provider = settings.SmsProvider ?? "twilio",
            AccountSid = settings.SmsAccountSid,
            AuthToken = settings.SmsAuthToken,
            FromNumber = settings.SmsFromNumber,
            WebhookUrl = settings.SmsWebhookUrl,
            RateLimitPerSecond = settings.SmsRateLimitPerSecond,
            IsEnabled = settings.SmsEnabled,
            IsConfigured = isConfigured
        };
    }

    public async Task<WhatsAppSettingsDto?> GetWhatsAppSettingsAsync(string shopDomain)
    {
        var settings = await _db.CommunicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        if (settings is null) return null;

        var isConfigured = !string.IsNullOrEmpty(settings.WhatsAppAccessToken) &&
                           !string.IsNullOrEmpty(settings.WhatsAppPhoneNumberId);

        return new WhatsAppSettingsDto
        {
            Provider = settings.WhatsAppProvider ?? "meta",
            AccessToken = settings.WhatsAppAccessToken,
            PhoneNumberId = settings.WhatsAppPhoneNumberId,
            BusinessAccountId = settings.WhatsAppBusinessAccountId,
            WebhookVerifyToken = settings.WhatsAppWebhookVerifyToken,
            ApiVersion = settings.WhatsAppApiVersion ?? "v20.0",
            IsEnabled = settings.WhatsAppEnabled,
            IsConfigured = isConfigured
        };
    }

    public async Task<bool> TestEmailConnectionAsync(string shopDomain)
    {
        var settings = await GetEmailSettingsAsync(shopDomain);
        if (settings is null || !settings.IsConfigured) return false;

        // TODO: Implement actual connection test
        _logger.LogInformation("Testing email connection for {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestSmsConnectionAsync(string shopDomain)
    {
        var settings = await GetSmsSettingsAsync(shopDomain);
        if (settings is null || !settings.IsConfigured) return false;

        // TODO: Implement actual connection test
        _logger.LogInformation("Testing SMS connection for {ShopDomain}", shopDomain);
        return true;
    }

    public async Task<bool> TestWhatsAppConnectionAsync(string shopDomain)
    {
        var settings = await GetWhatsAppSettingsAsync(shopDomain);
        if (settings is null || !settings.IsConfigured) return false;

        // TODO: Implement actual connection test
        _logger.LogInformation("Testing WhatsApp connection for {ShopDomain}", shopDomain);
        return true;
    }

    private static CommunicationSettingsDto MapToDto(CommunicationSettings s)
    {
        var emailConfigured = !string.IsNullOrEmpty(s.DefaultFromEmail) &&
            (s.EmailProvider == "smtp" ? !string.IsNullOrEmpty(s.SmtpHost) : !string.IsNullOrEmpty(s.EmailApiKey));

        var smsConfigured = !string.IsNullOrEmpty(s.SmsAccountSid) &&
                            !string.IsNullOrEmpty(s.SmsAuthToken) &&
                            !string.IsNullOrEmpty(s.SmsFromNumber);

        var whatsAppConfigured = !string.IsNullOrEmpty(s.WhatsAppAccessToken) &&
                                 !string.IsNullOrEmpty(s.WhatsAppPhoneNumberId);

        return new CommunicationSettingsDto
        {
            Id = s.Id,
            ShopDomain = s.ShopDomain,
            EmailProvider = s.EmailProvider,
            SmtpHost = s.SmtpHost,
            SmtpPort = s.SmtpPort,
            SmtpUseSsl = s.SmtpUseSsl,
            DefaultFromName = s.DefaultFromName,
            DefaultFromEmail = s.DefaultFromEmail,
            DefaultReplyTo = s.DefaultReplyTo,
            EmailEnabled = s.EmailEnabled,
            EmailConfigured = emailConfigured,
            SmsProvider = s.SmsProvider,
            SmsFromNumber = s.SmsFromNumber,
            SmsRateLimitPerSecond = s.SmsRateLimitPerSecond,
            SmsEnabled = s.SmsEnabled,
            SmsConfigured = smsConfigured,
            WhatsAppProvider = s.WhatsAppProvider,
            WhatsAppPhoneNumberId = s.WhatsAppPhoneNumberId,
            WhatsAppApiVersion = s.WhatsAppApiVersion,
            WhatsAppEnabled = s.WhatsAppEnabled,
            WhatsAppConfigured = whatsAppConfigured,
            DoubleOptInRequired = s.DoubleOptInRequired,
            UnsubscribePageUrl = s.UnsubscribePageUrl,
            CreatedAt = s.CreatedAt
        };
    }
}