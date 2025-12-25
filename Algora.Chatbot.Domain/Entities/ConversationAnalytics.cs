namespace Algora.Chatbot.Domain.Entities;

public class ConversationAnalytics
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    public DateTime SnapshotDate { get; set; }
    public string PeriodType { get; set; } = "daily";

    // Volume Metrics
    public int TotalConversations { get; set; }
    public int ResolvedConversations { get; set; }
    public int EscalatedConversations { get; set; }
    public int AbandonedConversations { get; set; }

    // Response Metrics
    public double AvgResponseTimeSeconds { get; set; }
    public double AvgConversationDurationMinutes { get; set; }
    public double AvgMessagesPerConversation { get; set; }

    // Satisfaction
    public double AvgRating { get; set; }
    public int TotalRatings { get; set; }
    public double HelpfulPercentage { get; set; }

    // Intent Distribution (JSON)
    public string? IntentDistributionJson { get; set; }

    // AI Cost
    public decimal TotalAiCost { get; set; }
    public int TotalTokensUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
