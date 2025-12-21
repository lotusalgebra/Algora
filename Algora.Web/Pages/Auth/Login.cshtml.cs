using Algora.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Algora.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthApiClient _authClient;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAuthApiClient authClient, ILogger<LoginModel> logger)
    {
        _authClient = authClient;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/dashboard");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/dashboard");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await _authClient.LoginAsync(new LoginRequest(Input.Email, Input.Password));

            if (response is null)
            {
                ErrorMessage = "Invalid email or password";
                return Page();
            }

            // Create claims from the auth response
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, Input.Email),
                new("access_token", response.AccessToken ?? ""),
                new("refresh_token", response.RefreshToken ?? ""),
                new("expires_at", response.ExpiresAt?.ToString("o") ?? "")
            };

            // Add user info claims if available
            if (response.User is not null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()));
                if (!string.IsNullOrEmpty(response.User.FirstName))
                    claims.Add(new Claim(ClaimTypes.GivenName, response.User.FirstName));
                if (!string.IsNullOrEmpty(response.User.Role))
                    claims.Add(new Claim(ClaimTypes.Role, response.User.Role));
                if (!string.IsNullOrEmpty(response.User?.ShopDomain))
                    claims.Add(new Claim("shop_domain", response.User.ShopDomain));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = response.ExpiresAt
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Email} logged in successfully", Input.Email);

            return LocalRedirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {Email}", Input.Email);
            ErrorMessage = "An error occurred during login. Please try again.";
            return Page();
        }
    }
}