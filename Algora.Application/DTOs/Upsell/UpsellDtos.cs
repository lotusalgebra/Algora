namespace Algora.Application.DTOs.Upsell;

/// <summary>
/// DTO for displaying upsell offers to customers.
/// </summary>
public record UpsellOfferDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }

    public long RecommendedProductId { get; init; }
    public long? RecommendedVariantId { get; init; }
    public string RecommendedProductTitle { get; init; } = string.Empty;
    public string? RecommendedProductImageUrl { get; init; }
    public decimal RecommendedProductPrice { get; init; }
    public decimal? DiscountedPrice { get; init; }

    public string? DiscountType { get; init; }
    public decimal? DiscountValue { get; init; }
    public string? DiscountCode { get; init; }

    public string? Headline { get; init; }
    public string? BodyText { get; init; }
    public string? ButtonText { get; init; }

    public int Priority { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<long>? TriggerProductIds { get; init; }

    public string RecommendationSource { get; init; } = "manual";
    public string? CartUrl { get; init; }

    // Statistics
    public int Impressions { get; init; }
    public int Clicks { get; init; }
    public int Conversions { get; init; }
    public decimal Revenue { get; init; }
}

/// <summary>
/// DTO for creating a new upsell offer.
/// </summary>
public record CreateUpsellOfferDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<long>? TriggerProductIds { get; set; }
    public long RecommendedProductId { get; init; }
    public long? RecommendedVariantId { get; init; }
    public string? DiscountType { get; init; }
    public decimal? DiscountValue { get; init; }
    public string? DiscountCode { get; init; }
    public string? Headline { get; init; }
    public string? BodyText { get; init; }
    public string? ButtonText { get; init; }
    public int DisplayOrder { get; init; }
    public int Priority { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// DTO for product affinity relationships.
/// </summary>
public record ProductAffinityDto
{
    public int Id { get; init; }
    public long SourceProductId { get; init; }
    public string SourceProductTitle { get; init; } = string.Empty;
    public long RelatedProductId { get; init; }
    public string RelatedProductTitle { get; init; } = string.Empty;
    public int CoOccurrences { get; init; }
    public decimal Support { get; init; }
    public decimal Confidence { get; init; }
    public decimal Lift { get; init; }
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// DTO for the order confirmation page.
/// </summary>
public record OrderConfirmationPageDto
{
    public OrderSummaryDto Order { get; init; } = null!;
    public List<UpsellOfferDto> Offers { get; init; } = new();
    public string SessionId { get; init; } = string.Empty;
    public UpsellPageSettingsDto Settings { get; init; } = null!;
}

/// <summary>
/// DTO for order summary on the confirmation page.
/// </summary>
public record OrderSummaryDto
{
    public long OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public List<OrderItemSummaryDto> Items { get; init; } = new();
    public DateTime OrderDate { get; init; }
}

/// <summary>
/// DTO for order line item summary.
/// </summary>
public record OrderItemSummaryDto
{
    public string ProductTitle { get; init; } = string.Empty;
    public string? VariantTitle { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

/// <summary>
/// DTO for upsell page display settings.
/// </summary>
public record UpsellPageSettingsDto
{
    public string? PageTitle { get; init; }
    public string? ThankYouMessage { get; init; }
    public string? UpsellSectionTitle { get; init; }
    public string DisplayLayout { get; init; } = "carousel";
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? CustomCss { get; init; }
}

/// <summary>
/// DTO for upsell settings configuration.
/// </summary>
public record UpsellSettingsDto
{
    public int Id { get; init; }
    public bool IsEnabled { get; init; }
    public bool ShowOnConfirmationPage { get; init; }
    public bool SendUpsellEmail { get; init; }
    public int MaxOffersToShow { get; init; }
    public string DisplayLayout { get; init; } = "carousel";
    public int AffinityLookbackDays { get; init; }
    public decimal MinimumConfidenceScore { get; init; }
    public decimal MinAffinityConfidence { get; init; }
    public int MinimumCoOccurrences { get; init; }
    public string? PageTitle { get; init; }
    public string? ThankYouMessage { get; init; }
    public string? UpsellSectionTitle { get; init; }
    public string? CustomCss { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public bool EnableAutoRecommendations { get; init; }
    public bool EnableAbTesting { get; init; }
    public bool TrackImpressions { get; init; }
}

/// <summary>
/// DTO for updating upsell settings.
/// </summary>
public record UpdateUpsellSettingsDto
{
    public bool? IsEnabled { get; init; }
    public bool? ShowOnConfirmationPage { get; init; }
    public bool? SendUpsellEmail { get; init; }
    public int? MaxOffersToShow { get; init; }
    public string? DisplayLayout { get; init; }
    public int? AffinityLookbackDays { get; init; }
    public decimal? MinimumConfidenceScore { get; init; }
    public decimal? MinAffinityConfidence { get; init; }
    public int? MinimumCoOccurrences { get; init; }
    public string? PageTitle { get; init; }
    public string? ThankYouMessage { get; init; }
    public string? UpsellSectionTitle { get; init; }
    public string? CustomCss { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public bool EnableAutoRecommendations { get; init; }
    public bool EnableAbTesting { get; init; }
    public bool TrackImpressions { get; init; }
}

/// <summary>
/// DTO for product affinity summary statistics.
/// </summary>
public record ProductAffinitySummaryDto
{
    public int TotalAffinities { get; init; }
    public int HighConfidenceCount { get; init; }
    public int ProductsWithAffinities { get; init; }
    public int OrdersAnalyzed { get; init; }
    public DateTime? LastCalculatedAt { get; init; }
}

/// <summary>
/// DTO for affinity summary on the admin page.
/// </summary>
public record AffinitySummaryDto
{
    public int TotalAffinities { get; init; }
    public int StrongAffinities { get; init; }
    public decimal AverageConfidence { get; init; }
    public DateTime? LastCalculated { get; init; }
}

/// <summary>
/// Request for adding items to cart.
/// </summary>
public record CartItemRequest(long VariantId, int Quantity);
