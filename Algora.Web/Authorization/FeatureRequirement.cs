using Microsoft.AspNetCore.Authorization;

namespace Algora.Web.Authorization;

/// <summary>
/// Requirement that checks if the user's shop has access to a specific feature.
/// </summary>
public class FeatureRequirement : IAuthorizationRequirement
{
    public string FeatureCode { get; }

    public FeatureRequirement(string featureCode)
    {
        FeatureCode = featureCode;
    }
}
