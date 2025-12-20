namespace Algora.Domain.Entities;

/// <summary>
/// Per-shop review widget and import settings.
/// </summary>
public class ReviewSettings
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;

    // Widget Settings
    /// <summary>
    /// Widget theme: light, dark, auto
    /// </summary>
    public string WidgetTheme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#000000";
    public string AccentColor { get; set; } = "#f5a623";
    public string StarColor { get; set; } = "#ffc107";
    /// <summary>
    /// Widget layout: grid, list, carousel
    /// </summary>
    public string WidgetLayout { get; set; } = "list";
    public int ReviewsPerPage { get; set; } = 10;
    public bool ShowReviewerName { get; set; } = true;
    public bool ShowReviewDate { get; set; } = true;
    public bool ShowVerifiedBadge { get; set; } = true;
    public bool ShowPhotoGallery { get; set; } = true;
    public bool AllowCustomerReviews { get; set; } = true;
    public bool RequireApproval { get; set; } = true;

    // Auto-moderation
    public bool AutoApproveReviews { get; set; }
    public int? AutoApproveMinRating { get; set; } = 4; // Auto-approve if rating >= this
    public bool AutoApproveVerifiedOnly { get; set; } = true;

    // Import Settings
    public bool TranslateImportedReviews { get; set; }
    public string? TranslateToLanguage { get; set; }
    public bool RemoveSourceBranding { get; set; } = true;
    public bool ImportPhotos { get; set; } = true;

    // API Key for widget embedding
    public string WidgetApiKey { get; set; } = string.Empty;

    // Email Settings
    public string? DefaultEmailFromName { get; set; }
    public string? DefaultEmailFromAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
