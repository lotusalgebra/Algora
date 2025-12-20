using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algora.Infrastructure.Services.Scrapers;

/// <summary>
/// Scraper for Amazon product reviews using ScraperAPI for anti-bot bypass
/// </summary>
public partial class AmazonReviewScraper : IReviewScraper
{
    private readonly ILogger<AmazonReviewScraper> _logger;
    private readonly HttpClient _httpClient;
    private readonly IBrowsingContext _browsingContext;
    private readonly ScraperApiOptions _options;

    // Rate limiting settings
    private const int MinDelayMs = 1000;
    private const int MaxDelayMs = 2000;
    private const int MaxRetries = 3;
    private const int ReviewsPerPage = 10;

    public string SourceType => "amazon";

    public AmazonReviewScraper(
        ILogger<AmazonReviewScraper> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<ScraperApiOptions> options)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ReviewScraper");
        _browsingContext = BrowsingContext.New(Configuration.Default);
        _options = options.Value;
    }

    public bool CanHandle(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("amazon.", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ParsedProductInfo?> ParseProductUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var asin = ExtractAsin(url);
            if (string.IsNullOrEmpty(asin))
            {
                _logger.LogWarning("Could not extract ASIN from URL: {Url}", url);
                return null;
            }

            var baseUrl = GetBaseUrl(url);
            var productUrl = $"{baseUrl}/dp/{asin}";

            var html = await FetchPageAsync(productUrl, cancellationToken);
            if (string.IsNullOrEmpty(html))
            {
                return new ParsedProductInfo
                {
                    SourceType = SourceType,
                    ProductId = asin,
                    ProductUrl = productUrl
                };
            }

            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            var title = document.QuerySelector("#productTitle")?.TextContent?.Trim()
                ?? document.QuerySelector("[data-feature-name='title'] span")?.TextContent?.Trim()
                ?? document.QuerySelector("#title span")?.TextContent?.Trim()
                ?? document.QuerySelector("h1 span")?.TextContent?.Trim()
                ?? "Unknown Product";

            var imageUrl = document.QuerySelector("#landingImage")?.GetAttribute("src")
                ?? document.QuerySelector("#imgBlkFront")?.GetAttribute("src")
                ?? document.QuerySelector("#main-image")?.GetAttribute("src")
                ?? document.QuerySelector(".a-dynamic-image")?.GetAttribute("src");

            var priceText = document.QuerySelector(".a-price .a-offscreen")?.TextContent?.Trim()
                ?? document.QuerySelector("#priceblock_ourprice")?.TextContent?.Trim()
                ?? document.QuerySelector(".a-price-whole")?.TextContent?.Trim()
                ?? document.QuerySelector("#corePrice_feature_buybox .a-offscreen")?.TextContent?.Trim();
            decimal? price = null;
            if (!string.IsNullOrEmpty(priceText))
            {
                var priceMatch = PriceRegex().Match(priceText);
                if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                {
                    price = parsedPrice;
                }
            }

            var ratingText = document.QuerySelector("#acrPopover")?.GetAttribute("title")
                ?? document.QuerySelector("[data-hook='rating-out-of-text']")?.TextContent
                ?? document.QuerySelector(".a-icon-star span.a-icon-alt")?.TextContent
                ?? document.QuerySelector("#averageCustomerReviews .a-icon-alt")?.TextContent;
            double? averageRating = null;
            if (!string.IsNullOrEmpty(ratingText))
            {
                var ratingMatch = RatingRegex().Match(ratingText);
                if (ratingMatch.Success && double.TryParse(ratingMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRating))
                {
                    averageRating = parsedRating;
                }
            }

            var reviewCountText = document.QuerySelector("#acrCustomerReviewText")?.TextContent
                ?? document.QuerySelector("[data-hook='total-review-count']")?.TextContent
                ?? document.QuerySelector("#averageCustomerReviews_feature_div #acrCustomerReviewLink span")?.TextContent;
            int? totalReviews = null;
            if (!string.IsNullOrEmpty(reviewCountText))
            {
                var countMatch = ReviewCountRegex().Match(reviewCountText);
                if (countMatch.Success && int.TryParse(countMatch.Groups[1].Value.Replace(",", "").Replace(".", ""), out var count))
                {
                    totalReviews = count;
                }
            }

            _logger.LogInformation("Parsed Amazon product: {Title}, Rating: {Rating}, Reviews: {Reviews}",
                title, averageRating, totalReviews);

            return new ParsedProductInfo
            {
                SourceType = SourceType,
                ProductId = asin,
                ProductTitle = title,
                ProductUrl = productUrl,
                ImageUrl = imageUrl,
                Price = price,
                AverageRating = averageRating,
                TotalReviews = totalReviews
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Amazon product URL: {Url}", url);
            return null;
        }
    }

    public async IAsyncEnumerable<ScrapedReview> ScrapeReviewsAsync(
        string productId,
        ScrapeOptions options,
        Action<ScrapeProgress>? progressCallback = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var progress = new ScrapeProgress { CurrentStatus = "Starting..." };
        progressCallback?.Invoke(progress);

        var baseUrl = "https://www.amazon.com";
        var pageNumber = 1;
        var reviewsSoFar = 0;
        var hasMorePages = true;

        while (hasMorePages && !cancellationToken.IsCancellationRequested)
        {
            if (options.MaxReviews.HasValue && reviewsSoFar >= options.MaxReviews.Value)
            {
                break;
            }

            progress.CurrentStatus = $"Fetching page {pageNumber}...";
            progressCallback?.Invoke(progress);

            var reviewsUrl = $"{baseUrl}/product-reviews/{productId}?pageNumber={pageNumber}&sortBy=recent";

            // Add delay between requests
            if (pageNumber > 1)
            {
                await Task.Delay(Random.Shared.Next(MinDelayMs, MaxDelayMs), cancellationToken);
            }

            string? html = null;
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    html = await FetchPageAsync(reviewsUrl, cancellationToken);
                    if (!string.IsNullOrEmpty(html) && html.Length > 10000) break;

                    _logger.LogWarning("Received small response, retrying... Attempt {Attempt}", retry + 1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Retry {Retry} failed for {Url}", retry + 1, reviewsUrl);
                }

                if (retry < MaxRetries - 1)
                {
                    await Task.Delay(MinDelayMs * (retry + 1), cancellationToken);
                }
            }

            if (string.IsNullOrEmpty(html) || html.Length < 10000)
            {
                progress.LastError = $"Failed to fetch page {pageNumber}";
                progressCallback?.Invoke(progress);
                break;
            }

            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            // Get total reviews count on first page
            if (pageNumber == 1)
            {
                var totalText = document.QuerySelector("[data-hook='cr-filter-info-review-rating-count']")?.TextContent
                    ?? document.QuerySelector("[data-hook='cr-filter-info-review-count']")?.TextContent;
                if (!string.IsNullOrEmpty(totalText))
                {
                    var match = TotalReviewsRegex().Match(totalText);
                    if (match.Success && int.TryParse(match.Groups[1].Value.Replace(",", "").Replace(".", ""), out var total))
                    {
                        progress.TotalReviews = total;
                        _logger.LogInformation("Found {Total} total reviews", total);
                    }
                }
            }

            var reviewElements = document.QuerySelectorAll("[data-hook='review']");
            if (!reviewElements.Any())
            {
                _logger.LogWarning("No review elements found on page {Page}", pageNumber);
                hasMorePages = false;
                break;
            }

            _logger.LogInformation("Found {Count} reviews on page {Page}", reviewElements.Length, pageNumber);

            foreach (var reviewEl in reviewElements)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (options.MaxReviews.HasValue && reviewsSoFar >= options.MaxReviews.Value) break;

                var review = ParseReviewElement(reviewEl);
                if (review != null)
                {
                    // Apply filters
                    if (options.MinRating.HasValue && review.Rating < options.MinRating.Value)
                    {
                        progress.SkippedReviews++;
                        continue;
                    }

                    if (options.IncludePhotosOnly && review.Media.Count == 0)
                    {
                        progress.SkippedReviews++;
                        continue;
                    }

                    if (options.ReviewsAfterDate.HasValue && review.ReviewDate < options.ReviewsAfterDate.Value)
                    {
                        progress.SkippedReviews++;
                        continue;
                    }

                    progress.ProcessedReviews++;
                    progress.ImportedReviews++;
                    progressCallback?.Invoke(progress);

                    reviewsSoFar++;
                    yield return review;
                }
            }

            // Check for next page
            var nextButton = document.QuerySelector(".a-pagination .a-last:not(.a-disabled)");
            hasMorePages = nextButton != null && reviewElements.Length >= ReviewsPerPage;
            pageNumber++;
        }

        progress.CurrentStatus = "Completed";
        progressCallback?.Invoke(progress);
    }

    private ScrapedReview? ParseReviewElement(IElement reviewEl)
    {
        try
        {
            var reviewId = reviewEl.GetAttribute("id") ?? Guid.NewGuid().ToString();

            // Rating - try multiple approaches
            var rating = 0;
            var ratingEl = reviewEl.QuerySelector("[data-hook='review-star-rating'], [data-hook='cmps-review-star-rating']");

            // Try class-based rating
            var ratingClass = ratingEl?.ClassName ?? "";
            var ratingMatch = StarRatingRegex().Match(ratingClass);
            if (ratingMatch.Success)
            {
                rating = int.Parse(ratingMatch.Groups[1].Value);
            }

            // Try text-based rating
            if (rating == 0)
            {
                var ratingText = ratingEl?.QuerySelector(".a-icon-alt")?.TextContent
                    ?? reviewEl.QuerySelector(".a-icon-star .a-icon-alt")?.TextContent;
                if (!string.IsNullOrEmpty(ratingText))
                {
                    var textMatch = RatingRegex().Match(ratingText);
                    if (textMatch.Success && double.TryParse(textMatch.Groups[1].Value, out var parsed))
                    {
                        rating = (int)Math.Round(parsed);
                    }
                }
            }

            if (rating == 0) return null;

            // Reviewer name
            var reviewerName = reviewEl.QuerySelector(".a-profile-name")?.TextContent?.Trim() ?? "Anonymous";

            // Title - clean up the title text
            var titleEl = reviewEl.QuerySelector("[data-hook='review-title']");
            var title = titleEl?.QuerySelector("span:not(.a-icon-alt)")?.TextContent?.Trim()
                ?? titleEl?.TextContent?.Trim();

            // Remove rating text from title if present
            if (!string.IsNullOrEmpty(title))
            {
                title = RatingPrefixRegex().Replace(title, "").Trim();
            }

            // Body
            var body = reviewEl.QuerySelector("[data-hook='review-body'] span")?.TextContent?.Trim()
                ?? reviewEl.QuerySelector("[data-hook='review-body']")?.TextContent?.Trim()
                ?? reviewEl.QuerySelector(".review-text-content span")?.TextContent?.Trim();

            // Date
            var dateText = reviewEl.QuerySelector("[data-hook='review-date']")?.TextContent?.Trim();
            var reviewDate = ParseReviewDate(dateText);

            // Verified purchase
            var verified = reviewEl.QuerySelector("[data-hook='avp-badge']") != null
                || reviewEl.QuerySelector(".avp-badge")?.TextContent?.Contains("Verified", StringComparison.OrdinalIgnoreCase) == true;

            // Media (images)
            var media = new List<ScrapedReviewMedia>();
            var imageElements = reviewEl.QuerySelectorAll("[data-hook='review-image-tile'] img, .review-image-tile-section img, .review-image img");
            foreach (var img in imageElements)
            {
                var src = img.GetAttribute("src");
                if (!string.IsNullOrEmpty(src))
                {
                    // Convert thumbnail to full-size URL
                    var fullUrl = Regex.Replace(src, @"\._[A-Z]+\d+_\.", "._SL1500_.");
                    media.Add(new ScrapedReviewMedia
                    {
                        MediaType = "image",
                        Url = fullUrl,
                        ThumbnailUrl = src
                    });
                }
            }

            return new ScrapedReview
            {
                ExternalReviewId = reviewId,
                ReviewerName = reviewerName,
                Rating = rating,
                Title = title,
                Body = body,
                ReviewDate = reviewDate,
                IsVerifiedPurchase = verified,
                Media = media
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing review element");
            return null;
        }
    }

    private static DateTime ParseReviewDate(string? dateText)
    {
        if (string.IsNullOrEmpty(dateText))
            return DateTime.UtcNow;

        // Format: "Reviewed in the United States on January 15, 2024"
        var match = DateRegex().Match(dateText);
        if (match.Success)
        {
            var dateStr = match.Groups[1].Value;
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return DateTime.UtcNow;
    }

    private static string? ExtractAsin(string url)
    {
        var asinMatch = AsinRegex().Match(url);
        return asinMatch.Success ? asinMatch.Groups[1].Value : null;
    }

    private static string GetBaseUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch
        {
            return "https://www.amazon.com";
        }
    }

    private async Task<string?> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            string requestUrl;

            if (_options.Enabled && !string.IsNullOrEmpty(_options.ApiKey))
            {
                // Use ScraperAPI
                requestUrl = BuildScraperApiUrl(url);
                _logger.LogInformation("Fetching via ScraperAPI: {Url}", url);
            }
            else
            {
                // Direct fetch (will likely be blocked)
                requestUrl = url;
                _logger.LogWarning("ScraperAPI not configured, attempting direct fetch: {Url}", url);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Successfully fetched page: {Url}, HTML length: {Length}", url, html.Length);

            // Check for CAPTCHA or error pages
            if (html.Contains("validateCaptcha") || html.Contains("api-services-support@amazon.com"))
            {
                _logger.LogWarning("CAPTCHA detected in response for {Url}", url);
                return null;
            }

            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching page: {Url}", url);
            return null;
        }
    }

    private string BuildScraperApiUrl(string targetUrl)
    {
        var encodedUrl = HttpUtility.UrlEncode(targetUrl);

        return _options.Provider.ToLowerInvariant() switch
        {
            "scraperapi" => BuildScraperApiRequest(encodedUrl),
            "zyte" => BuildZyteRequest(encodedUrl),
            "brightdata" => BuildBrightDataRequest(encodedUrl),
            _ => BuildScraperApiRequest(encodedUrl)
        };
    }

    private string BuildScraperApiRequest(string encodedUrl)
    {
        var baseUrl = "http://api.scraperapi.com";
        var queryParams = new List<string>
        {
            $"api_key={_options.ApiKey}",
            $"url={encodedUrl}",
            "keep_headers=true"
        };

        if (_options.RenderJs)
        {
            queryParams.Add("render=true");
        }

        if (!string.IsNullOrEmpty(_options.CountryCode))
        {
            queryParams.Add($"country_code={_options.CountryCode}");
        }

        if (_options.PremiumProxy)
        {
            queryParams.Add("premium=true");
        }

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    private string BuildZyteRequest(string encodedUrl)
    {
        // Zyte (formerly Scrapy Cloud) API format
        return $"https://api.zyte.com/v1/extract?apikey={_options.ApiKey}&url={encodedUrl}&render_js={_options.RenderJs.ToString().ToLower()}";
    }

    private string BuildBrightDataRequest(string encodedUrl)
    {
        // Bright Data (formerly Luminati) - uses proxy format
        // This is a simplified example - actual implementation may vary
        return $"https://api.brightdata.com/request?url={encodedUrl}&zone=web_unlocker";
    }

    [GeneratedRegex(@"([\d,]+(?:\.\d{2})?)")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*out")]
    private static partial Regex RatingRegex();

    [GeneratedRegex(@"([\d,\.]+)\s*(?:global\s*)?rating")]
    private static partial Regex ReviewCountRegex();

    [GeneratedRegex(@"([\d,\.]+)\s*(?:global\s*)?(?:review|rating)")]
    private static partial Regex TotalReviewsRegex();

    [GeneratedRegex(@"a-star-(\d)")]
    private static partial Regex StarRatingRegex();

    [GeneratedRegex(@"on\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"/(?:dp|product|gp/product|ASIN)/([A-Z0-9]{10})", RegexOptions.IgnoreCase)]
    private static partial Regex AsinRegex();

    [GeneratedRegex(@"^\d+(\.\d+)?\s*out\s*of\s*\d+\s*stars?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex RatingPrefixRegex();
}
