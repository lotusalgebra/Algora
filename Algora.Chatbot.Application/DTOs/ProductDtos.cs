namespace Algora.Chatbot.Application.DTOs;

public record ProductRecommendationDto
{
    public long ProductId { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public string? ImageUrl { get; init; }
    public string? ProductUrl { get; init; }
    public bool InStock { get; init; }
    public string? RecommendationReason { get; init; }
}

public record ShopifyProductDto
{
    public long Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? Vendor { get; init; }
    public string? ProductType { get; init; }
    public List<string>? Tags { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public string? ImageUrl { get; init; }
    public string? Handle { get; init; }
    public bool Available { get; init; }
    public int InventoryQuantity { get; init; }
    public List<ProductVariantDto>? Variants { get; init; }
}

public record ProductVariantDto
{
    public long Id { get; init; }
    public string Title { get; init; } = "";
    public decimal Price { get; init; }
    public string? Sku { get; init; }
    public int InventoryQuantity { get; init; }
    public bool Available { get; init; }
}
