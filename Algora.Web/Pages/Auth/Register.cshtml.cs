using Algora.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Algora.Web.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthApiClient _authClient;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IAuthApiClient authClient, ILogger<RegisterModel> logger)
    {
        _authClient = authClient;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Shop URL is required")]
        [RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9\-]*\.myshopify\.com$", 
            ErrorMessage = "Please enter a valid Shopify store URL (e.g., your-store.myshopify.com)")]
        [Display(Name = "Shop URL")]
        public string ShopDomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
        public bool AgreeTerms { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Parse name into first and last name
            var nameParts = Input.Name.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : null;

            var response = await _authClient.RegisterAsync(new RegisterRequest
            {
                ShopDomain = Input.ShopDomain,
                Email = Input.Email,
                Password = Input.Password,
                FirstName = firstName,
                LastName = lastName,
                Role = "Admin"
            });

            if (response is null || !response.Success)
            {
                ErrorMessage = response?.Message ?? "Registration failed. Please try again.";
                return Page();
            }

            _logger.LogInformation("User {Email} registered successfully for shop {Shop}", Input.Email, Input.ShopDomain);

            TempData["SuccessMessage"] = "Registration successful! Please sign in.";
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {Email}", Input.Email);
            ErrorMessage = "An error occurred during registration. Please try again.";
            return Page();
        }
    }
}