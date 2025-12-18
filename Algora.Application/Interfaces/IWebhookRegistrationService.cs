namespace Algora.Application.Interfaces;

/// <summary>
/// Service for registering Shopify webhooks.
/// </summary>
public interface IWebhookRegistrationService
{
    /// <summary>
    /// Registers all required webhooks for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain (e.g., "store.myshopify.com")</param>
    /// <param name="accessToken">The shop's access token</param>
    /// <returns>True if all webhooks were registered successfully</returns>
    Task<bool> RegisterAllWebhooksAsync(string shopDomain, string accessToken);

    /// <summary>
    /// Registers a single webhook for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="accessToken">The shop's access token</param>
    /// <param name="topic">The webhook topic (e.g., "orders/create")</param>
    /// <param name="callbackUrl">The URL to receive webhook events</param>
    /// <returns>True if webhook was registered successfully</returns>
    Task<bool> RegisterWebhookAsync(string shopDomain, string accessToken, string topic, string callbackUrl);

    /// <summary>
    /// Gets all currently registered webhooks for a shop.
    /// </summary>
    Task<IEnumerable<WebhookInfo>> GetRegisteredWebhooksAsync(string shopDomain, string accessToken);

    /// <summary>
    /// Deletes a webhook by ID.
    /// </summary>
    Task<bool> DeleteWebhookAsync(string shopDomain, string accessToken, long webhookId);
}

/// <summary>
/// Information about a registered webhook.
/// </summary>
public record WebhookInfo(long Id, string Topic, string Address, string Format);
