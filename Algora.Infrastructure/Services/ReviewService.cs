using System.Security.Cryptography;
using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing product reviews.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(AppDbContext db, ILogger<ReviewService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Review CRUD

    public async Task<PaginatedResult<ReviewListDto>> GetReviewsAsync(string shopDomain, ReviewFilterDto filter)
    {
        var query = _db.Reviews
            .Where(r => r.ShopDomain == shopDomain);

        // Apply filters
        if (filter.ProductId.HasValue)
            query = query.Where(r => r.PlatformProductId == filter.ProductId.Value);

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(r => r.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.Source))
            query = query.Where(r => r.Source == filter.Source);

        if (filter.MinRating.HasValue)
            query = query.Where(r => r.Rating >= filter.MinRating.Value);

        if (filter.MaxRating.HasValue)
            query = query.Where(r => r.Rating <= filter.MaxRating.Value);

        if (filter.HasMedia.HasValue)
            query = filter.HasMedia.Value
                ? query.Where(r => r.Media.Any())
                : query.Where(r => !r.Media.Any());

        if (filter.IsFeatured.HasValue)
            query = query.Where(r => r.IsFeatured == filter.IsFeatured.Value);

        if (filter.IsVerifiedPurchase.HasValue)
            query = query.Where(r => r.IsVerifiedPurchase == filter.IsVerifiedPurchase.Value);

        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(r => r.Title.Contains(filter.Search) ||
                                     r.Body.Contains(filter.Search) ||
                                     r.ReviewerName.Contains(filter.Search));

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.ReviewDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.ReviewDate <= filter.ToDate.Value);

        // Sort
        query = filter.SortBy?.ToLower() switch
        {
            "rating" => filter.SortDescending
                ? query.OrderByDescending(r => r.Rating)
                : query.OrderBy(r => r.Rating),
            "helpful" => filter.SortDescending
                ? query.OrderByDescending(r => r.HelpfulVotes)
                : query.OrderBy(r => r.HelpfulVotes),
            _ => filter.SortDescending
                ? query.OrderByDescending(r => r.ReviewDate)
                : query.OrderBy(r => r.ReviewDate)
        };

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(r => r.Media)
            .Select(r => new ReviewListDto
            {
                Id = r.Id,
                ProductTitle = r.ProductTitle,
                ReviewerName = r.ReviewerName,
                Rating = r.Rating,
                Title = r.Title,
                BodyPreview = r.Body.Length > 150 ? r.Body.Substring(0, 150) + "..." : r.Body,
                Source = r.Source,
                Status = r.Status,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                IsFeatured = r.IsFeatured,
                HasMedia = r.Media.Any(),
                MediaCount = r.Media.Count,
                ReviewDate = r.ReviewDate,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<ReviewListDto>
        {
            Items = reviews,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReviewDto?> GetReviewByIdAsync(int reviewId)
    {
        var review = await _db.Reviews
            .Include(r => r.Media.OrderBy(m => m.DisplayOrder))
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        return review != null ? MapToDto(review) : null;
    }

    public async Task<ReviewDto> CreateReviewAsync(string shopDomain, CreateReviewDto dto)
    {
        var review = new Review
        {
            ShopDomain = shopDomain,
            PlatformProductId = dto.PlatformProductId,
            ProductTitle = dto.ProductTitle,
            ProductSku = dto.ProductSku,
            ReviewerName = dto.ReviewerName,
            ReviewerEmail = dto.ReviewerEmail,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Title = dto.Title,
            Body = dto.Body,
            IsVerifiedPurchase = dto.IsVerifiedPurchase,
            Source = "manual",
            Status = "pending",
            ReviewDate = dto.ReviewDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Link to product if exists
        if (dto.PlatformProductId.HasValue)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.ShopDomain == shopDomain && p.PlatformProductId == dto.PlatformProductId);
            if (product != null)
            {
                review.ProductId = product.Id;
                review.ProductTitle ??= product.Title;
            }
        }

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        // Add media
        if (dto.Media.Any())
        {
            var order = 0;
            foreach (var mediaDto in dto.Media)
            {
                var media = new ReviewMedia
                {
                    ReviewId = review.Id,
                    MediaType = mediaDto.MediaType,
                    Url = mediaDto.Url,
                    ThumbnailUrl = mediaDto.ThumbnailUrl,
                    AltText = mediaDto.AltText,
                    DisplayOrder = mediaDto.DisplayOrder > 0 ? mediaDto.DisplayOrder : order++,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ReviewMedia.Add(media);
            }
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Created review {ReviewId} for shop {ShopDomain}", review.Id, shopDomain);

        return (await GetReviewByIdAsync(review.Id))!;
    }

    public async Task<ReviewDto> SubmitCustomerReviewAsync(string shopDomain, SubmitReviewDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        var review = new Review
        {
            ShopDomain = shopDomain,
            PlatformProductId = dto.PlatformProductId,
            ReviewerName = dto.ReviewerName,
            ReviewerEmail = dto.ReviewerEmail,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Title = dto.Title ?? string.Empty,
            Body = dto.Body,
            Source = string.IsNullOrEmpty(dto.TrackingToken) ? "manual" : "email_request",
            Status = "pending",
            ReviewDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Link to product
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ShopDomain == shopDomain && p.PlatformProductId == dto.PlatformProductId);
        if (product != null)
        {
            review.ProductId = product.Id;
            review.ProductTitle = product.Title;
        }

        // Check if verified purchase via tracking token
        if (!string.IsNullOrEmpty(dto.TrackingToken))
        {
            var emailLog = await _db.ReviewEmailLogs
                .Include(l => l.Order)
                .FirstOrDefaultAsync(l => l.TrackingToken == dto.TrackingToken);
            if (emailLog != null)
            {
                review.IsVerifiedPurchase = true;
                review.OrderId = emailLog.OrderId;
                review.CustomerId = emailLog.CustomerId;

                // Update email log
                emailLog.Status = "review_submitted";
                emailLog.ReviewSubmittedAt = DateTime.UtcNow;
                emailLog.ReviewId = review.Id;
            }
        }

        // Auto-approve check
        if (settings.AutoApproveReviews)
        {
            var shouldAutoApprove = review.Rating >= (settings.AutoApproveMinRating ?? 4);
            if (settings.AutoApproveVerifiedOnly && !review.IsVerifiedPurchase)
                shouldAutoApprove = false;

            if (shouldAutoApprove)
            {
                review.Status = "approved";
                review.ApprovedAt = DateTime.UtcNow;
            }
        }

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        // Add media URLs
        if (dto.MediaUrls?.Any() == true)
        {
            var order = 0;
            foreach (var url in dto.MediaUrls)
            {
                var media = new ReviewMedia
                {
                    ReviewId = review.Id,
                    MediaType = url.Contains("video") ? "video" : "image",
                    Url = url,
                    DisplayOrder = order++,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ReviewMedia.Add(media);
            }
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Customer submitted review {ReviewId} for product {ProductId}", review.Id, dto.PlatformProductId);

        return (await GetReviewByIdAsync(review.Id))!;
    }

    public async Task<ReviewDto?> UpdateReviewAsync(int reviewId, UpdateReviewDto dto)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return null;

        if (dto.ReviewerName != null) review.ReviewerName = dto.ReviewerName;
        if (dto.Rating.HasValue) review.Rating = Math.Clamp(dto.Rating.Value, 1, 5);
        if (dto.Title != null) review.Title = dto.Title;
        if (dto.Body != null) review.Body = dto.Body;
        if (dto.IsVerifiedPurchase.HasValue) review.IsVerifiedPurchase = dto.IsVerifiedPurchase.Value;
        if (dto.IsFeatured.HasValue) review.IsFeatured = dto.IsFeatured.Value;
        if (dto.ModerationNote != null) review.ModerationNote = dto.ModerationNote;

        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetReviewByIdAsync(reviewId);
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted review {ReviewId}", reviewId);
        return true;
    }

    public async Task<int> DeleteReviewsAsync(List<int> reviewIds)
    {
        var reviews = await _db.Reviews
            .Where(r => reviewIds.Contains(r.Id))
            .ToListAsync();

        _db.Reviews.RemoveRange(reviews);
        await _db.SaveChangesAsync();

        return reviews.Count;
    }

    #endregion

    #region Moderation

    public async Task<bool> ApproveReviewAsync(int reviewId, string? note = null)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        review.Status = "approved";
        review.ApprovedAt = DateTime.UtcNow;
        if (note != null) review.ModerationNote = note;
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Update automation stats if from email
        if (review.Source == "email_request")
        {
            var emailLog = await _db.ReviewEmailLogs
                .Include(l => l.Automation)
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId);

            if (emailLog?.Automation != null)
            {
                emailLog.Automation.TotalReviewsCollected++;
                await _db.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Approved review {ReviewId}", reviewId);
        return true;
    }

    public async Task<bool> RejectReviewAsync(int reviewId, string? note = null)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        review.Status = "rejected";
        if (note != null) review.ModerationNote = note;
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Rejected review {ReviewId}", reviewId);
        return true;
    }

    public async Task<int> BulkApproveReviewsAsync(List<int> reviewIds)
    {
        var reviews = await _db.Reviews
            .Where(r => reviewIds.Contains(r.Id) && r.Status == "pending")
            .ToListAsync();

        foreach (var review in reviews)
        {
            review.Status = "approved";
            review.ApprovedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return reviews.Count;
    }

    public async Task<int> BulkRejectReviewsAsync(List<int> reviewIds)
    {
        var reviews = await _db.Reviews
            .Where(r => reviewIds.Contains(r.Id) && r.Status == "pending")
            .ToListAsync();

        foreach (var review in reviews)
        {
            review.Status = "rejected";
            review.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return reviews.Count;
    }

    public async Task<bool> ToggleFeaturedAsync(int reviewId)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        review.IsFeatured = !review.IsFeatured;
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Media

    public async Task<ReviewMediaDto?> AddReviewMediaAsync(int reviewId, CreateReviewMediaDto dto)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return null;

        var maxOrder = await _db.ReviewMedia
            .Where(m => m.ReviewId == reviewId)
            .MaxAsync(m => (int?)m.DisplayOrder) ?? -1;

        var media = new ReviewMedia
        {
            ReviewId = reviewId,
            MediaType = dto.MediaType,
            Url = dto.Url,
            ThumbnailUrl = dto.ThumbnailUrl,
            AltText = dto.AltText,
            DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReviewMedia.Add(media);
        await _db.SaveChangesAsync();

        return new ReviewMediaDto
        {
            Id = media.Id,
            ReviewId = media.ReviewId,
            MediaType = media.MediaType,
            Url = media.Url,
            ThumbnailUrl = media.ThumbnailUrl,
            AltText = media.AltText,
            DisplayOrder = media.DisplayOrder
        };
    }

    public async Task<bool> RemoveReviewMediaAsync(int mediaId)
    {
        var media = await _db.ReviewMedia.FindAsync(mediaId);
        if (media == null) return false;

        _db.ReviewMedia.Remove(media);
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Widget/Public API

    public async Task<PaginatedResult<WidgetReviewDto>> GetWidgetReviewsAsync(
        string apiKey,
        long productId,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null)
    {
        var settings = await _db.ReviewSettings.FirstOrDefaultAsync(s => s.WidgetApiKey == apiKey);
        if (settings == null)
            return new PaginatedResult<WidgetReviewDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };

        var query = _db.Reviews
            .Where(r => r.ShopDomain == settings.ShopDomain &&
                       r.PlatformProductId == productId &&
                       r.Status == "approved");

        query = sortBy?.ToLower() switch
        {
            "rating" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.ReviewDate),
            "helpful" => query.OrderByDescending(r => r.HelpfulVotes).ThenByDescending(r => r.ReviewDate),
            "oldest" => query.OrderBy(r => r.ReviewDate),
            _ => query.OrderByDescending(r => r.ReviewDate)
        };

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Media)
            .Select(r => new WidgetReviewDto
            {
                Id = r.Id,
                ReviewerName = settings.ShowReviewerName ? r.ReviewerName : "Customer",
                Rating = r.Rating,
                Title = r.Title,
                Body = r.Body,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                ReviewDate = r.ReviewDate,
                HelpfulVotes = r.HelpfulVotes,
                Media = r.Media.Select(m => new WidgetReviewMediaDto
                {
                    MediaType = m.MediaType,
                    Url = m.Url,
                    ThumbnailUrl = m.ThumbnailUrl
                }).ToList()
            })
            .ToListAsync();

        return new PaginatedResult<WidgetReviewDto>
        {
            Items = reviews,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductReviewSummaryDto?> GetProductReviewSummaryAsync(string apiKey, long productId)
    {
        var settings = await _db.ReviewSettings.FirstOrDefaultAsync(s => s.WidgetApiKey == apiKey);
        if (settings == null) return null;

        var reviews = await _db.Reviews
            .Where(r => r.ShopDomain == settings.ShopDomain &&
                       r.PlatformProductId == productId &&
                       r.Status == "approved")
            .ToListAsync();

        if (!reviews.Any())
        {
            return new ProductReviewSummaryDto
            {
                ProductId = productId,
                AverageRating = 0,
                TotalReviews = 0
            };
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ShopDomain == settings.ShopDomain && p.PlatformProductId == productId);

        return new ProductReviewSummaryDto
        {
            ProductId = productId,
            ProductTitle = product?.Title ?? string.Empty,
            AverageRating = Math.Round((decimal)reviews.Average(r => r.Rating), 1),
            TotalReviews = reviews.Count,
            FiveStarCount = reviews.Count(r => r.Rating == 5),
            FourStarCount = reviews.Count(r => r.Rating == 4),
            ThreeStarCount = reviews.Count(r => r.Rating == 3),
            TwoStarCount = reviews.Count(r => r.Rating == 2),
            OneStarCount = reviews.Count(r => r.Rating == 1),
            PhotoReviewCount = reviews.Count(r => r.Media.Any()),
            VerifiedPurchaseCount = reviews.Count(r => r.IsVerifiedPurchase)
        };
    }

    public async Task<WidgetConfigDto?> GetWidgetConfigAsync(string apiKey)
    {
        var settings = await _db.ReviewSettings.FirstOrDefaultAsync(s => s.WidgetApiKey == apiKey);
        if (settings == null) return null;

        return new WidgetConfigDto
        {
            Theme = settings.WidgetTheme,
            PrimaryColor = settings.PrimaryColor,
            AccentColor = settings.AccentColor,
            StarColor = settings.StarColor,
            Layout = settings.WidgetLayout,
            ReviewsPerPage = settings.ReviewsPerPage,
            ShowReviewerName = settings.ShowReviewerName,
            ShowReviewDate = settings.ShowReviewDate,
            ShowVerifiedBadge = settings.ShowVerifiedBadge,
            ShowPhotoGallery = settings.ShowPhotoGallery,
            AllowSubmission = settings.AllowCustomerReviews
        };
    }

    public async Task<bool> RecordHelpfulVoteAsync(int reviewId, bool isHelpful)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        if (isHelpful)
            review.HelpfulVotes++;
        else
            review.UnhelpfulVotes++;

        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Settings

    public async Task<ReviewSettingsDto> GetSettingsAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        return MapSettingsToDto(settings);
    }

    public async Task<ReviewSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateReviewSettingsDto dto)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);

        if (dto.WidgetTheme != null) settings.WidgetTheme = dto.WidgetTheme;
        if (dto.PrimaryColor != null) settings.PrimaryColor = dto.PrimaryColor;
        if (dto.AccentColor != null) settings.AccentColor = dto.AccentColor;
        if (dto.StarColor != null) settings.StarColor = dto.StarColor;
        if (dto.WidgetLayout != null) settings.WidgetLayout = dto.WidgetLayout;
        if (dto.ReviewsPerPage.HasValue) settings.ReviewsPerPage = dto.ReviewsPerPage.Value;
        if (dto.ShowReviewerName.HasValue) settings.ShowReviewerName = dto.ShowReviewerName.Value;
        if (dto.ShowReviewDate.HasValue) settings.ShowReviewDate = dto.ShowReviewDate.Value;
        if (dto.ShowVerifiedBadge.HasValue) settings.ShowVerifiedBadge = dto.ShowVerifiedBadge.Value;
        if (dto.ShowPhotoGallery.HasValue) settings.ShowPhotoGallery = dto.ShowPhotoGallery.Value;
        if (dto.AllowCustomerReviews.HasValue) settings.AllowCustomerReviews = dto.AllowCustomerReviews.Value;
        if (dto.RequireApproval.HasValue) settings.RequireApproval = dto.RequireApproval.Value;
        if (dto.AutoApproveReviews.HasValue) settings.AutoApproveReviews = dto.AutoApproveReviews.Value;
        if (dto.AutoApproveMinRating.HasValue) settings.AutoApproveMinRating = dto.AutoApproveMinRating.Value;
        if (dto.AutoApproveVerifiedOnly.HasValue) settings.AutoApproveVerifiedOnly = dto.AutoApproveVerifiedOnly.Value;
        if (dto.TranslateImportedReviews.HasValue) settings.TranslateImportedReviews = dto.TranslateImportedReviews.Value;
        if (dto.TranslateToLanguage != null) settings.TranslateToLanguage = dto.TranslateToLanguage;
        if (dto.RemoveSourceBranding.HasValue) settings.RemoveSourceBranding = dto.RemoveSourceBranding.Value;
        if (dto.ImportPhotos.HasValue) settings.ImportPhotos = dto.ImportPhotos.Value;
        if (dto.DefaultEmailFromName != null) settings.DefaultEmailFromName = dto.DefaultEmailFromName;
        if (dto.DefaultEmailFromAddress != null) settings.DefaultEmailFromAddress = dto.DefaultEmailFromAddress;

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapSettingsToDto(settings);
    }

    public async Task<string> RegenerateApiKeyAsync(string shopDomain)
    {
        var settings = await GetOrCreateSettingsAsync(shopDomain);
        settings.WidgetApiKey = GenerateApiKey();
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return settings.WidgetApiKey;
    }

    #endregion

    #region Analytics

    public async Task<ReviewAnalyticsSummaryDto> GetAnalyticsSummaryAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var allReviews = await _db.Reviews
            .Where(r => r.ShopDomain == shopDomain)
            .Include(r => r.Media)
            .ToListAsync();

        var periodReviews = allReviews.Where(r => r.CreatedAt >= from && r.CreatedAt <= to).ToList();

        var lastMonthStart = DateTime.UtcNow.AddMonths(-1).Date;
        var lastMonthEnd = DateTime.UtcNow.Date;
        var previousMonthStart = lastMonthStart.AddMonths(-1);
        var previousMonthEnd = lastMonthStart.AddDays(-1);

        var thisMonthCount = allReviews.Count(r => r.CreatedAt >= lastMonthStart && r.CreatedAt <= lastMonthEnd);
        var lastMonthCount = allReviews.Count(r => r.CreatedAt >= previousMonthStart && r.CreatedAt <= previousMonthEnd);

        var ratingDistribution = Enumerable.Range(1, 5)
            .Select(rating => new RatingDistributionDto
            {
                Rating = rating,
                Count = allReviews.Count(r => r.Rating == rating),
                Percentage = allReviews.Count > 0
                    ? Math.Round((decimal)allReviews.Count(r => r.Rating == rating) / allReviews.Count * 100, 1)
                    : 0
            })
            .OrderByDescending(r => r.Rating)
            .ToList();

        var sources = allReviews.GroupBy(r => r.Source)
            .Select(g => new ReviewSourceDistributionDto
            {
                Source = g.Key,
                Count = g.Count(),
                Percentage = allReviews.Count > 0
                    ? Math.Round((decimal)g.Count() / allReviews.Count * 100, 1)
                    : 0
            })
            .ToList();

        var dailyCounts = periodReviews
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new DailyReviewCountDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new ReviewAnalyticsSummaryDto
        {
            TotalReviews = allReviews.Count,
            PendingReviews = allReviews.Count(r => r.Status == "pending"),
            ApprovedReviews = allReviews.Count(r => r.Status == "approved"),
            RejectedReviews = allReviews.Count(r => r.Status == "rejected"),
            AverageRating = allReviews.Count > 0 ? Math.Round((decimal)allReviews.Average(r => r.Rating), 1) : 0,
            PhotoReviews = allReviews.Count(r => r.Media.Any()),
            VerifiedPurchaseReviews = allReviews.Count(r => r.IsVerifiedPurchase),
            ImportedReviews = allReviews.Count(r => r.Source == "amazon" || r.Source == "aliexpress"),
            EmailCollectedReviews = allReviews.Count(r => r.Source == "email_request"),
            ManualReviews = allReviews.Count(r => r.Source == "manual"),
            ReviewsThisMonth = thisMonthCount,
            ReviewsLastMonth = lastMonthCount,
            MonthOverMonthGrowth = lastMonthCount > 0
                ? Math.Round((decimal)(thisMonthCount - lastMonthCount) / lastMonthCount * 100, 1)
                : 0,
            RatingDistribution = ratingDistribution,
            SourceDistribution = sources,
            DailyReviewCounts = dailyCounts
        };
    }

    public async Task<int> GetPendingReviewCountAsync(string shopDomain)
    {
        return await _db.Reviews
            .CountAsync(r => r.ShopDomain == shopDomain && r.Status == "pending");
    }

    #endregion

    #region Private Helpers

    private async Task<ReviewSettings> GetOrCreateSettingsAsync(string shopDomain)
    {
        var settings = await _db.ReviewSettings.FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);
        if (settings == null)
        {
            settings = new ReviewSettings
            {
                ShopDomain = shopDomain,
                WidgetApiKey = GenerateApiKey(),
                CreatedAt = DateTime.UtcNow
            };
            _db.ReviewSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return settings;
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
    }

    private static ReviewDto MapToDto(Review review) => new()
    {
        Id = review.Id,
        ShopDomain = review.ShopDomain,
        ProductId = review.ProductId,
        PlatformProductId = review.PlatformProductId,
        ProductTitle = review.ProductTitle,
        ProductSku = review.ProductSku,
        ReviewerName = review.ReviewerName,
        ReviewerEmail = review.ReviewerEmail,
        Rating = review.Rating,
        Title = review.Title,
        Body = review.Body,
        IsVerifiedPurchase = review.IsVerifiedPurchase,
        Source = review.Source,
        SourceUrl = review.SourceUrl,
        Status = review.Status,
        IsFeatured = review.IsFeatured,
        ModerationNote = review.ModerationNote,
        HelpfulVotes = review.HelpfulVotes,
        UnhelpfulVotes = review.UnhelpfulVotes,
        ReviewDate = review.ReviewDate,
        CreatedAt = review.CreatedAt,
        ApprovedAt = review.ApprovedAt,
        Media = review.Media.Select(m => new ReviewMediaDto
        {
            Id = m.Id,
            ReviewId = m.ReviewId,
            MediaType = m.MediaType,
            Url = m.Url,
            ThumbnailUrl = m.ThumbnailUrl,
            AltText = m.AltText,
            DisplayOrder = m.DisplayOrder
        }).ToList()
    };

    private static ReviewSettingsDto MapSettingsToDto(ReviewSettings settings) => new()
    {
        Id = settings.Id,
        ShopDomain = settings.ShopDomain,
        WidgetTheme = settings.WidgetTheme,
        PrimaryColor = settings.PrimaryColor,
        AccentColor = settings.AccentColor,
        StarColor = settings.StarColor,
        WidgetLayout = settings.WidgetLayout,
        ReviewsPerPage = settings.ReviewsPerPage,
        ShowReviewerName = settings.ShowReviewerName,
        ShowReviewDate = settings.ShowReviewDate,
        ShowVerifiedBadge = settings.ShowVerifiedBadge,
        ShowPhotoGallery = settings.ShowPhotoGallery,
        AllowCustomerReviews = settings.AllowCustomerReviews,
        RequireApproval = settings.RequireApproval,
        AutoApproveReviews = settings.AutoApproveReviews,
        AutoApproveMinRating = settings.AutoApproveMinRating,
        AutoApproveVerifiedOnly = settings.AutoApproveVerifiedOnly,
        TranslateImportedReviews = settings.TranslateImportedReviews,
        TranslateToLanguage = settings.TranslateToLanguage,
        RemoveSourceBranding = settings.RemoveSourceBranding,
        ImportPhotos = settings.ImportPhotos,
        WidgetApiKey = settings.WidgetApiKey,
        DefaultEmailFromName = settings.DefaultEmailFromName,
        DefaultEmailFromAddress = settings.DefaultEmailFromAddress,
        CreatedAt = settings.CreatedAt,
        UpdatedAt = settings.UpdatedAt
    };

    #endregion
}
