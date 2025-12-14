namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an audit log entry for tracking changes.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Create, Update, Delete
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}