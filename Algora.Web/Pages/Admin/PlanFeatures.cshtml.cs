using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Admin
{
    [Authorize]
    public class PlanFeaturesModel : PageModel
    {
        private readonly IPlanFeatureService _featureService;
        private readonly ILogger<PlanFeaturesModel> _logger;

        public PlanFeaturesModel(
            IPlanFeatureService featureService,
            ILogger<PlanFeaturesModel> logger)
        {
            _featureService = featureService;
            _logger = logger;
        }

        public Dictionary<string, List<PlanFeatureDto>> FeaturesByCategory { get; set; } = new();
        public IEnumerable<PlanFeatureDto> AllFeatures { get; set; } = [];
        public IEnumerable<PlanWithFeaturesDto> PlansWithFeatures { get; set; } = [];
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                await LoadPageDataAsync();

                if (Request.Query.ContainsKey("added"))
                    SuccessMessage = "Feature added successfully.";
                else if (Request.Query.ContainsKey("updated"))
                    SuccessMessage = "Feature updated successfully.";
                else if (Request.Query.ContainsKey("deleted"))
                    SuccessMessage = "Feature deleted successfully.";
                else if (Request.Query.ContainsKey("assigned"))
                    SuccessMessage = "Feature assigned to plan.";
                else if (Request.Query.ContainsKey("removed"))
                    SuccessMessage = "Feature removed from plan.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plan features");
                ErrorMessage = "Failed to load features. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostAddFeatureAsync(string code, string name, string description, string category, string? iconClass, int sortOrder)
        {
            try
            {
                var dto = new CreatePlanFeatureDto
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    Category = category,
                    IconClass = iconClass,
                    SortOrder = sortOrder
                };

                await _featureService.CreateFeatureAsync(dto);
                _logger.LogInformation("Created feature: {Code}", code);

                return RedirectToPage(new { added = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add feature");
                ErrorMessage = "Failed to add feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateFeatureAsync(int featureId, string name, string description, string category, string? iconClass, int sortOrder, bool isActive = false)
        {
            try
            {
                var dto = new UpdatePlanFeatureDto
                {
                    Name = name,
                    Description = description,
                    Category = category,
                    IconClass = iconClass,
                    SortOrder = sortOrder,
                    IsActive = isActive
                };

                await _featureService.UpdateFeatureAsync(featureId, dto);
                _logger.LogInformation("Updated feature: {FeatureId}", featureId);

                return RedirectToPage(new { updated = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update feature {FeatureId}", featureId);
                ErrorMessage = "Failed to update feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteFeatureAsync(int featureId)
        {
            try
            {
                var success = await _featureService.DeleteFeatureAsync(featureId);
                if (success)
                {
                    _logger.LogInformation("Deleted feature: {FeatureId}", featureId);
                    return RedirectToPage(new { deleted = true });
                }

                ErrorMessage = "Failed to delete feature.";
                await LoadPageDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete feature {FeatureId}", featureId);
                ErrorMessage = "Failed to delete feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleFeatureAsync(int featureId)
        {
            try
            {
                await _featureService.ToggleFeatureActiveAsync(featureId);
                _logger.LogInformation("Toggled feature: {FeatureId}", featureId);
                return RedirectToPage(new { updated = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle feature {FeatureId}", featureId);
                ErrorMessage = "Failed to toggle feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAssignFeatureAsync(int planId, int featureId)
        {
            try
            {
                var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";
                var dto = new AssignFeatureToPlanDto
                {
                    PlanId = planId,
                    FeatureId = featureId,
                    AssignedBy = adminEmail
                };

                await _featureService.AssignFeatureToPlanAsync(dto);
                _logger.LogInformation("Assigned feature {FeatureId} to plan {PlanId}", featureId, planId);

                return RedirectToPage(new { assigned = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign feature {FeatureId} to plan {PlanId}", featureId, planId);
                ErrorMessage = "Failed to assign feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRemoveFeatureAsync(int planId, int featureId)
        {
            try
            {
                await _featureService.RemoveFeatureFromPlanAsync(planId, featureId);
                _logger.LogInformation("Removed feature {FeatureId} from plan {PlanId}", featureId, planId);

                return RedirectToPage(new { removed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove feature {FeatureId} from plan {PlanId}", featureId, planId);
                ErrorMessage = "Failed to remove feature. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        private async Task LoadPageDataAsync()
        {
            FeaturesByCategory = await _featureService.GetFeaturesByCategoryAsync(activeOnly: false);
            AllFeatures = await _featureService.GetAllFeaturesAsync();
            PlansWithFeatures = await _featureService.GetAllPlansWithFeaturesAsync();
        }
    }
}
