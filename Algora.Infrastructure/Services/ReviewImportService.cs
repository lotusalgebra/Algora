using System.Text.Json;
using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Algora.Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for importing reviews from external sources
/// </summary>
public class ReviewImportService : IReviewImportService
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<IReviewScraper> _scrapers;
    private readonly ILogger<ReviewImportService> _logger;

    public ReviewImportService(
        AppDbContext context,
        IEnumerable<IReviewScraper> scrapers,
        ILogger<ReviewImportService> logger)
    {
        _context = context;
        _scrapers = scrapers;
        _logger = logger;
    }

    #region Import Jobs

    public async Task<PaginatedResult<ReviewImportJobListDto>> GetImportJobsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ReviewImportJobs
            .Where(j => j.ShopDomain == shopDomain);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new ReviewImportJobListDto
            {
                Id = j.Id,
                SourceType = j.SourceType,
                SourceProductTitle = j.SourceProductTitle,
                TargetProductTitle = j.TargetProductTitle,
                Status = j.Status,
                TotalReviews = j.TotalReviews,
                ImportedReviews = j.ImportedReviews,
                CreatedAt = j.CreatedAt,
                CompletedAt = j.CompletedAt
            })
            .ToListAsync();

        return PaginatedResult<ReviewImportJobListDto>.Create(jobs, totalCount, page, pageSize);
    }

    public async Task<ReviewImportJobDto?> GetImportJobByIdAsync(int jobId)
    {
        var job = await _context.ReviewImportJobs.FindAsync(jobId);
        return job == null ? null : MapToDto(job);
    }

    public async Task<ReviewImportJobDto> CreateImportJobAsync(string shopDomain, CreateReviewImportJobDto dto)
    {
        var parsed = await ParseProductUrlAsync(dto.SourceUrl);
        if (!parsed.IsValid)
        {
            throw new InvalidOperationException(parsed.ErrorMessage ?? "Could not parse URL");
        }

        // Find target product if PlatformProductId is provided
        int? targetProductId = null;
        if (dto.TargetPlatformProductId.HasValue)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ShopDomain == shopDomain && p.PlatformProductId == dto.TargetPlatformProductId.Value);
            targetProductId = product?.Id;
        }

        var job = new ReviewImportJob
        {
            ShopDomain = shopDomain,
            SourceType = parsed.SourceType,
            SourceUrl = dto.SourceUrl,
            SourceProductId = parsed.ProductId,
            SourceProductTitle = parsed.ProductTitle,
            TargetProductId = targetProductId,
            TargetPlatformProductId = dto.TargetPlatformProductId,
            TargetProductTitle = dto.TargetProductTitle,
            MappingMethod = dto.MappingMethod,
            Status = "pending",
            MinRating = dto.MinRating,
            IncludePhotosOnly = dto.IncludePhotosOnly,
            ReviewsAfterDate = dto.ReviewsAfterDate,
            MaxReviews = dto.MaxReviews,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewImportJobs.Add(job);
        await _context.SaveChangesAsync();

        return MapToDto(job);
    }

    public async Task<bool> CancelImportJobAsync(int jobId)
    {
        var job = await _context.ReviewImportJobs.FindAsync(jobId);
        if (job == null)
            return false;

        if (job.Status == "pending" || job.Status == "processing")
        {
            job.Status = "cancelled";
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> RetryImportJobAsync(int jobId)
    {
        var job = await _context.ReviewImportJobs.FindAsync(jobId);
        if (job == null)
            return false;

        if (job.Status == "failed" || job.Status == "cancelled")
        {
            job.Status = "pending";
            job.ErrorMessage = null;
            job.ImportedReviews = 0;
            job.SkippedReviews = 0;
            job.FailedReviews = 0;
            job.StartedAt = null;
            job.CompletedAt = null;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteImportJobAsync(int jobId, bool deleteReviews = false)
    {
        var job = await _context.ReviewImportJobs.FindAsync(jobId);
        if (job == null)
            return false;

        if (job.Status == "processing")
            return false;

        if (deleteReviews)
        {
            var reviewsToDelete = await _context.Reviews
                .Where(r => r.ImportJobId == jobId)
                .ToListAsync();
            _context.Reviews.RemoveRange(reviewsToDelete);
        }

        _context.ReviewImportJobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region URL Parsing

    public async Task<ParsedReviewUrlDto> ParseProductUrlAsync(string url)
    {
        var scraper = _scrapers.FirstOrDefault(s => s.CanHandle(url));
        if (scraper == null)
        {
            return new ParsedReviewUrlDto
            {
                IsValid = false,
                ErrorMessage = "Unsupported URL. Only Amazon and AliExpress URLs are supported."
            };
        }

        try
        {
            var result = await scraper.ParseProductUrlAsync(url);
            if (result == null)
            {
                return new ParsedReviewUrlDto
                {
                    IsValid = false,
                    SourceType = scraper.SourceType,
                    ErrorMessage = "Could not parse product information from URL"
                };
            }

            return new ParsedReviewUrlDto
            {
                IsValid = true,
                SourceType = result.SourceType,
                ProductId = result.ProductId,
                ProductTitle = result.ProductTitle,
                ProductImageUrl = result.ImageUrl,
                TotalReviewCount = result.TotalReviews,
                AverageRating = result.AverageRating.HasValue ? (decimal)result.AverageRating.Value : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing URL: {Url}", url);
            return new ParsedReviewUrlDto
            {
                IsValid = false,
                SourceType = scraper.SourceType,
                ErrorMessage = "Error parsing URL: " + ex.Message
            };
        }
    }

    public bool IsSupportedUrl(string url)
    {
        return _scrapers.Any(s => s.CanHandle(url));
    }

    public string? GetSourceType(string url)
    {
        var scraper = _scrapers.FirstOrDefault(s => s.CanHandle(url));
        return scraper?.SourceType;
    }

    #endregion

    #region Import Processing

    public async Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var pendingJobs = await _context.ReviewImportJobs
            .Where(j => j.Status == "pending")
            .OrderBy(j => j.CreatedAt)
            .Take(2)
            .ToListAsync(cancellationToken);

        foreach (var job in pendingJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ProcessImportJobAsync(job.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing import job {JobId}", job.Id);
            }
        }
    }

    public async Task ProcessImportJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await _context.ReviewImportJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Import job not found: {jobId}");
        }

        if (job.Status != "pending")
        {
            return; // Already processed or cancelled
        }

        job.Status = "processing";
        job.StartedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var scraper = _scrapers.FirstOrDefault(s => s.SourceType == job.SourceType);
            if (scraper == null)
            {
                throw new InvalidOperationException($"No scraper found for source type: {job.SourceType}");
            }

            var options = new ScrapeOptions
            {
                MinRating = job.MinRating,
                IncludePhotosOnly = job.IncludePhotosOnly,
                ReviewsAfterDate = job.ReviewsAfterDate,
                MaxReviews = job.MaxReviews
            };

            var progressLog = new List<string>();

            await foreach (var scrapedReview in scraper.ScrapeReviewsAsync(
                job.SourceProductId!,
                options,
                progress => UpdateJobProgress(job, progress, progressLog),
                cancellationToken))
            {
                try
                {
                    await ImportReviewAsync(job, scrapedReview, cancellationToken);
                    job.ImportedReviews++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to import review {ReviewId}", scrapedReview.ExternalReviewId);
                    job.FailedReviews++;
                    progressLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] Failed: {ex.Message}");
                }

                job.ProgressLog = JsonSerializer.Serialize(progressLog.TakeLast(100));
                job.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            progressLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] Completed. Imported: {job.ImportedReviews}, Skipped: {job.SkippedReviews}, Failed: {job.FailedReviews}");
            job.ProgressLog = JsonSerializer.Serialize(progressLog.TakeLast(100));
        }
        catch (OperationCanceledException)
        {
            job.Status = "cancelled";
            job.ErrorMessage = "Import was cancelled";
            _logger.LogInformation("Import job {JobId} was cancelled", jobId);
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Import job {JobId} failed", jobId);
        }
        finally
        {
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<ImportProgressDto> GetImportProgressAsync(int jobId)
    {
        var job = await _context.ReviewImportJobs.FindAsync(jobId);
        if (job == null)
        {
            return new ImportProgressDto
            {
                JobId = jobId,
                Status = "not_found",
                ErrorMessage = "Job not found"
            };
        }

        var progressPercent = job.TotalReviews > 0
            ? (int)((job.ImportedReviews + job.SkippedReviews + job.FailedReviews) * 100.0 / job.TotalReviews)
            : 0;

        var log = !string.IsNullOrEmpty(job.ProgressLog)
            ? JsonSerializer.Deserialize<List<string>>(job.ProgressLog) ?? []
            : [];

        return new ImportProgressDto
        {
            JobId = job.Id,
            Status = job.Status,
            TotalReviews = job.TotalReviews,
            ImportedReviews = job.ImportedReviews,
            SkippedReviews = job.SkippedReviews,
            FailedReviews = job.FailedReviews,
            ProgressPercent = progressPercent,
            ErrorMessage = job.ErrorMessage,
            Log = log
        };
    }

    #endregion

    #region Private Helpers

    private async Task ImportReviewAsync(
        ReviewImportJob job,
        ScrapedReview scrapedReview,
        CancellationToken cancellationToken)
    {
        // Check for duplicate
        var exists = await _context.Reviews
            .AnyAsync(r => r.ShopDomain == job.ShopDomain &&
                r.ExternalReviewId == scrapedReview.ExternalReviewId &&
                r.Source == job.SourceType,
                cancellationToken);

        if (exists)
        {
            job.SkippedReviews++;
            return;
        }

        // Get settings for auto-approval
        var settings = await _context.ReviewSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == job.ShopDomain, cancellationToken);

        var autoApprove = settings?.AutoApproveReviews == true &&
            (!settings.AutoApproveMinRating.HasValue || scrapedReview.Rating >= settings.AutoApproveMinRating.Value);

        var review = new Review
        {
            ShopDomain = job.ShopDomain,
            ProductId = job.TargetProductId,
            PlatformProductId = job.TargetPlatformProductId,
            ProductTitle = job.TargetProductTitle ?? job.SourceProductTitle,
            ReviewerName = scrapedReview.ReviewerName,
            Rating = scrapedReview.Rating,
            Title = scrapedReview.Title ?? string.Empty,
            Body = scrapedReview.Body ?? string.Empty,
            IsVerifiedPurchase = scrapedReview.IsVerifiedPurchase,
            Source = job.SourceType,
            SourceUrl = job.SourceUrl,
            ExternalReviewId = scrapedReview.ExternalReviewId,
            ImportJobId = job.Id,
            Status = autoApprove ? "approved" : "pending",
            ApprovedAt = autoApprove ? DateTime.UtcNow : null,
            ReviewDate = scrapedReview.ReviewDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        // Import media
        if (scrapedReview.Media.Count > 0 && (settings?.ImportPhotos != false))
        {
            var order = 0;
            foreach (var media in scrapedReview.Media)
            {
                var reviewMedia = new ReviewMedia
                {
                    ReviewId = review.Id,
                    MediaType = media.MediaType,
                    Url = media.Url,
                    ThumbnailUrl = media.ThumbnailUrl,
                    DisplayOrder = order++,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ReviewMedia.Add(reviewMedia);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private void UpdateJobProgress(ReviewImportJob job, ScrapeProgress progress, List<string> progressLog)
    {
        job.TotalReviews = progress.TotalReviews;
        job.SkippedReviews = progress.SkippedReviews;

        if (!string.IsNullOrEmpty(progress.CurrentStatus))
        {
            progressLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] {progress.CurrentStatus}");
        }

        if (!string.IsNullOrEmpty(progress.LastError))
        {
            progressLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] Error: {progress.LastError}");
        }
    }

    private static ReviewImportJobDto MapToDto(ReviewImportJob job)
    {
        return new ReviewImportJobDto
        {
            Id = job.Id,
            ShopDomain = job.ShopDomain,
            SourceType = job.SourceType,
            SourceUrl = job.SourceUrl,
            SourceProductId = job.SourceProductId,
            SourceProductTitle = job.SourceProductTitle,
            TargetProductId = job.TargetProductId,
            TargetPlatformProductId = job.TargetPlatformProductId,
            TargetProductTitle = job.TargetProductTitle,
            MappingMethod = job.MappingMethod,
            Status = job.Status,
            TotalReviews = job.TotalReviews,
            ImportedReviews = job.ImportedReviews,
            SkippedReviews = job.SkippedReviews,
            FailedReviews = job.FailedReviews,
            ErrorMessage = job.ErrorMessage,
            MinRating = job.MinRating,
            IncludePhotosOnly = job.IncludePhotosOnly,
            ReviewsAfterDate = job.ReviewsAfterDate,
            MaxReviews = job.MaxReviews,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            CreatedAt = job.CreatedAt
        };
    }

    #endregion
}
