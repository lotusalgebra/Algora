namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an application user (merchant staff member).
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; } = "staff"; // owner, admin, staff
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}