namespace Algora.Domain.Entities;

public class ChatbotMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public ChatbotConversation Conversation { get; set; } = null!;
    public string Role { get; set; } = "user"; // user, assistant, system
    public string Content { get; set; } = "";
    public string? Intent { get; set; }
    public decimal? Confidence { get; set; }
    public string? SuggestedActions { get; set; }
    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
