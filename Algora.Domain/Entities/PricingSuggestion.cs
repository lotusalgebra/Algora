namespace Algora.Domain.Entities;

public class PricingSuggestion
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = "";
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal ChangePercent { get; set; }
    public string? Reasoning { get; set; }
    public string? Factors { get; set; } // JSON
    public decimal Confidence { get; set; }
    public bool WasApplied { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
