using Algora.Domain.Entities;

namespace Algora.Infrastructure.Services.Scrapers;

/// <summary>
/// Result of parsing a product URL
/// </summary>
public class ParsedProductInfo
{
    public string SourceType { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public double? AverageRating { get; set; }
    public int? TotalReviews { get; set; }
}

/// <summary>
/// Scraped review data
/// </summary>
public class ScrapedReview
{
    public string ExternalReviewId { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime ReviewDate { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public List<ScrapedReviewMedia> Media { get; set; } = [];
}

/// <summary>
/// Scraped media attached to a review
/// </summary>
public class ScrapedReviewMedia
{
    public string MediaType { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Scraping progress callback
/// </summary>
public class ScrapeProgress
{
    public int TotalReviews { get; set; }
    public int ProcessedReviews { get; set; }
    public int ImportedReviews { get; set; }
    public int SkippedReviews { get; set; }
    public string? CurrentStatus { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Interface for review scrapers
/// </summary>
public interface IReviewScraper
{
    /// <summary>
    /// The source type this scraper handles (e.g., "amazon", "aliexpress")
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// Check if this scraper can handle the given URL
    /// </summary>
    bool CanHandle(string url);

    /// <summary>
    /// Parse product information from the URL
    /// </summary>
    Task<ParsedProductInfo?> ParseProductUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrape reviews from the product page
    /// </summary>
    /// <param name="productId">The external product ID</param>
    /// <param name="options">Scraping options</param>
    /// <param name="progressCallback">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    IAsyncEnumerable<ScrapedReview> ScrapeReviewsAsync(
        string productId,
        ScrapeOptions options,
        Action<ScrapeProgress>? progressCallback = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for scraping reviews
/// </summary>
public class ScrapeOptions
{
    public int? MinRating { get; set; }
    public bool IncludePhotosOnly { get; set; }
    public DateTime? ReviewsAfterDate { get; set; }
    public int? MaxReviews { get; set; }
}
