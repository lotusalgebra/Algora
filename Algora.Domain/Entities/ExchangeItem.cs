namespace Algora.Domain.Entities;

/// <summary>
/// Represents an item being exchanged with its replacement selection.
/// </summary>
public class ExchangeItem
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the exchange.
    /// </summary>
    public int ExchangeId { get; set; }

    /// <summary>
    /// Navigation property to the exchange.
    /// </summary>
    public Exchange Exchange { get; set; } = null!;

    // Original item being returned

    /// <summary>
    /// Original order line ID.
    /// </summary>
    public int OriginalOrderLineId { get; set; }

    /// <summary>
    /// Original product ID.
    /// </summary>
    public int OriginalProductId { get; set; }

    /// <summary>
    /// Original product variant ID.
    /// </summary>
    public int? OriginalProductVariantId { get; set; }

    /// <summary>
    /// Original product title (denormalized).
    /// </summary>
    public string OriginalProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Original variant title (denormalized).
    /// </summary>
    public string? OriginalVariantTitle { get; set; }

    /// <summary>
    /// Original SKU (denormalized).
    /// </summary>
    public string? OriginalSku { get; set; }

    /// <summary>
    /// Original item price.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Quantity being exchanged.
    /// </summary>
    public int Quantity { get; set; }

    // New item being sent

    /// <summary>
    /// Foreign key to the new product.
    /// </summary>
    public int? NewProductId { get; set; }

    /// <summary>
    /// Navigation property to the new product.
    /// </summary>
    public Product? NewProduct { get; set; }

    /// <summary>
    /// Foreign key to the new product variant.
    /// </summary>
    public int? NewProductVariantId { get; set; }

    /// <summary>
    /// Navigation property to the new product variant.
    /// </summary>
    public ProductVariant? NewProductVariant { get; set; }

    /// <summary>
    /// New product title (denormalized).
    /// </summary>
    public string? NewProductTitle { get; set; }

    /// <summary>
    /// New variant title (denormalized).
    /// </summary>
    public string? NewVariantTitle { get; set; }

    /// <summary>
    /// New SKU (denormalized).
    /// </summary>
    public string? NewSku { get; set; }

    /// <summary>
    /// New item price.
    /// </summary>
    public decimal? NewPrice { get; set; }

    /// <summary>
    /// Reason for the exchange.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Additional note from the customer.
    /// </summary>
    public string? CustomerNote { get; set; }
}
