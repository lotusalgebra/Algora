using Algora.Application.DTOs.Plan;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Manages subscription plans and plan change requests.
    /// Handles plan lookups, feature access checks, limit validation, and plan change workflows.
    /// </summary>
    public interface IPlanService
    {
        /// <summary>
        /// Gets all active plans available for selection.
        /// </summary>
        /// <returns>Collection of plan DTOs ordered by sort order.</returns>
        Task<IEnumerable<PlanDto>> GetAllPlansAsync();

        /// <summary>
        /// Gets a specific plan by its name.
        /// </summary>
        /// <param name="planName">The plan name (e.g., "Free", "Premium", "Enterprise").</param>
        /// <returns>Plan DTO if found; otherwise null.</returns>
        Task<PlanDto?> GetPlanByNameAsync(string planName);

        /// <summary>
        /// Gets the current plan for a shop based on their license.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <returns>Current plan DTO if found; otherwise null (defaults to Free).</returns>
        Task<PlanDto?> GetCurrentPlanAsync(string shopDomain);

        /// <summary>
        /// Checks if a shop can access a specific feature based on their plan.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <param name="featureName">Feature identifier (e.g., "whatsapp", "email_campaigns", "sms", "advanced_reports", "api_access").</param>
        /// <returns>True if the shop's plan includes the feature; otherwise false.</returns>
        Task<bool> CanAccessFeatureAsync(string shopDomain, string featureName);

        /// <summary>
        /// Checks if a shop is within their plan's limit for a specific resource.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <param name="limitType">Limit type (e.g., "orders", "products", "customers").</param>
        /// <param name="currentCount">Current count of the resource.</param>
        /// <returns>True if within limit; false if limit exceeded.</returns>
        Task<bool> IsWithinLimitAsync(string shopDomain, string limitType, int currentCount);

        /// <summary>
        /// Initiates a plan change request. Upgrades are processed immediately via Shopify billing.
        /// Downgrades create a pending request requiring admin approval.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <param name="accessToken">Shop's Shopify access token for billing API.</param>
        /// <param name="newPlanName">The desired plan name.</param>
        /// <returns>
        /// For upgrades: Shopify billing confirmation URL to redirect the merchant.
        /// For downgrades: "pending" to indicate request was created.
        /// Null if request failed.
        /// </returns>
        Task<string?> RequestPlanChangeAsync(string shopDomain, string accessToken, string newPlanName);

        /// <summary>
        /// Gets all pending plan change requests (for admin view).
        /// </summary>
        /// <returns>Collection of pending request DTOs.</returns>
        Task<IEnumerable<PlanChangeRequestDto>> GetPendingRequestsAsync();

        /// <summary>
        /// Gets all plan change requests for a specific shop.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain.</param>
        /// <returns>Collection of request DTOs for the shop.</returns>
        Task<IEnumerable<PlanChangeRequestDto>> GetRequestsForShopAsync(string shopDomain);

        /// <summary>
        /// Approves a pending plan change request and updates the shop's license.
        /// </summary>
        /// <param name="requestId">The request ID to approve.</param>
        /// <param name="adminEmail">Email of the admin processing the request.</param>
        /// <param name="adminNotes">Optional notes about the approval.</param>
        /// <returns>True if approved successfully; otherwise false.</returns>
        Task<bool> ApproveRequestAsync(int requestId, string adminEmail, string? adminNotes);

        /// <summary>
        /// Rejects a pending plan change request.
        /// </summary>
        /// <param name="requestId">The request ID to reject.</param>
        /// <param name="adminEmail">Email of the admin processing the request.</param>
        /// <param name="adminNotes">Reason for rejection.</param>
        /// <returns>True if rejected successfully; otherwise false.</returns>
        Task<bool> RejectRequestAsync(int requestId, string adminEmail, string? adminNotes);

        /// <summary>
        /// Seeds the default plans if they don't exist in the database.
        /// Should be called on application startup.
        /// </summary>
        Task SeedDefaultPlansAsync();
    }
}
