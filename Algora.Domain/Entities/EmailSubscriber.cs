namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an email subscriber for marketing communications.
    /// </summary>
    public class EmailSubscriber
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public string Status { get; set; } = "subscribed"; // subscribed, unsubscribed, cleaned, pending
        public string Source { get; set; } = "manual"; // manual, import, popup, checkout, api
        public bool EmailOptIn { get; set; } = true;
        public bool SmsOptIn { get; set; }
        public bool WhatsAppOptIn { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public string? UnsubscribeReason { get; set; }
        public string? Tags { get; set; } // comma-separated
        public string? CustomFields { get; set; } // JSON
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<EmailListSubscriber> ListSubscriptions { get; set; } = new List<EmailListSubscriber>();
    }
}