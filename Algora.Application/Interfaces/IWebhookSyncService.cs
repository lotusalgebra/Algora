namespace Algora.Application.Interfaces;

/// <summary>
/// Service for syncing Shopify webhook data to the local database.
/// </summary>
public interface IWebhookSyncService
{
    // Order webhooks
    Task SyncOrderCreatedAsync(string shopDomain, string payload);
    Task SyncOrderUpdatedAsync(string shopDomain, string payload);
    Task SyncOrderCancelledAsync(string shopDomain, string payload);
    Task SyncOrderFulfilledAsync(string shopDomain, string payload);

    // Customer webhooks
    Task SyncCustomerCreatedAsync(string shopDomain, string payload);
    Task SyncCustomerUpdatedAsync(string shopDomain, string payload);
    Task SyncCustomerDeletedAsync(string shopDomain, string payload);

    // Product webhooks
    Task SyncProductCreatedAsync(string shopDomain, string payload);
    Task SyncProductUpdatedAsync(string shopDomain, string payload);
    Task SyncProductDeletedAsync(string shopDomain, string payload);
}
