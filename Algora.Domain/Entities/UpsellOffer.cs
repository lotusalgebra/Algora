namespace Algora.Domain.Entities;

/// <summary>
/// Represents a configured upsell offer that can be displayed
/// on the post-purchase confirmation page.
/// </summary>
public class UpsellOffer
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this offer belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Offer configuration
    /// <summary>
    /// Name of the offer (for admin reference).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the offer.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this offer is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Targeting: Which trigger products activate this offer
    /// <summary>
    /// JSON array of platform product IDs that trigger this offer.
    /// Empty or null means all products trigger this offer.
    /// </summary>
    public string? TriggerProductIds { get; set; }

    // What to recommend
    /// <summary>
    /// Platform product ID of the recommended product.
    /// </summary>
    public long RecommendedProductId { get; set; }

    /// <summary>
    /// Optional specific variant ID to recommend.
    /// </summary>
    public long? RecommendedVariantId { get; set; }

    /// <summary>
    /// Recommended product title (denormalized for display).
    /// </summary>
    public string RecommendedProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Recommended product image URL.
    /// </summary>
    public string? RecommendedProductImageUrl { get; set; }

    /// <summary>
    /// Recommended product price.
    /// </summary>
    public decimal RecommendedProductPrice { get; set; }

    // Discount configuration (optional)
    /// <summary>
    /// Type of discount: percentage, fixed_amount, or null for no discount.
    /// </summary>
    public string? DiscountType { get; set; }

    /// <summary>
    /// Discount value (percentage or fixed amount).
    /// </summary>
    public decimal? DiscountValue { get; set; }

    /// <summary>
    /// Pre-created Shopify discount code to apply.
    /// </summary>
    public string? DiscountCode { get; set; }

    // Display settings
    /// <summary>
    /// Headline text displayed on the offer.
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// Body text displayed on the offer.
    /// </summary>
    public string? BodyText { get; set; }

    /// <summary>
    /// Button text (e.g., "Add to Cart", "Get It Now").
    /// </summary>
    public string? ButtonText { get; set; }

    /// <summary>
    /// Display order for sorting multiple offers.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Recommendation source
    /// <summary>
    /// Source of this recommendation: manual, affinity, ai.
    /// </summary>
    public string RecommendationSource { get; set; } = "manual";

    /// <summary>
    /// Foreign key to product affinity if source is affinity-based.
    /// </summary>
    public int? ProductAffinityId { get; set; }

    /// <summary>
    /// Navigation property to product affinity.
    /// </summary>
    public ProductAffinity? ProductAffinity { get; set; }

    // Experiment assignment
    /// <summary>
    /// Foreign key to experiment if this offer is part of an A/B test.
    /// </summary>
    public int? ExperimentId { get; set; }

    /// <summary>
    /// Navigation property to experiment.
    /// </summary>
    public UpsellExperiment? Experiment { get; set; }

    /// <summary>
    /// Which variant this offer represents: control, variant_a, variant_b, etc.
    /// </summary>
    public string? ExperimentVariant { get; set; }

    // Timestamps
    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
