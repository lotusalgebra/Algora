namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a notification sent to customer or merchant.
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public string NotificationType { get; set; } = string.Empty; // email, sms, push
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? Recipient { get; set; } // email address or phone
        public string Status { get; set; } = "pending"; // pending, sent, failed, delivered
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}