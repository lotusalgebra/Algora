namespace Algora.Chatbot.Domain.Entities;

public class Shop
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Domain { get; set; } = string.Empty;
    public string? OfflineAccessToken { get; set; }
    public string? ShopName { get; set; }
    public string? Email { get; set; }
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
    public string? Country { get; set; }
    public string? PlanName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime? UninstalledAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    // Navigation properties
    public ChatbotSettings? Settings { get; set; }
    public License? License { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
