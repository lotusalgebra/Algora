using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Scrapers;

/// <summary>
/// Scraper for AliExpress product reviews
/// </summary>
public partial class AliExpressReviewScraper : IReviewScraper
{
    private readonly ILogger<AliExpressReviewScraper> _logger;
    private readonly HttpClient _httpClient;
    private readonly IBrowsingContext _browsingContext;

    // Rate limiting settings
    private const int MinDelayMs = 3000;
    private const int MaxDelayMs = 6000;
    private const int MaxRetries = 3;
    private const int ReviewsPerPage = 10;

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0"
    ];

    public string SourceType => "aliexpress";

    public AliExpressReviewScraper(ILogger<AliExpressReviewScraper> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ReviewScraper");
        _browsingContext = BrowsingContext.New(Configuration.Default);
    }

    public bool CanHandle(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("aliexpress", StringComparison.OrdinalIgnoreCase);
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
            var productId = ExtractProductId(url);
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogWarning("Could not extract product ID from URL: {Url}", url);
                return null;
            }

            var productUrl = $"https://www.aliexpress.com/item/{productId}.html";

            var html = await FetchPageAsync(productUrl, cancellationToken);
            if (string.IsNullOrEmpty(html))
            {
                return new ParsedProductInfo
                {
                    SourceType = SourceType,
                    ProductId = productId,
                    ProductUrl = productUrl
                };
            }

            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            // Try to extract product data from embedded JSON
            var productData = ExtractProductDataFromScript(html);

            var title = productData?.Title
                ?? document.QuerySelector("h1")?.TextContent?.Trim()
                ?? document.QuerySelector("[data-pl='product-title']")?.TextContent?.Trim()
                ?? "Unknown Product";

            var imageUrl = productData?.ImageUrl
                ?? document.QuerySelector("[class*='magnifier'] img")?.GetAttribute("src")
                ?? document.QuerySelector(".product-image img")?.GetAttribute("src");

            return new ParsedProductInfo
            {
                SourceType = SourceType,
                ProductId = productId,
                ProductTitle = title,
                ProductUrl = productUrl,
                ImageUrl = imageUrl,
                Price = productData?.Price,
                AverageRating = productData?.AverageRating,
                TotalReviews = productData?.TotalReviews
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AliExpress product URL: {Url}", url);
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

            // AliExpress uses a feedback API
            var feedbackUrl = BuildFeedbackUrl(productId, pageNumber);

            // Add delay between requests
            if (pageNumber > 1)
            {
                await Task.Delay(Random.Shared.Next(MinDelayMs, MaxDelayMs), cancellationToken);
            }

            FeedbackApiResponse? feedbackData = null;
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    feedbackData = await FetchFeedbackApiAsync(feedbackUrl, productId, cancellationToken);
                    if (feedbackData != null) break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Retry {Retry} failed for feedback API", retry + 1);
                    if (retry < MaxRetries - 1)
                    {
                        await Task.Delay(MinDelayMs * (retry + 1), cancellationToken);
                    }
                }
            }

            // Fall back to HTML scraping if API fails
            if (feedbackData == null || feedbackData.Reviews.Count == 0)
            {
                // Try HTML scraping
                var reviews = await ScrapeReviewsFromHtmlAsync(productId, pageNumber, cancellationToken);
                if (reviews.Count == 0)
                {
                    progress.LastError = $"Failed to fetch reviews for page {pageNumber}";
                    progressCallback?.Invoke(progress);
                    break;
                }

                foreach (var review in reviews)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (options.MaxReviews.HasValue && reviewsSoFar >= options.MaxReviews.Value) break;

                    if (ApplyFilters(review, options, progress))
                    {
                        reviewsSoFar++;
                        yield return review;
                    }
                }

                hasMorePages = reviews.Count >= ReviewsPerPage;
                pageNumber++;
                continue;
            }

            // Set total on first page
            if (pageNumber == 1 && feedbackData.TotalCount > 0)
            {
                progress.TotalReviews = feedbackData.TotalCount;
            }

            foreach (var review in feedbackData.Reviews)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (options.MaxReviews.HasValue && reviewsSoFar >= options.MaxReviews.Value) break;

                if (ApplyFilters(review, options, progress))
                {
                    reviewsSoFar++;
                    yield return review;
                }
            }

            hasMorePages = feedbackData.HasMore;
            pageNumber++;
        }

        progress.CurrentStatus = "Completed";
        progressCallback?.Invoke(progress);
    }

    private bool ApplyFilters(ScrapedReview review, ScrapeOptions options, ScrapeProgress progress)
    {
        if (options.MinRating.HasValue && review.Rating < options.MinRating.Value)
        {
            progress.SkippedReviews++;
            return false;
        }

        if (options.IncludePhotosOnly && review.Media.Count == 0)
        {
            progress.SkippedReviews++;
            return false;
        }

        if (options.ReviewsAfterDate.HasValue && review.ReviewDate < options.ReviewsAfterDate.Value)
        {
            progress.SkippedReviews++;
            return false;
        }

        progress.ProcessedReviews++;
        progress.ImportedReviews++;
        return true;
    }

    private async Task<List<ScrapedReview>> ScrapeReviewsFromHtmlAsync(
        string productId,
        int page,
        CancellationToken cancellationToken)
    {
        var reviews = new List<ScrapedReview>();

        try
        {
            // Construct review page URL
            var url = $"https://www.aliexpress.com/item/{productId}.html?page={page}";
            var html = await FetchPageAsync(url, cancellationToken);

            if (string.IsNullOrEmpty(html))
                return reviews;

            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            // Try to find embedded JSON review data
            var scriptContent = document.QuerySelectorAll("script")
                .Select(s => s.TextContent)
                .FirstOrDefault(s => s.Contains("\"feedbackList\"") || s.Contains("\"reviews\""));

            if (!string.IsNullOrEmpty(scriptContent))
            {
                reviews = ParseReviewsFromJson(scriptContent);
            }
            else
            {
                // Fallback: parse HTML elements
                var reviewElements = document.QuerySelectorAll("[class*='feedback-item'], [class*='review-item']");
                foreach (var el in reviewElements)
                {
                    var review = ParseReviewElement(el);
                    if (review != null)
                    {
                        reviews.Add(review);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scraping reviews from HTML for product {ProductId}", productId);
        }

        return reviews;
    }

    private ScrapedReview? ParseReviewElement(AngleSharp.Dom.IElement element)
    {
        try
        {
            // Parse star rating
            var stars = element.QuerySelectorAll("[class*='star-view'] span[class*='star-active'], .star-view .yellow").Count();
            if (stars == 0)
            {
                var ratingText = element.QuerySelector("[class*='rating']")?.TextContent;
                if (!string.IsNullOrEmpty(ratingText) && int.TryParse(ratingText.Trim(), out var r))
                {
                    stars = r;
                }
            }
            if (stars == 0) stars = 5; // Default

            var reviewerName = element.QuerySelector("[class*='user-name'], [class*='buyer']")?.TextContent?.Trim()
                ?? "Anonymous";

            var body = element.QuerySelector("[class*='content'], [class*='feedback-text']")?.TextContent?.Trim();

            var dateText = element.QuerySelector("[class*='date'], [class*='time']")?.TextContent?.Trim();
            var reviewDate = ParseDate(dateText);

            // Extract images
            var media = new List<ScrapedReviewMedia>();
            var images = element.QuerySelectorAll("[class*='pic-view'] img, [class*='photo'] img");
            foreach (var img in images)
            {
                var src = img.GetAttribute("src");
                if (!string.IsNullOrEmpty(src))
                {
                    media.Add(new ScrapedReviewMedia
                    {
                        MediaType = "image",
                        Url = NormalizeImageUrl(src),
                        ThumbnailUrl = src
                    });
                }
            }

            return new ScrapedReview
            {
                ExternalReviewId = Guid.NewGuid().ToString(),
                ReviewerName = reviewerName,
                Rating = Math.Min(5, Math.Max(1, stars)),
                Body = body,
                ReviewDate = reviewDate,
                IsVerifiedPurchase = true, // AliExpress reviews are from buyers
                Media = media
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing review element");
            return null;
        }
    }

    private List<ScrapedReview> ParseReviewsFromJson(string json)
    {
        var reviews = new List<ScrapedReview>();

        try
        {
            // Find the feedback array in the JSON
            var feedbackMatch = FeedbackJsonRegex().Match(json);
            if (!feedbackMatch.Success)
                return reviews;

            var feedbackJson = feedbackMatch.Groups[1].Value;

            using var doc = JsonDocument.Parse($"[{feedbackJson}]");
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var review = ParseReviewFromJsonElement(item);
                if (review != null)
                {
                    reviews.Add(review);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing reviews from JSON");
        }

        return reviews;
    }

    private ScrapedReview? ParseReviewFromJsonElement(JsonElement element)
    {
        try
        {
            var rating = element.TryGetProperty("buyerEval", out var evalProp) ? evalProp.GetInt32() : 5;
            if (rating == 0)
            {
                rating = element.TryGetProperty("star", out var starProp) ? starProp.GetInt32() : 5;
            }

            var reviewerName = element.TryGetProperty("buyerName", out var nameProp)
                ? nameProp.GetString() ?? "Anonymous"
                : element.TryGetProperty("anonymousUser", out var anonProp)
                    ? anonProp.GetString() ?? "Anonymous"
                    : "Anonymous";

            var body = element.TryGetProperty("buyerFeedback", out var feedbackProp)
                ? feedbackProp.GetString()
                : element.TryGetProperty("content", out var contentProp)
                    ? contentProp.GetString()
                    : null;

            var dateString = element.TryGetProperty("evalDate", out var dateProp)
                ? dateProp.GetString()
                : element.TryGetProperty("gmtCreate", out var gmtProp)
                    ? gmtProp.GetString()
                    : null;
            var reviewDate = ParseDate(dateString);

            var reviewId = element.TryGetProperty("id", out var idProp)
                ? idProp.ToString()
                : element.TryGetProperty("evaluationId", out var evalIdProp)
                    ? evalIdProp.ToString()
                    : Guid.NewGuid().ToString();

            // Parse images
            var media = new List<ScrapedReviewMedia>();
            if (element.TryGetProperty("images", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var img in imagesProp.EnumerateArray())
                {
                    var url = img.GetString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        media.Add(new ScrapedReviewMedia
                        {
                            MediaType = "image",
                            Url = NormalizeImageUrl(url),
                            ThumbnailUrl = url
                        });
                    }
                }
            }

            return new ScrapedReview
            {
                ExternalReviewId = reviewId,
                ReviewerName = reviewerName,
                Rating = Math.Min(5, Math.Max(1, rating)),
                Body = body,
                ReviewDate = reviewDate,
                IsVerifiedPurchase = true,
                Media = media
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing review from JSON element");
            return null;
        }
    }

    private async Task<FeedbackApiResponse?> FetchFeedbackApiAsync(
        string url,
        string productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", UserAgents[Random.Shared.Next(UserAgents.Length)]);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Referer", $"https://www.aliexpress.com/item/{productId}.html");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limited by AliExpress");
                return null;
            }

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseFeedbackApiResponse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching feedback API");
            return null;
        }
    }

    private FeedbackApiResponse? ParseFeedbackApiResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var response = new FeedbackApiResponse();

            // Navigate the response structure
            if (root.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.TryGetProperty("productReviewList", out var reviewListProp) &&
                    reviewListProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in reviewListProp.EnumerateArray())
                    {
                        var review = ParseReviewFromJsonElement(item);
                        if (review != null)
                        {
                            response.Reviews.Add(review);
                        }
                    }
                }

                if (dataProp.TryGetProperty("totalPage", out var totalPageProp))
                {
                    response.TotalPages = totalPageProp.GetInt32();
                }

                if (dataProp.TryGetProperty("totalNum", out var totalNumProp))
                {
                    response.TotalCount = totalNumProp.GetInt32();
                }

                if (dataProp.TryGetProperty("currentPage", out var currentPageProp))
                {
                    response.CurrentPage = currentPageProp.GetInt32();
                }
            }

            response.HasMore = response.CurrentPage < response.TotalPages;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing feedback API response");
            return null;
        }
    }

    private static string BuildFeedbackUrl(string productId, int page)
    {
        // AliExpress feedback API endpoint
        return $"https://feedback.aliexpress.com/pc/searchEvaluation.do?productId={productId}&page={page}&pageSize={ReviewsPerPage}&filter=all&sort=default";
    }

    private static string NormalizeImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        // Remove size suffixes to get larger images
        url = ImageSizeSuffixRegex().Replace(url, "$1");

        // Add https if missing
        if (url.StartsWith("//"))
            url = "https:" + url;

        return url;
    }

    private static DateTime ParseDate(string? dateText)
    {
        if (string.IsNullOrEmpty(dateText))
            return DateTime.UtcNow;

        // Try various formats
        string[] formats =
        [
            "yyyy-MM-dd",
            "dd MMM yyyy",
            "MMM dd, yyyy",
            "yyyy-MM-dd HH:mm:ss"
        ];

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        if (DateTime.TryParse(dateText, out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }

    private static string? ExtractProductId(string url)
    {
        // Pattern: /item/1234567890.html or /item/1234567890
        var match = ProductIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    private AliExpressProductData? ExtractProductDataFromScript(string html)
    {
        try
        {
            // Look for window.runParams or similar data
            var dataMatch = ProductDataRegex().Match(html);
            if (!dataMatch.Success)
                return null;

            var json = dataMatch.Groups[1].Value;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = new AliExpressProductData();

            if (root.TryGetProperty("pageModule", out var pageModule))
            {
                data.Title = pageModule.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString()
                    : null;
            }

            if (root.TryGetProperty("priceModule", out var priceModule))
            {
                if (priceModule.TryGetProperty("minAmount", out var minProp) &&
                    decimal.TryParse(minProp.GetRawText(), out var price))
                {
                    data.Price = price;
                }
            }

            if (root.TryGetProperty("titleModule", out var titleModule))
            {
                if (titleModule.TryGetProperty("feedbackRating", out var feedbackRating))
                {
                    if (feedbackRating.TryGetProperty("averageStar", out var avgProp) &&
                        double.TryParse(avgProp.GetRawText(), out var avg))
                    {
                        data.AverageRating = avg;
                    }

                    if (feedbackRating.TryGetProperty("totalValidNum", out var totalProp))
                    {
                        data.TotalReviews = totalProp.GetInt32();
                    }
                }
            }

            if (root.TryGetProperty("imageModule", out var imageModule))
            {
                if (imageModule.TryGetProperty("imagePathList", out var imagesProp) &&
                    imagesProp.ValueKind == JsonValueKind.Array &&
                    imagesProp.GetArrayLength() > 0)
                {
                    data.ImageUrl = imagesProp[0].GetString();
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting product data from script");
            return null;
        }
    }

    private async Task<string?> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", UserAgents[Random.Shared.Next(UserAgents.Length)]);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limited by AliExpress");
                await Task.Delay(10000, cancellationToken);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching page: {Url}", url);
            return null;
        }
    }

    [GeneratedRegex(@"/item/(\d+)")]
    private static partial Regex ProductIdRegex();

    [GeneratedRegex(@"""feedbackList"":\s*\[(.*?)\]", RegexOptions.Singleline)]
    private static partial Regex FeedbackJsonRegex();

    [GeneratedRegex(@"_\d+x\d+(\.\w+)$")]
    private static partial Regex ImageSizeSuffixRegex();

    [GeneratedRegex(@"window\.runParams\s*=\s*(\{.*?\});", RegexOptions.Singleline)]
    private static partial Regex ProductDataRegex();

    private class FeedbackApiResponse
    {
        public List<ScrapedReview> Reviews { get; set; } = [];
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public bool HasMore { get; set; }
    }

    private class AliExpressProductData
    {
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public double? AverageRating { get; set; }
        public int? TotalReviews { get; set; }
    }
}
