namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Configuration options for email sending.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// Email provider: smtp, sendgrid, mailgun, ses, etc.
    /// </summary>
    public string Provider { get; set; } = "smtp";

    /// <summary>
    /// SMTP host server.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP port (typically 587 for TLS, 465 for SSL, 25 for unencrypted).
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password for authentication.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS for SMTP connection.
    /// </summary>
    public bool SmtpUseSsl { get; set; } = true;

    /// <summary>
    /// API key for cloud email providers (SendGrid, Mailgun, etc.).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default sender email address.
    /// </summary>
    public string DefaultFromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender name.
    /// </summary>
    public string DefaultFromName { get; set; } = string.Empty;

    /// <summary>
    /// Default reply-to email address.
    /// </summary>
    public string? DefaultReplyTo { get; set; }
}