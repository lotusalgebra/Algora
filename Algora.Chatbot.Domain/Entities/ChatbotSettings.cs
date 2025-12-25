namespace Algora.Chatbot.Domain.Entities;

public class ChatbotSettings
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // AI Configuration
    public string PreferredAiProvider { get; set; } = "openai";
    public string? FallbackAiProvider { get; set; } = "anthropic";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 500;

    // Personality & Branding
    public string BotName { get; set; } = "Support Assistant";
    public string? WelcomeMessage { get; set; } = "Hi! How can I help you today?";
    public string? CustomInstructions { get; set; }
    public string Tone { get; set; } = "professional";

    // Feature Toggles
    public bool EnableOrderTracking { get; set; } = true;
    public bool EnableProductRecommendations { get; set; } = true;
    public bool EnableReturns { get; set; } = true;
    public bool EnablePolicyLookup { get; set; } = true;
    public bool EnableHumanEscalation { get; set; } = true;

    // Escalation Settings
    public int EscalateAfterMessages { get; set; } = 10;
    public decimal ConfidenceThreshold { get; set; } = 0.6m;
    public string? EscalationEmail { get; set; }
    public string? EscalationWebhookUrl { get; set; }

    // Operating Hours
    public string? OperatingHoursJson { get; set; }
    public string? OutOfHoursMessage { get; set; }

    // Widget Configuration
    public int? WidgetConfigurationId { get; set; }
    public WidgetConfiguration? WidgetConfiguration { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
