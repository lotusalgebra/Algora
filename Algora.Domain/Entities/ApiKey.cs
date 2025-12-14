namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an API key for external integrations.
    /// </summary>
    public class ApiKey
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string? SecretHash { get; set; }
        public string? Scopes { get; set; } // comma-separated permissions
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}