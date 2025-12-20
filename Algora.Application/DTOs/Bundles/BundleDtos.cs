namespace Algora.Application.DTOs.Bundles;

#region Bundle DTOs

/// <summary>
/// DTO for bundle display.
/// </summary>
public class BundleDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BundleType { get; set; } = "fixed";
    public string Status { get; set; } = "draft";
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public string? DiscountCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public long? ShopifyProductId { get; set; }
    public long? ShopifyVariantId { get; set; }
    public string ShopifySyncStatus { get; set; } = "pending";
    public string? ShopifySyncError { get; set; }
    public DateTime? ShopifySyncedAt { get; set; }
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal BundlePrice { get; set; }
    public decimal SavingsAmount { get; set; }
    public decimal SavingsPercent { get; set; }
    public string Currency { get; set; } = "USD";
    public int AvailableQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BundleItemDto> Items { get; set; } = new();
    public List<BundleRuleDto> Rules { get; set; } = new();
}

/// <summary>
/// DTO for creating a new bundle.
/// </summary>
public class CreateBundleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string BundleType { get; set; } = "fixed";
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public string? DiscountCode { get; set; }
    public string? ImageUrl { get; set; }
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
    public bool IsActive { get; set; } = false;
    public List<CreateBundleItemDto> Items { get; set; } = new();
    public List<CreateBundleRuleDto> Rules { get; set; } = new();
}

/// <summary>
/// DTO for updating a bundle.
/// </summary>
public class UpdateBundleDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public string? DiscountCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
    public string? Status { get; set; }
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
}

/// <summary>
/// DTO for bundle list view (minimal data).
/// </summary>
public class BundleListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BundleType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal BundlePrice { get; set; }
    public decimal SavingsPercent { get; set; }
    public int ItemCount { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsActive { get; set; }
    public string ShopifySyncStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

#endregion

#region BundleItem DTOs

/// <summary>
/// DTO for bundle item display.
/// </summary>
public class BundleItemDto
{
    public int Id { get; set; }
    public int BundleId { get; set; }
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int DisplayOrder { get; set; }
    public int CurrentInventory { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
}

/// <summary>
/// DTO for creating a bundle item.
/// </summary>
public class CreateBundleItemDto
{
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public int DisplayOrder { get; set; }
}

#endregion

#region BundleRule DTOs

/// <summary>
/// DTO for bundle rule display.
/// </summary>
public class BundleRuleDto
{
    public int Id { get; set; }
    public int BundleId { get; set; }
    public string? Name { get; set; }
    public List<long> EligibleProductIds { get; set; } = new();
    public List<long> EligibleCollectionIds { get; set; } = new();
    public List<string> EligibleTags { get; set; } = new();
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool AllowDuplicates { get; set; }
    public int DisplayOrder { get; set; }
    public string? DisplayLabel { get; set; }
    public List<BundleRuleTierDto> Tiers { get; set; } = new();
    public List<EligibleProductDto> EligibleProducts { get; set; } = new();
}

/// <summary>
/// DTO for creating a bundle rule.
/// </summary>
public class CreateBundleRuleDto
{
    public string? Name { get; set; }
    public List<long> EligibleProductIds { get; set; } = new();
    public List<long> EligibleCollectionIds { get; set; } = new();
    public List<string> EligibleTags { get; set; } = new();
    public int MinQuantity { get; set; } = 1;
    public int? MaxQuantity { get; set; }
    public bool AllowDuplicates { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? DisplayLabel { get; set; }
    public List<CreateBundleRuleTierDto> Tiers { get; set; } = new();
}

/// <summary>
/// DTO for bundle rule tier display.
/// </summary>
public class BundleRuleTierDto
{
    public int Id { get; set; }
    public int BundleRuleId { get; set; }
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public string? DisplayLabel { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for creating a bundle rule tier.
/// </summary>
public class CreateBundleRuleTierDto
{
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
    public string? DisplayLabel { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for eligible product in mix-and-match selection.
/// </summary>
public class EligibleProductDto
{
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int InventoryQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
}

#endregion

#region BundleSettings DTOs

/// <summary>
/// DTO for bundle settings display.
/// </summary>
public class BundleSettingsDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string DefaultDiscountType { get; set; } = "percentage";
    public decimal DefaultDiscountValue { get; set; }
    public bool ShowInventoryWarnings { get; set; }
    public int LowInventoryThreshold { get; set; }
    public bool HideOutOfStock { get; set; }
    public string? BundlePageTitle { get; set; }
    public string? BundlePageDescription { get; set; }
    public string DisplayLayout { get; set; } = "grid";
    public int BundlesPerPage { get; set; }
    public bool ShowOnStorefront { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CustomCss { get; set; }
    public bool AutoSyncToShopify { get; set; }
    public string? ShopifyProductType { get; set; }
    public string? ShopifyProductTags { get; set; }
}

/// <summary>
/// DTO for updating bundle settings.
/// </summary>
public class UpdateBundleSettingsDto
{
    public bool? IsEnabled { get; set; }
    public string? DefaultDiscountType { get; set; }
    public decimal? DefaultDiscountValue { get; set; }
    public bool? ShowInventoryWarnings { get; set; }
    public int? LowInventoryThreshold { get; set; }
    public bool? HideOutOfStock { get; set; }
    public string? BundlePageTitle { get; set; }
    public string? BundlePageDescription { get; set; }
    public string? DisplayLayout { get; set; }
    public int? BundlesPerPage { get; set; }
    public bool? ShowOnStorefront { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CustomCss { get; set; }
    public bool? AutoSyncToShopify { get; set; }
    public string? ShopifyProductType { get; set; }
    public string? ShopifyProductTags { get; set; }
}

#endregion

#region Customer Selection DTOs

/// <summary>
/// DTO for customer's bundle selection (mix-and-match).
/// </summary>
public class CustomerBundleSelectionDto
{
    public int BundleId { get; set; }
    public List<SelectedItemDto> SelectedItems { get; set; } = new();
}

/// <summary>
/// DTO for a selected item in a mix-and-match bundle.
/// </summary>
public class SelectedItemDto
{
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// DTO for bundle price calculation result.
/// </summary>
public class BundlePriceCalculationDto
{
    public int BundleId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal SavingsPercent { get; set; }
    public string? DiscountCode { get; set; }
    public string? AppliedTierLabel { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    public List<SelectedItemDetailDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for selected item details in price calculation.
/// </summary>
public class SelectedItemDetailDto
{
    public long PlatformProductId { get; set; }
    public long? PlatformVariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// DTO for cart URL generation result.
/// </summary>
public class BundleCartUrlDto
{
    public string CartUrl { get; set; } = string.Empty;
    public string? DiscountCode { get; set; }
    public decimal TotalPrice { get; set; }
    public int TotalItems { get; set; }
}

#endregion

#region Analytics DTOs

/// <summary>
/// DTO for bundle analytics summary.
/// </summary>
public class BundleAnalyticsSummaryDto
{
    public int TotalBundles { get; set; }
    public int ActiveBundles { get; set; }
    public int FixedBundles { get; set; }
    public int MixAndMatchBundles { get; set; }
    public int SyncedToShopify { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<BundlePerformanceDto> TopBundles { get; set; } = new();
}

/// <summary>
/// DTO for individual bundle performance.
/// </summary>
public class BundlePerformanceDto
{
    public int BundleId { get; set; }
    public string BundleName { get; set; } = string.Empty;
    public int Views { get; set; }
    public int AddToCarts { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public decimal ConversionRate { get; set; }
}

#endregion
