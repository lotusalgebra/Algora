namespace Algora.Domain.Entities
{
    /// <summary>
    /// Many-to-many relationship between email lists and subscribers.
    /// </summary>
    public class EmailListSubscriber
    {
        public int Id { get; set; }
        public int EmailListId { get; set; }
        public EmailList EmailList { get; set; } = null!;
        public int EmailSubscriberId { get; set; }
        public EmailSubscriber EmailSubscriber { get; set; } = null!;
        public string Status { get; set; } = "subscribed"; // subscribed, unsubscribed
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnsubscribedAt { get; set; }
    }
}