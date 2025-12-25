namespace Algora.Chatbot.Domain.Entities;

public class License
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public string Status { get; set; } = "trial";
    public string? ShopifyChargeId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public int TrialDaysRemaining { get; set; }

    // Usage Tracking
    public int ConversationsThisMonth { get; set; }
    public int MessagesThisMonth { get; set; }
    public DateTime UsagePeriodStart { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
