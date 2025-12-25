namespace Algora.Chatbot.Domain.Entities;

public class KnowledgeArticle
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? KeyPhrases { get; set; }

    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
