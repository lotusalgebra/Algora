namespace Algora.Chatbot.Application.DTOs;

public record DashboardStatsDto
{
    public int TotalConversationsToday { get; init; }
    public int TotalConversationsThisMonth { get; init; }
    public int ActiveConversations { get; init; }
    public int EscalatedConversations { get; init; }
    public double ResolutionRate { get; init; }
    public double AvgRating { get; init; }
    public double AvgResponseTimeSeconds { get; init; }
    public double AvgConversationDurationMinutes { get; init; }
    public decimal TotalAiCostThisMonth { get; init; }
    public Dictionary<string, int> IntentDistribution { get; init; } = new();
    public List<ConversationTrendDto> RecentTrend { get; init; } = new();
}

public record ConversationTrendDto
{
    public DateTime Date { get; init; }
    public int Count { get; init; }
    public int Resolved { get; init; }
    public int Escalated { get; init; }
}

public record AnalyticsSnapshotDto
{
    public DateTime SnapshotDate { get; init; }
    public int TotalConversations { get; init; }
    public int ResolvedConversations { get; init; }
    public int EscalatedConversations { get; init; }
    public double AvgResponseTimeSeconds { get; init; }
    public double AvgRating { get; init; }
    public double HelpfulPercentage { get; init; }
    public decimal TotalAiCost { get; init; }
    public Dictionary<string, int> IntentDistribution { get; init; } = new();
}
