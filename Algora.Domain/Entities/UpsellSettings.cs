namespace Algora.Domain.Entities;

/// <summary>
/// Per-shop settings for the upsell feature.
/// </summary>
public class UpsellSettings
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain these settings belong to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Feature toggles
    /// <summary>
    /// Whether the upsell feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to show upsells on the confirmation page.
    /// </summary>
    public bool ShowOnConfirmationPage { get; set; } = true;

    /// <summary>
    /// Whether to send upsell offers via email.
    /// </summary>
    public bool SendUpsellEmail { get; set; } = false;

    // Display settings
    /// <summary>
    /// Maximum number of offers to show at once.
    /// </summary>
    public int MaxOffersToShow { get; set; } = 3;

    /// <summary>
    /// Layout type: carousel, grid, single.
    /// </summary>
    public string DisplayLayout { get; set; } = "carousel";

    // Affinity calculation settings
    /// <summary>
    /// Number of days to look back for affinity calculation.
    /// </summary>
    public int AffinityLookbackDays { get; set; } = 90;

    /// <summary>
    /// Minimum confidence score for affinity recommendations.
    /// </summary>
    public decimal MinimumConfidenceScore { get; set; } = 0.1m;

    /// <summary>
    /// Minimum co-occurrences required for affinity.
    /// </summary>
    public int MinimumCoOccurrences { get; set; } = 3;

    // Confirmation page settings
    /// <summary>
    /// Page title for the confirmation page.
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// Thank you message displayed at the top.
    /// </summary>
    public string? ThankYouMessage { get; set; }

    /// <summary>
    /// Title for the upsell section.
    /// </summary>
    public string? UpsellSectionTitle { get; set; }

    /// <summary>
    /// Custom CSS for styling.
    /// </summary>
    public string? CustomCss { get; set; }

    // Branding
    /// <summary>
    /// Logo URL for the confirmation page.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex).
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex).
    /// </summary>
    public string? SecondaryColor { get; set; }

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
