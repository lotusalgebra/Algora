using Algora.Application.Interfaces;
using ShopifySharp;
using ShopifySharp.Filters;
using System;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Shopify.Billing;

public class ShopifyBillingService : IShopifyBillingService
{
    public async Task<string> CreateRecurringChargeAsync(string shopDomain, string accessToken, string planName, decimal price, int trialDays)
    {
        var billingService = new RecurringChargeService(shopDomain, accessToken);

        var charge = new RecurringCharge
        {
            Name = planName,
            Price = price,
            ReturnUrl = $"https://yourapp.com/licensing/activate?shop={shopDomain}",
            TrialDays = trialDays,
            Test = false // Set true for dev/testing
        };

        var result = await billingService.CreateAsync(charge);
        return result.ConfirmationUrl; // redirect merchant here
    }

    public async Task<bool> ActivateChargeAsync(string shopDomain, string accessToken, string chargeId)
    {
        // The ShopifySharp SDK version in this project does not expose a direct ActivateAsync method.
        // After the merchant confirms the charge (they visit the confirmation URL) Shopify sets the charge status.
        // Here we safely check the charge status via GetAsync and return true when it's active.
        if (!long.TryParse(chargeId, out var id))
            return false;

        var billingService = new RecurringChargeService(shopDomain, accessToken);
        var charge = await billingService.GetAsync(id);
        if (charge == null) return false;

        return string.Equals(charge.Status, "active", StringComparison.OrdinalIgnoreCase);
    }
}
