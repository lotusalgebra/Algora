namespace Algora.Domain.Entities;

/// <summary>
/// Represents an application user for authentication.
/// </summary>
public class AppUser
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; } // Owner, Admin, Staff, etc.
    public bool IsActive { get; set; } = true;

    // Token management
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Timestamps
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}