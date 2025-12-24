namespace Algora.Domain.Entities
{
    /// <summary>
    /// Rules for detecting inactive customers eligible for win-back campaigns.
    /// </summary>
    public class WinbackRule
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public int AutomationId { get; set; }
        public EmailAutomation Automation { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public int DaysInactive { get; set; } = 60; // days since last purchase
        public decimal? MinimumLifetimeValue { get; set; } // minimum LTV to qualify
        public int? MinimumOrders { get; set; } // minimum order count to qualify
        public int? MaximumOrders { get; set; } // maximum order count (exclude VIPs if needed)
        public bool ExcludeRecentSubscribers { get; set; } // exclude recent email subscribers
        public int? ExcludeSubscribedWithinDays { get; set; }
        public string? CustomerTags { get; set; } // JSON array of tags to include
        public string? ExcludeTags { get; set; } // JSON array of tags to exclude
        public bool IsActive { get; set; } = true;
        public DateTime? LastRunAt { get; set; }
        public int CustomersEnrolledLastRun { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
