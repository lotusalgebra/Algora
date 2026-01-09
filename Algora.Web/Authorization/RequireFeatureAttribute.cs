using Microsoft.AspNetCore.Authorization;

namespace Algora.Web.Authorization;

/// <summary>
/// Attribute to require a specific plan feature for accessing a page or action.
/// Usage: [RequireFeature("ai_descriptions")]
/// </summary>
public class RequireFeatureAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Feature_";

    public RequireFeatureAttribute(string featureCode)
    {
        Policy = $"{PolicyPrefix}{featureCode}";
    }
}
