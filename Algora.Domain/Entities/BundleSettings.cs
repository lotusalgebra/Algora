namespace Algora.Domain.Entities;

/// <summary>
/// Per-shop configuration for the bundle builder feature.
/// </summary>
public class BundleSettings
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain these settings belong to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>
    /// Whether the bundle feature is enabled for this shop.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // Default settings for new bundles

    /// <summary>
    /// Default discount type for new bundles.
    /// </summary>
    public string DefaultDiscountType { get; set; } = "percentage";

    /// <summary>
    /// Default discount value for new bundles.
    /// </summary>
    public decimal DefaultDiscountValue { get; set; } = 10;

    // Inventory settings

    /// <summary>
    /// Whether to show inventory warnings on bundle pages.
    /// </summary>
    public bool ShowInventoryWarnings { get; set; } = true;

    /// <summary>
    /// Threshold for low inventory warnings.
    /// </summary>
    public int LowInventoryThreshold { get; set; } = 5;

    /// <summary>
    /// Whether to hide out-of-stock items from mix-and-match selection.
    /// </summary>
    public bool HideOutOfStock { get; set; } = false;

    // Display settings

    /// <summary>
    /// Bundle listing page title.
    /// </summary>
    public string? BundlePageTitle { get; set; }

    /// <summary>
    /// Bundle listing page description.
    /// </summary>
    public string? BundlePageDescription { get; set; }

    /// <summary>
    /// Display layout: "grid", "list", or "carousel".
    /// </summary>
    public string DisplayLayout { get; set; } = "grid";

    /// <summary>
    /// Number of bundles to show per page.
    /// </summary>
    public int BundlesPerPage { get; set; } = 12;

    /// <summary>
    /// Whether to show bundles on the storefront.
    /// </summary>
    public bool ShowOnStorefront { get; set; } = true;

    // Branding

    /// <summary>
    /// Primary brand color (hex).
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex).
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Custom CSS for bundle pages.
    /// </summary>
    public string? CustomCss { get; set; }

    // Shopify sync settings

    /// <summary>
    /// Whether to automatically sync bundles to Shopify as products.
    /// </summary>
    public bool AutoSyncToShopify { get; set; } = false;

    /// <summary>
    /// Product type to use for synced bundle products in Shopify.
    /// </summary>
    public string? ShopifyProductType { get; set; } = "Bundle";

    /// <summary>
    /// Tags to add to synced bundle products in Shopify.
    /// </summary>
    public string? ShopifyProductTags { get; set; } = "bundle";

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
