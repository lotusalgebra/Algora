namespace Algora.Domain.Entities;

/// <summary>
/// Represents communication/marketing settings for a shop, stored per client/shop.
/// </summary>
public class CommunicationSettings
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // ===== Email Settings =====
    public string? EmailProvider { get; set; } // smtp, sendgrid, mailgun, ses
    public string? EmailApiKey { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; } // Encrypted
    public bool SmtpUseSsl { get; set; } = true;
    public string? DefaultFromName { get; set; }
    public string? DefaultFromEmail { get; set; }
    public string? DefaultReplyTo { get; set; }
    public bool EmailEnabled { get; set; } = true;

    // ===== WhatsApp Settings =====
    public string? WhatsAppProvider { get; set; } // meta, twilio, messagebird
    public string? WhatsAppAccessToken { get; set; } // Encrypted
    public string? WhatsAppPhoneNumberId { get; set; }
    public string? WhatsAppBusinessAccountId { get; set; }
    public string? WhatsAppWebhookVerifyToken { get; set; }
    public string? WhatsAppApiVersion { get; set; } = "v20.0";
    public bool WhatsAppEnabled { get; set; }

    // ===== SMS Settings =====
    public string? SmsProvider { get; set; } // twilio, nexmo, messagebird
    public string? SmsAccountSid { get; set; }
    public string? SmsAuthToken { get; set; } // Encrypted
    public string? SmsFromNumber { get; set; }
    public string? SmsWebhookUrl { get; set; }
    public int SmsRateLimitPerSecond { get; set; } = 10;
    public bool SmsEnabled { get; set; }

    // ===== General Settings =====
    public bool DoubleOptInRequired { get; set; }
    public string? UnsubscribePageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}