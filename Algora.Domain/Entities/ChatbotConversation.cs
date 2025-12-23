namespace Algora.Domain.Entities;

public class ChatbotConversation
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = "";
    public string SessionId { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? CustomerEmail { get; set; }
    public string Status { get; set; } = "active"; // active, resolved, escalated
    public string? Topic { get; set; }
    public int? RelatedOrderId { get; set; }
    public Order? RelatedOrder { get; set; }
    public bool? WasHelpful { get; set; }
    public DateTime? EscalatedToAgentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public ICollection<ChatbotMessage> Messages { get; set; } = new List<ChatbotMessage>();
}
