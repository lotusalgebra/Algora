namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a member of a customer segment.
    /// </summary>
    public class CustomerSegmentMember
    {
        public int Id { get; set; }
        public int SegmentId { get; set; }
        public CustomerSegment Segment { get; set; } = null!;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? SubscriberId { get; set; }
        public EmailSubscriber? Subscriber { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}