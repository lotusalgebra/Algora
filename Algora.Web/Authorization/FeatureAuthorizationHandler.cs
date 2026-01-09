using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Algora.Web.Authorization;

/// <summary>
/// Authorization handler that checks if the user's shop has access to a specific feature.
/// </summary>
public class FeatureAuthorizationHandler : AuthorizationHandler<FeatureRequirement>
{
    private readonly IPlanFeatureService _featureService;

    public FeatureAuthorizationHandler(IPlanFeatureService featureService)
    {
        _featureService = featureService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        // Get shop domain from claims
        var shopDomain = context.User.FindFirst("shop_domain")?.Value;

        if (string.IsNullOrEmpty(shopDomain))
        {
            // No shop domain claim - fail authorization
            return;
        }

        // Check if shop has the required feature
        var hasFeature = await _featureService.ShopHasFeatureAsync(shopDomain, requirement.FeatureCode);

        if (hasFeature)
        {
            context.Succeed(requirement);
        }
    }
}
