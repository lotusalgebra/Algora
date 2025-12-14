namespace Algora.Domain.Entities
{
    /// <summary>
    /// Represents a customer segment for targeted marketing.
    /// </summary>
    public class CustomerSegment
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SegmentType { get; set; } = "static"; // static, dynamic
        public string? FilterCriteria { get; set; } // JSON filter rules for dynamic segments
        public int MemberCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastCalculatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<CustomerSegmentMember> Members { get; set; } = new List<CustomerSegmentMember>();
    }
}