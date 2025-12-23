namespace Algora.Domain.Entities;

public class ProductSeoData
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Keywords { get; set; }
    public string? FocusKeyword { get; set; }
    public int? SeoScore { get; set; }
    public string? Provider { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
