namespace Algora.Domain.Entities;

/// <summary>
/// Represents a product bundle configuration.
/// Supports both fixed bundles (predefined products) and mix-and-match bundles (customer picks items).
/// </summary>
public class Bundle
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this bundle belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Bundle name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for the bundle page.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Bundle description (can contain HTML).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of bundle: "fixed" or "mix_and_match".
    /// </summary>
    public string BundleType { get; set; } = "fixed";

    /// <summary>
    /// Bundle status: "draft", "active", or "archived".
    /// </summary>
    public string Status { get; set; } = "draft";

    // Discount configuration

    /// <summary>
    /// Discount type: "percentage" or "fixed_amount".
    /// </summary>
    public string DiscountType { get; set; } = "percentage";

    /// <summary>
    /// Discount value (e.g., 15 for 15% or 10.00 for $10 off).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Manual discount code to show customers (created by admin in Shopify).
    /// </summary>
    public string? DiscountCode { get; set; }

    // Display

    /// <summary>
    /// Main bundle image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Thumbnail image URL.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether the bundle is active and visible to customers.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Shopify sync

    /// <summary>
    /// Shopify product ID if synced.
    /// </summary>
    public long? ShopifyProductId { get; set; }

    /// <summary>
    /// Shopify variant ID for the bundle product.
    /// </summary>
    public long? ShopifyVariantId { get; set; }

    /// <summary>
    /// Sync status: "pending", "synced", "error".
    /// </summary>
    public string ShopifySyncStatus { get; set; } = "pending";

    /// <summary>
    /// Error message if sync failed.
    /// </summary>
    public string? ShopifySyncError { get; set; }

    /// <summary>
    /// When the bundle was last synced to Shopify.
    /// </summary>
    public DateTime? ShopifySyncedAt { get; set; }

    // Mix-and-match configuration

    /// <summary>
    /// Minimum number of items for mix-and-match bundles.
    /// </summary>
    public int? MinItems { get; set; }

    /// <summary>
    /// Maximum number of items for mix-and-match bundles.
    /// </summary>
    public int? MaxItems { get; set; }

    // Pricing (cached for display)

    /// <summary>
    /// Original price before discount (sum of components for fixed bundles).
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Final bundle price after discount.
    /// </summary>
    public decimal BundlePrice { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    // Timestamps

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Items in this bundle (for fixed bundles).
    /// </summary>
    public ICollection<BundleItem> Items { get; set; } = new List<BundleItem>();

    /// <summary>
    /// Rules for this bundle (for mix-and-match bundles).
    /// </summary>
    public ICollection<BundleRule> Rules { get; set; } = new List<BundleRule>();
}
