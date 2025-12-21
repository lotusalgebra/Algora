namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Configuration options for SMS provider (Twilio, Nexmo, etc.).
/// </summary>
public class SmsOptions
{
    /// <summary>
    /// SMS provider: twilio, nexmo, messagebird, etc.
    /// </summary>
    public string Provider { get; set; } = "twilio";

    /// <summary>
    /// Account SID or API Key.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Auth Token or API Secret.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Phone number or sender ID to send messages from.
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL for delivery status callbacks.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Maximum messages per second for rate limiting.
    /// </summary>
    public int RateLimitPerSecond { get; set; } = 10;
}