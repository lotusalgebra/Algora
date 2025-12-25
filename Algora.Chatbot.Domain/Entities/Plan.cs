namespace Algora.Chatbot.Domain.Entities;

public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }
    public int TrialDays { get; set; } = 14;

    // Limits
    public int ConversationsPerMonth { get; set; }
    public int MessagesPerConversation { get; set; }
    public int KnowledgeArticles { get; set; }

    // Features
    public bool HasMultipleProviders { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
    public bool HasCustomBranding { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasWebhookIntegrations { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
