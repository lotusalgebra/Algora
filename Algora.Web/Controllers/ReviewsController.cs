using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IReviewImportService _importService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        IReviewService reviewService,
        IReviewImportService importService,
        IShopContext shopContext,
        ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _importService = importService;
        _shopContext = shopContext;
        _logger = logger;
    }

    #region Admin Endpoints

    /// <summary>
    /// Get all reviews with filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] ReviewFilterDto filter)
    {
        try
        {
            var reviews = await _reviewService.GetReviewsAsync(_shopContext.ShopDomain, filter);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews");
            return StatusCode(500, new { error = "Failed to get reviews" });
        }
    }

    /// <summary>
    /// Get a review by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReview(int id)
    {
        try
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
                return NotFound(new { error = "Review not found" });

            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to get review" });
        }
    }

    /// <summary>
    /// Create a new review (manual entry).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        try
        {
            var review = await _reviewService.CreateReviewAsync(_shopContext.ShopDomain, dto);
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { error = "Failed to create review" });
        }
    }

    /// <summary>
    /// Update a review.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            var review = await _reviewService.UpdateReviewAsync(id, dto);
            if (review == null)
                return NotFound(new { error = "Review not found" });

            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to update review" });
        }
    }

    /// <summary>
    /// Delete a review.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        try
        {
            var result = await _reviewService.DeleteReviewAsync(id);
            if (!result)
                return NotFound(new { error = "Review not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to delete review" });
        }
    }

    /// <summary>
    /// Bulk delete reviews.
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteReviews([FromBody] List<int> reviewIds)
    {
        try
        {
            var count = await _reviewService.DeleteReviewsAsync(reviewIds);
            return Ok(new { deleted = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting reviews");
            return StatusCode(500, new { error = "Failed to delete reviews" });
        }
    }

    #endregion

    #region Moderation Endpoints

    /// <summary>
    /// Approve a review.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> ApproveReview(int id, [FromBody] ModerateReviewDto? dto)
    {
        try
        {
            var result = await _reviewService.ApproveReviewAsync(id, dto?.ModerationNote);
            if (!result)
                return NotFound(new { error = "Review not found" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to approve review" });
        }
    }

    /// <summary>
    /// Reject a review.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> RejectReview(int id, [FromBody] ModerateReviewDto? dto)
    {
        try
        {
            var result = await _reviewService.RejectReviewAsync(id, dto?.ModerationNote);
            if (!result)
                return NotFound(new { error = "Review not found" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to reject review" });
        }
    }

    /// <summary>
    /// Bulk approve reviews.
    /// </summary>
    [HttpPost("bulk-approve")]
    public async Task<IActionResult> BulkApproveReviews([FromBody] List<int> reviewIds)
    {
        try
        {
            var count = await _reviewService.BulkApproveReviewsAsync(reviewIds);
            return Ok(new { approved = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving reviews");
            return StatusCode(500, new { error = "Failed to approve reviews" });
        }
    }

    /// <summary>
    /// Bulk reject reviews.
    /// </summary>
    [HttpPost("bulk-reject")]
    public async Task<IActionResult> BulkRejectReviews([FromBody] List<int> reviewIds)
    {
        try
        {
            var count = await _reviewService.BulkRejectReviewsAsync(reviewIds);
            return Ok(new { rejected = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk rejecting reviews");
            return StatusCode(500, new { error = "Failed to reject reviews" });
        }
    }

    /// <summary>
    /// Toggle featured status.
    /// </summary>
    [HttpPost("{id:int}/toggle-featured")]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        try
        {
            var result = await _reviewService.ToggleFeaturedAsync(id);
            if (!result)
                return NotFound(new { error = "Review not found" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling featured status for review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to toggle featured status" });
        }
    }

    #endregion

    #region Media Endpoints

    /// <summary>
    /// Add media to a review.
    /// </summary>
    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> AddMedia(int id, [FromBody] CreateReviewMediaDto dto)
    {
        try
        {
            var media = await _reviewService.AddReviewMediaAsync(id, dto);
            if (media == null)
                return NotFound(new { error = "Review not found" });

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding media to review {ReviewId}", id);
            return StatusCode(500, new { error = "Failed to add media" });
        }
    }

    /// <summary>
    /// Remove media from a review.
    /// </summary>
    [HttpDelete("media/{mediaId:int}")]
    public async Task<IActionResult> RemoveMedia(int mediaId)
    {
        try
        {
            var result = await _reviewService.RemoveReviewMediaAsync(mediaId);
            if (!result)
                return NotFound(new { error = "Media not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing media {MediaId}", mediaId);
            return StatusCode(500, new { error = "Failed to remove media" });
        }
    }

    #endregion

    #region Settings Endpoints

    /// <summary>
    /// Get review settings.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await _reviewService.GetSettingsAsync(_shopContext.ShopDomain);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update review settings.
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateReviewSettingsDto dto)
    {
        try
        {
            var settings = await _reviewService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    /// <summary>
    /// Regenerate widget API key.
    /// </summary>
    [HttpPost("settings/regenerate-api-key")]
    public async Task<IActionResult> RegenerateApiKey()
    {
        try
        {
            var apiKey = await _reviewService.RegenerateApiKeyAsync(_shopContext.ShopDomain);
            return Ok(new { apiKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating API key");
            return StatusCode(500, new { error = "Failed to regenerate API key" });
        }
    }

    #endregion

    #region Analytics Endpoints

    /// <summary>
    /// Get review analytics summary.
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var analytics = await _reviewService.GetAnalyticsSummaryAsync(_shopContext.ShopDomain, fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review analytics");
            return StatusCode(500, new { error = "Failed to get analytics" });
        }
    }

    /// <summary>
    /// Get pending review count.
    /// </summary>
    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        try
        {
            var count = await _reviewService.GetPendingReviewCountAsync(_shopContext.ShopDomain);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending review count");
            return StatusCode(500, new { error = "Failed to get pending count" });
        }
    }

    #endregion

    #region Import Endpoints

    /// <summary>
    /// Parse an Amazon/AliExpress product URL.
    /// </summary>
    [HttpGet("imports/parse-url")]
    public async Task<IActionResult> ParseUrl([FromQuery] string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest(new { error = "URL is required" });

            var result = await _importService.ParseProductUrlAsync(url);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing URL: {Url}", url);
            return StatusCode(500, new { error = "Failed to parse URL" });
        }
    }

    /// <summary>
    /// Get all import jobs.
    /// </summary>
    [HttpGet("imports")]
    public async Task<IActionResult> GetImportJobs(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var jobs = await _importService.GetImportJobsAsync(_shopContext.ShopDomain, status, page, pageSize);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import jobs");
            return StatusCode(500, new { error = "Failed to get import jobs" });
        }
    }

    /// <summary>
    /// Get an import job by ID.
    /// </summary>
    [HttpGet("imports/{id:int}")]
    public async Task<IActionResult> GetImportJob(int id)
    {
        try
        {
            var job = await _importService.GetImportJobByIdAsync(id);
            if (job == null)
                return NotFound(new { error = "Import job not found" });

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import job {JobId}", id);
            return StatusCode(500, new { error = "Failed to get import job" });
        }
    }

    /// <summary>
    /// Create an import job.
    /// </summary>
    [HttpPost("imports")]
    public async Task<IActionResult> CreateImportJob([FromBody] CreateReviewImportJobDto dto)
    {
        try
        {
            var job = await _importService.CreateImportJobAsync(_shopContext.ShopDomain, dto);
            return CreatedAtAction(nameof(GetImportJob), new { id = job.Id }, job);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating import job");
            return StatusCode(500, new { error = "Failed to create import job" });
        }
    }

    /// <summary>
    /// Get import job progress.
    /// </summary>
    [HttpGet("imports/{id:int}/progress")]
    public async Task<IActionResult> GetImportProgress(int id)
    {
        try
        {
            var progress = await _importService.GetImportProgressAsync(id);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import progress for job {JobId}", id);
            return StatusCode(500, new { error = "Failed to get progress" });
        }
    }

    /// <summary>
    /// Cancel an import job.
    /// </summary>
    [HttpPost("imports/{id:int}/cancel")]
    public async Task<IActionResult> CancelImportJob(int id)
    {
        try
        {
            var result = await _importService.CancelImportJobAsync(id);
            if (!result)
                return BadRequest(new { error = "Cannot cancel job (not found or already processed)" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling import job {JobId}", id);
            return StatusCode(500, new { error = "Failed to cancel job" });
        }
    }

    /// <summary>
    /// Retry a failed import job.
    /// </summary>
    [HttpPost("imports/{id:int}/retry")]
    public async Task<IActionResult> RetryImportJob(int id)
    {
        try
        {
            var result = await _importService.RetryImportJobAsync(id);
            if (!result)
                return BadRequest(new { error = "Cannot retry job (not found or not in failed/cancelled state)" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying import job {JobId}", id);
            return StatusCode(500, new { error = "Failed to retry job" });
        }
    }

    /// <summary>
    /// Delete an import job.
    /// </summary>
    [HttpDelete("imports/{id:int}")]
    public async Task<IActionResult> DeleteImportJob(int id, [FromQuery] bool deleteReviews = false)
    {
        try
        {
            var result = await _importService.DeleteImportJobAsync(id, deleteReviews);
            if (!result)
                return BadRequest(new { error = "Cannot delete job (not found or still processing)" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting import job {JobId}", id);
            return StatusCode(500, new { error = "Failed to delete job" });
        }
    }

    #endregion

    #region Widget/Public Endpoints (Anonymous)

    /// <summary>
    /// Get reviews for widget display.
    /// </summary>
    [HttpGet("widget/{apiKey}/product/{productId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWidgetReviews(
        string apiKey,
        long productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null)
    {
        try
        {
            var reviews = await _reviewService.GetWidgetReviewsAsync(apiKey, productId, page, pageSize, sortBy);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget reviews");
            return StatusCode(500, new { error = "Failed to get reviews" });
        }
    }

    /// <summary>
    /// Get product review summary for widget.
    /// </summary>
    [HttpGet("widget/{apiKey}/summary/{productId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductSummary(string apiKey, long productId)
    {
        try
        {
            var summary = await _reviewService.GetProductReviewSummaryAsync(apiKey, productId);
            if (summary == null)
                return NotFound(new { error = "Product not found" });

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product review summary");
            return StatusCode(500, new { error = "Failed to get summary" });
        }
    }

    /// <summary>
    /// Get widget configuration.
    /// </summary>
    [HttpGet("widget/{apiKey}/config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWidgetConfig(string apiKey)
    {
        try
        {
            var config = await _reviewService.GetWidgetConfigAsync(apiKey);
            if (config == null)
                return NotFound(new { error = "Invalid API key" });

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget config");
            return StatusCode(500, new { error = "Failed to get config" });
        }
    }

    /// <summary>
    /// Submit a customer review.
    /// </summary>
    [HttpPost("widget/{apiKey}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitReview(string apiKey, [FromBody] SubmitReviewDto dto)
    {
        try
        {
            // Validate API key first
            var config = await _reviewService.GetWidgetConfigAsync(apiKey);
            if (config == null)
                return NotFound(new { error = "Invalid API key" });

            if (!config.AllowSubmission)
                return BadRequest(new { error = "Review submissions are disabled" });

            // Get shop domain from settings
            var settings = await _reviewService.GetSettingsAsync(_shopContext.ShopDomain);

            var review = await _reviewService.SubmitCustomerReviewAsync(_shopContext.ShopDomain, dto);
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting customer review");
            return StatusCode(500, new { error = "Failed to submit review" });
        }
    }

    /// <summary>
    /// Record a helpful vote.
    /// </summary>
    [HttpPost("widget/vote/{reviewId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordVote(int reviewId, [FromQuery] bool isHelpful)
    {
        try
        {
            var result = await _reviewService.RecordHelpfulVoteAsync(reviewId, isHelpful);
            if (!result)
                return NotFound(new { error = "Review not found" });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording vote for review {ReviewId}", reviewId);
            return StatusCode(500, new { error = "Failed to record vote" });
        }
    }

    #endregion
}
