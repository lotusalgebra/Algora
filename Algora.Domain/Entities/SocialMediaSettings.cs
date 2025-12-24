namespace Algora.Domain.Entities;

/// <summary>
/// Represents Facebook and Instagram API credentials for a shop.
/// </summary>
public class SocialMediaSettings
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain (unique).
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Facebook Page ID.
    /// </summary>
    public string? FacebookPageId { get; set; }

    /// <summary>
    /// Facebook Page Access Token (encrypted).
    /// </summary>
    public string? FacebookPageAccessToken { get; set; }

    /// <summary>
    /// Instagram Business Account ID.
    /// </summary>
    public string? InstagramAccountId { get; set; }

    /// <summary>
    /// Meta App ID.
    /// </summary>
    public string? MetaAppId { get; set; }

    /// <summary>
    /// Meta App Secret (encrypted).
    /// </summary>
    public string? MetaAppSecret { get; set; }

    /// <summary>
    /// Webhook verification token.
    /// </summary>
    public string? WebhookVerifyToken { get; set; }

    /// <summary>
    /// Whether social media integration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the settings were created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the settings were last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
