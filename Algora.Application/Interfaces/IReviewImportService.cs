using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for importing reviews from external sources (Amazon, AliExpress).
/// </summary>
public interface IReviewImportService
{
    #region Import Jobs

    /// <summary>
    /// Gets import jobs for a shop with pagination.
    /// </summary>
    Task<PaginatedResult<ReviewImportJobListDto>> GetImportJobsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets an import job by ID.
    /// </summary>
    Task<ReviewImportJobDto?> GetImportJobByIdAsync(int jobId);

    /// <summary>
    /// Creates a new import job.
    /// </summary>
    Task<ReviewImportJobDto> CreateImportJobAsync(string shopDomain, CreateReviewImportJobDto dto);

    /// <summary>
    /// Cancels a pending or processing import job.
    /// </summary>
    Task<bool> CancelImportJobAsync(int jobId);

    /// <summary>
    /// Retries a failed import job.
    /// </summary>
    Task<bool> RetryImportJobAsync(int jobId);

    /// <summary>
    /// Deletes an import job and optionally its imported reviews.
    /// </summary>
    Task<bool> DeleteImportJobAsync(int jobId, bool deleteReviews = false);

    #endregion

    #region URL Parsing

    /// <summary>
    /// Parses a product URL to extract source info (Amazon ASIN, AliExpress product ID).
    /// </summary>
    Task<ParsedReviewUrlDto> ParseProductUrlAsync(string url);

    /// <summary>
    /// Validates that a URL is supported for import.
    /// </summary>
    bool IsSupportedUrl(string url);

    /// <summary>
    /// Gets the source type from a URL (amazon, aliexpress).
    /// </summary>
    string? GetSourceType(string url);

    #endregion

    #region Import Processing

    /// <summary>
    /// Processes pending import jobs (called by background service).
    /// </summary>
    Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a specific import job.
    /// </summary>
    Task ProcessImportJobAsync(int jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the progress of an import job.
    /// </summary>
    Task<ImportProgressDto> GetImportProgressAsync(int jobId);

    #endregion
}

/// <summary>
/// DTO for import progress tracking.
/// </summary>
public class ImportProgressDto
{
    public int JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public int ImportedReviews { get; set; }
    public int SkippedReviews { get; set; }
    public int FailedReviews { get; set; }
    public int ProgressPercent { get; set; }
    public string? CurrentAction { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Log { get; set; } = new();
}
