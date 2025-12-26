using Algora.Application.DTOs.Plan;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Manages plan features and feature assignments to plans.
    /// Provides dynamic feature management without schema changes.
    /// </summary>
    public interface IPlanFeatureService
    {
        // ===== Feature CRUD =====

        /// <summary>
        /// Gets all plan features.
        /// </summary>
        /// <param name="activeOnly">If true, only returns active features.</param>
        /// <returns>Collection of feature DTOs.</returns>
        Task<IEnumerable<PlanFeatureDto>> GetAllFeaturesAsync(bool activeOnly = false);

        /// <summary>
        /// Gets a feature by its ID.
        /// </summary>
        Task<PlanFeatureDto?> GetFeatureByIdAsync(int featureId);

        /// <summary>
        /// Gets a feature by its code.
        /// </summary>
        Task<PlanFeatureDto?> GetFeatureByCodeAsync(string code);

        /// <summary>
        /// Gets all features grouped by category.
        /// </summary>
        Task<Dictionary<string, List<PlanFeatureDto>>> GetFeaturesByCategoryAsync(bool activeOnly = true);

        /// <summary>
        /// Creates a new plan feature.
        /// </summary>
        Task<PlanFeatureDto> CreateFeatureAsync(CreatePlanFeatureDto dto);

        /// <summary>
        /// Updates an existing plan feature.
        /// </summary>
        Task<PlanFeatureDto?> UpdateFeatureAsync(int featureId, UpdatePlanFeatureDto dto);

        /// <summary>
        /// Deletes a plan feature and all its assignments.
        /// </summary>
        Task<bool> DeleteFeatureAsync(int featureId);

        /// <summary>
        /// Toggles the active status of a feature.
        /// </summary>
        Task<bool> ToggleFeatureActiveAsync(int featureId);

        // ===== Feature Assignment =====

        /// <summary>
        /// Gets all plans with their assigned features.
        /// </summary>
        Task<IEnumerable<PlanWithFeaturesDto>> GetAllPlansWithFeaturesAsync();

        /// <summary>
        /// Gets a specific plan with its assigned features.
        /// </summary>
        Task<PlanWithFeaturesDto?> GetPlanWithFeaturesAsync(int planId);

        /// <summary>
        /// Gets all features assigned to a plan.
        /// </summary>
        Task<IEnumerable<PlanFeatureDto>> GetFeaturesForPlanAsync(int planId);

        /// <summary>
        /// Assigns a feature to a plan.
        /// </summary>
        Task<bool> AssignFeatureToPlanAsync(AssignFeatureToPlanDto dto);

        /// <summary>
        /// Removes a feature from a plan.
        /// </summary>
        Task<bool> RemoveFeatureFromPlanAsync(int planId, int featureId);

        /// <summary>
        /// Bulk assigns features to a plan (replaces existing assignments).
        /// </summary>
        Task<bool> BulkAssignFeaturesAsync(BulkAssignFeaturesDto dto);

        // ===== Feature Access Check =====

        /// <summary>
        /// Checks if a shop has access to a specific feature based on their plan.
        /// </summary>
        /// <param name="shopDomain">The shop's domain.</param>
        /// <param name="featureCode">The feature code to check.</param>
        /// <returns>True if the shop's plan includes this feature.</returns>
        Task<bool> ShopHasFeatureAsync(string shopDomain, string featureCode);

        /// <summary>
        /// Gets all feature codes available to a shop based on their plan.
        /// </summary>
        Task<IEnumerable<string>> GetShopFeaturesAsync(string shopDomain);

        // ===== Seed Default Features =====

        /// <summary>
        /// Seeds default features if they don't exist.
        /// Should be called on application startup.
        /// </summary>
        Task SeedDefaultFeaturesAsync();
    }
}
