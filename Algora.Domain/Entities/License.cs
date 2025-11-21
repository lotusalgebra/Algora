using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a persisted license record for an installed shop.
    /// Used to track plan, billing information, activation state and expiry for a merchant.
    /// </summary>
    public class License
    {
        /// <summary>
        /// Primary key for the license record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The shop's myshopify domain (for example: "example-shop.myshopify.com").
        /// </summary>
        public string ShopDomain { get; set; } = string.Empty;

        /// <summary>
        /// The human-friendly plan name assigned to the shop (for example: "Basic", "Pro").
        /// </summary>
        public string PlanName { get; set; } = "Basic";

        /// <summary>
        /// UTC date/time when the license became active or was created.
        /// Store times in UTC to avoid timezone issues.
        /// </summary>
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC date/time when the license expires or the next renewal is due.
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// True when the license is currently active and the shop is allowed to use paid features.
        /// False when cancelled or explicitly deactivated.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Platform billing charge identifier associated with this license (recurring charge id, invoice id, etc.).
        /// Store this to reconcile billing events and to activate/deactivate on webhook callbacks.
        /// </summary>
        public string ChargeId { get; set; } = string.Empty;

        /// <summary>
        /// Friendly status string describing the license lifecycle state.
        /// Typical values: "trial", "active", "cancelled", "expired".
        /// Use consistent values in code to check for access (avoid relying solely on IsActive).
        /// </summary>
        public string Status { get; set; } = "trial"; // active, trial, cancelled, expired
    }
}
