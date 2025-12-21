using System.ComponentModel.DataAnnotations;

namespace Algora.Auth.Models;

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; init; } = string.Empty;

    public string? ShopDomain { get; init; }
}

public record RegisterRequest
{
    [Required]
    public string? ShopDomain { get; init; } = null;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; init; } = string.Empty;

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Role { get; init; } = "Admin";
}

public record AuthResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public UserInfo? User { get; init; }
}

public record UserInfo
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ShopDomain { get; init; }
    public string? Role { get; init; }
}

public record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;
}

public record ShopifyInstallRequest
{
    [Required]
    public string Shop { get; init; } = string.Empty;
}

public record ShopifyCallbackRequest
{
    public string Shop { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Hmac { get; init; } = string.Empty;
    public string? Timestamp { get; init; }
}

public record ShopifyAuthResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ShopDomain { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public UserInfo? User { get; init; }
    public string? RedirectUrl { get; init; }
}