using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing product reviews.
/// </summary>
public interface IReviewService
{
    #region Review CRUD

    /// <summary>
    /// Gets all reviews for a shop with filtering and pagination.
    /// </summary>
    Task<PaginatedResult<ReviewListDto>> GetReviewsAsync(string shopDomain, ReviewFilterDto filter);

    /// <summary>
    /// Gets a review by ID.
    /// </summary>
    Task<ReviewDto?> GetReviewByIdAsync(int reviewId);

    /// <summary>
    /// Creates a new review (manual entry).
    /// </summary>
    Task<ReviewDto> CreateReviewAsync(string shopDomain, CreateReviewDto dto);

    /// <summary>
    /// Submits a customer review (from widget).
    /// </summary>
    Task<ReviewDto> SubmitCustomerReviewAsync(string shopDomain, SubmitReviewDto dto);

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    Task<ReviewDto?> UpdateReviewAsync(int reviewId, UpdateReviewDto dto);

    /// <summary>
    /// Deletes a review.
    /// </summary>
    Task<bool> DeleteReviewAsync(int reviewId);

    /// <summary>
    /// Deletes multiple reviews.
    /// </summary>
    Task<int> DeleteReviewsAsync(List<int> reviewIds);

    #endregion

    #region Moderation

    /// <summary>
    /// Approves a review.
    /// </summary>
    Task<bool> ApproveReviewAsync(int reviewId, string? note = null);

    /// <summary>
    /// Rejects a review.
    /// </summary>
    Task<bool> RejectReviewAsync(int reviewId, string? note = null);

    /// <summary>
    /// Bulk approve reviews.
    /// </summary>
    Task<int> BulkApproveReviewsAsync(List<int> reviewIds);

    /// <summary>
    /// Bulk reject reviews.
    /// </summary>
    Task<int> BulkRejectReviewsAsync(List<int> reviewIds);

    /// <summary>
    /// Toggles featured status.
    /// </summary>
    Task<bool> ToggleFeaturedAsync(int reviewId);

    #endregion

    #region Media

    /// <summary>
    /// Adds media to a review.
    /// </summary>
    Task<ReviewMediaDto?> AddReviewMediaAsync(int reviewId, CreateReviewMediaDto dto);

    /// <summary>
    /// Removes media from a review.
    /// </summary>
    Task<bool> RemoveReviewMediaAsync(int mediaId);

    #endregion

    #region Widget/Public API

    /// <summary>
    /// Gets reviews for widget display (approved only).
    /// </summary>
    Task<PaginatedResult<WidgetReviewDto>> GetWidgetReviewsAsync(
        string apiKey,
        long productId,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null);

    /// <summary>
    /// Gets product review summary for widget.
    /// </summary>
    Task<ProductReviewSummaryDto?> GetProductReviewSummaryAsync(string apiKey, long productId);

    /// <summary>
    /// Gets widget configuration.
    /// </summary>
    Task<WidgetConfigDto?> GetWidgetConfigAsync(string apiKey);

    /// <summary>
    /// Records a helpful vote.
    /// </summary>
    Task<bool> RecordHelpfulVoteAsync(int reviewId, bool isHelpful);

    #endregion

    #region Settings

    /// <summary>
    /// Gets review settings for a shop.
    /// </summary>
    Task<ReviewSettingsDto> GetSettingsAsync(string shopDomain);

    /// <summary>
    /// Updates review settings.
    /// </summary>
    Task<ReviewSettingsDto> UpdateSettingsAsync(string shopDomain, UpdateReviewSettingsDto dto);

    /// <summary>
    /// Regenerates the widget API key.
    /// </summary>
    Task<string> RegenerateApiKeyAsync(string shopDomain);

    #endregion

    #region Analytics

    /// <summary>
    /// Gets review analytics summary.
    /// </summary>
    Task<ReviewAnalyticsSummaryDto> GetAnalyticsSummaryAsync(string shopDomain, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets reviews pending moderation count.
    /// </summary>
    Task<int> GetPendingReviewCountAsync(string shopDomain);

    #endregion
}
