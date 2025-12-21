namespace Algora.WhatsApp.Configuration;

/// <summary>
/// Configuration options for Facebook WhatsApp Business API.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>
    /// Facebook Graph API access token with WhatsApp permissions.
    /// Generate from Facebook Business Manager or Meta for Developers.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp Business Phone Number ID.
    /// Found in WhatsApp Manager > Phone Numbers.
    /// </summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp Business Account ID.
    /// Found in WhatsApp Manager > Account tools > Phone numbers.
    /// </summary>
    public string BusinessAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Verify token for webhook registration.
    /// Set this in Meta App Dashboard > WhatsApp > Configuration.
    /// </summary>
    public string WebhookVerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// App Secret for webhook signature verification.
    /// Found in Meta App Dashboard > App settings > Basic.
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Facebook Graph API version. Default is v21.0 (current stable).
    /// </summary>
    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>
    /// Base URL for Facebook Graph API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable message status webhook callbacks.
    /// </summary>
    public bool EnableStatusCallbacks { get; set; } = true;

    /// <summary>
    /// Whether to automatically mark messages as read when received.
    /// </summary>
    public bool AutoMarkAsRead { get; set; } = true;

    /// <summary>
    /// Default reply-to message window in hours (WhatsApp allows 24 hours).
    /// </summary>
    public int MessageWindowHours { get; set; } = 24;
}
