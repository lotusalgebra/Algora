namespace Algora.Application.DTOs.Communication;

public record CommunicationSettingsDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;

    // Email
    public string? EmailProvider { get; init; }
    public string? SmtpHost { get; init; }
    public int? SmtpPort { get; init; }
    public bool SmtpUseSsl { get; init; }
    public string? DefaultFromName { get; init; }
    public string? DefaultFromEmail { get; init; }
    public string? DefaultReplyTo { get; init; }
    public bool EmailEnabled { get; init; }
    public bool EmailConfigured { get; init; }

    // SMS
    public string? SmsProvider { get; init; }
    public string? SmsFromNumber { get; init; }
    public int SmsRateLimitPerSecond { get; init; }
    public bool SmsEnabled { get; init; }
    public bool SmsConfigured { get; init; }

    // WhatsApp
    public string? WhatsAppProvider { get; init; }
    public string? WhatsAppPhoneNumberId { get; init; }
    public string? WhatsAppApiVersion { get; init; }
    public bool WhatsAppEnabled { get; init; }
    public bool WhatsAppConfigured { get; init; }

    // General
    public bool DoubleOptInRequired { get; init; }
    public string? UnsubscribePageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UpdateCommunicationSettingsDto
{
    // Email settings
    public string? EmailProvider { get; init; }
    public string? EmailApiKey { get; init; }
    public string? SmtpHost { get; init; }
    public int? SmtpPort { get; init; }
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public bool? SmtpUseSsl { get; init; }
    public string? DefaultFromName { get; init; }
    public string? DefaultFromEmail { get; init; }
    public string? DefaultReplyTo { get; init; }
    public bool? EmailEnabled { get; init; }

    // WhatsApp settings
    public string? WhatsAppProvider { get; init; }
    public string? WhatsAppAccessToken { get; init; }
    public string? WhatsAppPhoneNumberId { get; init; }
    public string? WhatsAppBusinessAccountId { get; init; }
    public string? WhatsAppWebhookVerifyToken { get; init; }
    public string? WhatsAppApiVersion { get; init; }
    public bool? WhatsAppEnabled { get; init; }

    // SMS settings
    public string? SmsProvider { get; init; }
    public string? SmsAccountSid { get; init; }
    public string? SmsAuthToken { get; init; }
    public string? SmsFromNumber { get; init; }
    public string? SmsWebhookUrl { get; init; }
    public int? SmsRateLimitPerSecond { get; init; }
    public bool? SmsEnabled { get; init; }

    // General
    public bool? DoubleOptInRequired { get; init; }
    public string? UnsubscribePageUrl { get; init; }
}

/// <summary>
/// Email settings loaded from database for use by email services.
/// </summary>
public record EmailSettingsDto
{
    public string Provider { get; init; } = "smtp";
    public string? ApiKey { get; init; }
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public bool SmtpUseSsl { get; init; } = true;
    public string DefaultFromEmail { get; init; } = string.Empty;
    public string DefaultFromName { get; init; } = string.Empty;
    public string? DefaultReplyTo { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsConfigured { get; init; }
}

/// <summary>
/// SMS settings loaded from database for use by SMS services.
/// </summary>
public record SmsSettingsDto
{
    public string Provider { get; init; } = "twilio";
    public string? AccountSid { get; init; }
    public string? AuthToken { get; init; }
    public string? FromNumber { get; init; }
    public string? WebhookUrl { get; init; }
    public int RateLimitPerSecond { get; init; } = 10;
    public bool IsEnabled { get; init; }
    public bool IsConfigured { get; init; }
}

/// <summary>
/// WhatsApp settings loaded from database for use by WhatsApp services.
/// </summary>
public record WhatsAppSettingsDto
{
    public string Provider { get; init; } = "meta";
    public string? AccessToken { get; init; }
    public string? PhoneNumberId { get; init; }
    public string? BusinessAccountId { get; init; }
    public string? WebhookVerifyToken { get; init; }
    public string ApiVersion { get; init; } = "v20.0";
    public bool IsEnabled { get; init; }
    public bool IsConfigured { get; init; }
}