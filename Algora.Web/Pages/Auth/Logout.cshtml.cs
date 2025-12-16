using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await SignOutAndRedirect();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SignOutAndRedirect();
    }

    private async Task<IActionResult> SignOutAndRedirect()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        _logger.LogInformation("User {Email} logged out", email ?? "Unknown");
        
        return RedirectToPage("/Auth/Login");
    }
}