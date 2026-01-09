using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Authorization;

/// <summary>
/// Base page model that requires authentication and provides common functionality.
/// All secured pages should inherit from this.
/// </summary>
[Authorize]
public abstract class SecurePageModel : PageModel
{
    /// <summary>
    /// Gets the current shop domain from the user's claims.
    /// </summary>
    protected string ShopDomain =>
        User.FindFirst("shop_domain")?.Value ?? string.Empty;

    /// <summary>
    /// Gets the current user's email from claims.
    /// </summary>
    protected string UserEmail =>
        User.FindFirst("email")?.Value ?? string.Empty;

    /// <summary>
    /// Gets the current user's name from claims.
    /// </summary>
    protected string UserName =>
        User.FindFirst("name")?.Value ?? string.Empty;

    /// <summary>
    /// Gets the current shop's plan ID from claims.
    /// </summary>
    protected int? PlanId
    {
        get
        {
            var planIdStr = User.FindFirst("plan_id")?.Value;
            return int.TryParse(planIdStr, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Returns an access denied result with optional message.
    /// </summary>
    protected IActionResult AccessDenied(string? message = null)
    {
        TempData["ErrorMessage"] = message ?? "You do not have access to this feature. Please upgrade your plan.";
        return RedirectToPage("/Plans/Index");
    }

    /// <summary>
    /// Sets a success message to be displayed on the next page.
    /// </summary>
    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    /// <summary>
    /// Sets an error message to be displayed on the next page.
    /// </summary>
    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }
}
