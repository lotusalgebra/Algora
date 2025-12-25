namespace Algora.Chatbot.Application.Interfaces.Shopify;

public interface IShopifyBillingService
{
    Task<string> CreateRecurringChargeAsync(string shopDomain, string accessToken, string planName, decimal price, int trialDays);
    Task<bool> ActivateChargeAsync(string shopDomain, string accessToken, string chargeId);
    Task<string?> GetActiveChargeIdAsync(string shopDomain, string accessToken);
    Task CancelChargeAsync(string shopDomain, string accessToken, string chargeId);
}
