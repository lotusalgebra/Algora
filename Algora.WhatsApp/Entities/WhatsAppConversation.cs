namespace Algora.WhatsApp.Entities;

/// <summary>
/// Represents a WhatsApp conversation thread with a customer.
/// </summary>
public class WhatsAppConversation
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string? ExternalConversationId { get; set; }
    public int? CustomerId { get; set; } // Reference to Customer in main domain
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = "open"; // open, closed, pending
    public string? AssignedTo { get; set; } // staff user ID or name
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
    public bool IsBusinessInitiated { get; set; }
    public DateTime? WindowExpiresAt { get; set; } // 24-hour messaging window
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();
}
