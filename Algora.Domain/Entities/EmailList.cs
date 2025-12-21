namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents an email list/audience for marketing.
    /// </summary>
    public class EmailList
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public bool DoubleOptIn { get; set; }
        public int SubscriberCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<EmailListSubscriber> Subscribers { get; set; } = new List<EmailListSubscriber>();
    }
}