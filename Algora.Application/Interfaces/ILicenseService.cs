using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Manages licensing state for installed shops.
    /// Implementations are responsible for creating, updating, retrieving and deactivating
    /// license records (for example after billing events or app uninstall webhooks).
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>
        /// Retrieves the license information for the specified shop domain.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain (for example: "example-shop.myshopify.com").</param>
        /// <returns>
        /// A task that resolves to a <see cref="LicenseDto"/> containing license details,
        /// or <c>null</c> if no license is found for the given shop.
        /// </returns>
        Task<LicenseDto?> GetLicenseAsync(string shopDomain);

        /// <summary>
        /// Creates a new license record or updates an existing one for the given shop.
        /// Use this after a successful billing charge or plan change to persist licensing state.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <param name="planName">The plan identifier or name the merchant purchased.</param>
        /// <param name="chargeId">Platform charge identifier (billing recurring charge id or payment id).</param>
        /// <param name="expiry">UTC date/time when the license expires (or next renewal date).</param>
        /// <param name="isTrial">True when the license was created as a trial.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> when the create/update operation succeeded; otherwise <c>false</c>.
        /// </returns>
        Task<bool> CreateOrUpdateLicenseAsync(string shopDomain, string planName, string chargeId, DateTime expiry, bool isTrial);

        /// <summary>
        /// Deactivates the license for the specified shop domain. Typically called when a charge is cancelled
        /// or the app is uninstalled to prevent further access to paid features.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the license was successfully deactivated or did not exist;
        /// otherwise <c>false</c> when the operation failed.
        /// </returns>
        Task<bool> DeactivateLicenseAsync(string shopDomain);
    }
}
