using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Performs billing operations against Shopify for the installed shops.
    /// Implementations should use the Shopify Billing API (recurring or one-time charges)
    /// to create charges and activate them once the merchant accepts the charge in Shopify.
    /// </summary>
    public interface IShopifyBillingService
    {
        /// <summary>
        /// Creates a recurring billing charge for the specified shop.
        /// </summary>
        /// <param name="shopDomain">Shop's myshopify domain (for example "example-shop.myshopify.com").</param>
        /// <param name="accessToken">An access token with permissions to create charges for the shop.</param>
        /// <param name="planName">Human-friendly plan name or identifier used in the charge.</param>
        /// <param name="price">Recurring price for the plan (in shop currency).</param>
        /// <param name="trialDays">Number of trial days to offer (0 for no trial).</param>
        /// <returns>
        /// A task that resolves to a string. Implementations should return the charge confirmation URL
        /// that the merchant must be redirected to in order to accept the charge. If an implementation
        /// returns a different value (for example the created charge id), document that behavior accordingly.
        /// </returns>
        Task<string> CreateRecurringChargeAsync(string shopDomain, string accessToken, string planName, decimal price, int trialDays);

        /// <summary>
        /// Activates a previously created charge after the merchant has accepted it in Shopify.
        /// </summary>
        /// <param name="shopDomain">Shop's myshopify domain.</param>
        /// <param name="accessToken">Access token with permission to activate charges for the shop.</param>
        /// <param name="chargeId">The identifier of the charge to activate (as returned by the creation step or Shopify callback).</param>
        /// <returns>
        /// A task that resolves to <c>true</c> when the activation completes successfully; otherwise <c>false</c>.
        /// Implementations should handle and log API errors and return <c>false</c> for non-recoverable failures.
        /// </returns>
        Task<bool> ActivateChargeAsync(string shopDomain, string accessToken, string chargeId);
    }
}
