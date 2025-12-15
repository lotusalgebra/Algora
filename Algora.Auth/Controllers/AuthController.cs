using Algora.Auth.Models;
using Algora.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Algora.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IShopifyAuthService _shopifyAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IShopifyAuthService shopifyAuthService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _shopifyAuthService = shopifyAuthService;
        _logger = logger;
    }

    /// <summary>
    /// User login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Register a new user for a shop
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(AuthResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfo), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _authService.GetUserByIdAsync(userId);
        return user is not null ? Ok(user) : NotFound();
    }

    // ==================== Shopify OAuth Endpoints ====================

    /// <summary>
    /// Initiate Shopify OAuth install flow
    /// </summary>
    [HttpGet("shopify/install")]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ShopifyInstall([FromQuery] string shop)
    {
        if (string.IsNullOrWhiteSpace(shop))
            return BadRequest(new { message = "shop parameter is required" });

        // Normalize shop domain
        if (!shop.EndsWith(".myshopify.com"))
            shop = $"{shop}.myshopify.com";

        var state = Guid.NewGuid().ToString("N");

        // Store state in cookie for validation
        Response.Cookies.Append("shopify_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        var installUrl = await _shopifyAuthService.GetInstallUrlAsync(shop, state);
        return Redirect(installUrl);
    }

    /// <summary>
    /// Handle Shopify OAuth callback
    /// </summary>
    [HttpGet("shopify/callback")]
    [ProducesResponseType(typeof(ShopifyAuthResponse), 200)]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ShopifyCallback(
        [FromQuery] string shop,
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string hmac,
        [FromQuery] string? timestamp)
    {
        // Get saved state from cookie
        if (!Request.Cookies.TryGetValue("shopify_state", out var savedState))
            return BadRequest(new { message = "State cookie not found" });

        // Clear state cookie
        Response.Cookies.Delete("shopify_state");

        var request = new ShopifyCallbackRequest
        {
            Shop = shop,
            Code = code,
            State = state,
            Hmac = hmac,
            Timestamp = timestamp
        };

        var result = await _shopifyAuthService.HandleCallbackAsync(request, savedState);

        if (!result.Success)
            return BadRequest(result);

        // For API clients, return JSON
        if (Request.Headers.Accept.Any(h => h?.Contains("application/json") == true))
            return Ok(result);

        // For browser, set cookie and redirect
        Response.Cookies.Append("access_token", result.AccessToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Redirect(result.RedirectUrl ?? "/dashboard");
    }

    /// <summary>
    /// Validate Shopify webhook HMAC
    /// </summary>
    [HttpPost("shopify/validate-webhook")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> ValidateWebhook([FromBody] Dictionary<string, string> queryParams, [FromQuery] string hmac)
    {
        var shop = queryParams.GetValueOrDefault("shop", "");
        var isValid = await _shopifyAuthService.ValidateHmacAsync(shop, queryParams, hmac);
        return Ok(new { valid = isValid });
    }
}