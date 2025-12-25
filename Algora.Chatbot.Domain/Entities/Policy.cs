namespace Algora.Chatbot.Domain.Entities;

public class Policy
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    public string PolicyType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }

    // Key Info (extracted for quick access)
    public int? ReturnWindowDays { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public string? ShippingTimeframe { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
