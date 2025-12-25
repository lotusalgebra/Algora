namespace Algora.Chatbot.Domain.Entities;

public class WidgetConfiguration
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Position & Display
    public string Position { get; set; } = "bottom-right";
    public int OffsetX { get; set; } = 20;
    public int OffsetY { get; set; } = 20;
    public string TriggerStyle { get; set; } = "bubble";

    // Colors
    public string PrimaryColor { get; set; } = "#7c3aed";
    public string SecondaryColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#333333";
    public string HeaderBackgroundColor { get; set; } = "#7c3aed";
    public string HeaderTextColor { get; set; } = "#ffffff";

    // Branding
    public string? LogoUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public string HeaderTitle { get; set; } = "Chat with us";
    public string TriggerText { get; set; } = "Need help?";

    // Behavior
    public bool AutoOpenOnFirstVisit { get; set; } = false;
    public int AutoOpenDelaySeconds { get; set; } = 5;
    public bool ShowTypingIndicator { get; set; } = true;
    public bool ShowTimestamps { get; set; } = true;
    public bool EnableSoundNotifications { get; set; } = true;

    // Input Options
    public string? PlaceholderText { get; set; } = "Type your message...";
    public bool EnableFileUpload { get; set; } = false;
    public bool EnableEmoji { get; set; } = true;

    // Custom CSS
    public string? CustomCss { get; set; }

    // Powered by badge
    public bool ShowPoweredBy { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
