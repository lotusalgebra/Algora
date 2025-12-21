namespace Algora.Application.DTOs.Reviews;

#region Review DTOs

/// <summary>
/// DTO for review display.
/// </summary>
public class ReviewDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public long? PlatformProductId { get; set; }
    public string? ProductTitle { get; set; }
    public string? ProductSku { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerEmail { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public string? ModerationNote { get; set; }
    public int HelpfulVotes { get; set; }
    public int UnhelpfulVotes { get; set; }
    public DateTime ReviewDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<ReviewMediaDto> Media { get; set; } = new();
}

/// <summary>
/// DTO for review list view (minimal data).
/// </summary>
public class ReviewListDto
{
    public int Id { get; set; }
    public string? ProductTitle { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public bool IsFeatured { get; set; }
    public bool HasMedia { get; set; }
    public int MediaCount { get; set; }
    public DateTime ReviewDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new review (manual entry).
/// </summary>
public class CreateReviewDto
{
    public long? PlatformProductId { get; set; }
    public string? ProductTitle { get; set; }
    public string? ProductSku { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerEmail { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public DateTime? ReviewDate { get; set; }
    public List<CreateReviewMediaDto> Media { get; set; } = new();
}

/// <summary>
/// DTO for customer-submitted review.
/// </summary>
public class SubmitReviewDto
{
    public long PlatformProductId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerEmail { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? TrackingToken { get; set; }
    public List<string>? MediaUrls { get; set; }
}

/// <summary>
/// DTO for updating a review.
/// </summary>
public class UpdateReviewDto
{
    public string? ReviewerName { get; set; }
    public int? Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public bool? IsVerifiedPurchase { get; set; }
    public bool? IsFeatured { get; set; }
    public string? ModerationNote { get; set; }
}

/// <summary>
/// DTO for review moderation action.
/// </summary>
public class ModerateReviewDto
{
    public string Action { get; set; } = string.Empty; // approve, reject
    public string? ModerationNote { get; set; }
}

/// <summary>
/// DTO for review filter options.
/// </summary>
public class ReviewFilterDto
{
    public long? ProductId { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public bool? HasMedia { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsVerifiedPurchase { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; } // date, rating, helpful
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

#endregion

#region Review Media DTOs

/// <summary>
/// DTO for review media display.
/// </summary>
public class ReviewMediaDto
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for creating review media.
/// </summary>
public class CreateReviewMediaDto
{
    public string MediaType { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}

#endregion

#region Review Import DTOs

/// <summary>
/// DTO for import job display.
/// </summary>
public class ReviewImportJobDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string? SourceProductId { get; set; }
    public string? SourceProductTitle { get; set; }
    public int? TargetProductId { get; set; }
    public long? TargetPlatformProductId { get; set; }
    public string? TargetProductTitle { get; set; }
    public string MappingMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public int ImportedReviews { get; set; }
    public int SkippedReviews { get; set; }
    public int FailedReviews { get; set; }
    public string? ErrorMessage { get; set; }
    public int? MinRating { get; set; }
    public bool IncludePhotosOnly { get; set; }
    public DateTime? ReviewsAfterDate { get; set; }
    public int? MaxReviews { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for import job list view.
/// </summary>
public class ReviewImportJobListDto
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceProductTitle { get; set; }
    public string? TargetProductTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public int ImportedReviews { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// DTO for creating an import job.
/// </summary>
public class CreateReviewImportJobDto
{
    public string SourceUrl { get; set; } = string.Empty;
    public long? TargetPlatformProductId { get; set; }
    public string? TargetProductTitle { get; set; }
    public string MappingMethod { get; set; } = "manual";
    public int? MinRating { get; set; }
    public bool IncludePhotosOnly { get; set; }
    public DateTime? ReviewsAfterDate { get; set; }
    public int? MaxReviews { get; set; }
}

/// <summary>
/// DTO for URL parsing result.
/// </summary>
public class ParsedReviewUrlDto
{
    public bool IsValid { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public string? ProductTitle { get; set; }
    public string? ProductImageUrl { get; set; }
    public int? TotalReviewCount { get; set; }
    public decimal? AverageRating { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Email Automation DTOs

/// <summary>
/// DTO for email automation display.
/// </summary>
public class ReviewEmailAutomationDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
    public decimal? MinOrderValue { get; set; }
    public List<long>? ProductIds { get; set; }
    public List<long>? ExcludedProductIds { get; set; }
    public List<string>? CustomerTags { get; set; }
    public List<string>? ExcludedCustomerTags { get; set; }
    public bool ExcludeRepeatedCustomers { get; set; }
    public int? RepeatedCustomerExclusionDays { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? EmailTemplateId { get; set; }
    public string? EmailTemplateName { get; set; }
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalReviewsCollected { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ClickRate { get; set; }
    public decimal ConversionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for email automation list view.
/// </summary>
public class ReviewEmailAutomationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public int TotalSent { get; set; }
    public int TotalReviewsCollected { get; set; }
    public decimal ConversionRate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating an email automation.
/// </summary>
public class CreateReviewEmailAutomationDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string TriggerType { get; set; } = "after_delivery";
    public int DelayDays { get; set; } = 7;
    public int DelayHours { get; set; }
    public decimal? MinOrderValue { get; set; }
    public List<long>? ProductIds { get; set; }
    public List<long>? ExcludedProductIds { get; set; }
    public List<string>? CustomerTags { get; set; }
    public List<string>? ExcludedCustomerTags { get; set; }
    public bool ExcludeRepeatedCustomers { get; set; } = true;
    public int? RepeatedCustomerExclusionDays { get; set; } = 30;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? EmailTemplateId { get; set; }
}

/// <summary>
/// DTO for updating an email automation.
/// </summary>
public class UpdateReviewEmailAutomationDto
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public string? TriggerType { get; set; }
    public int? DelayDays { get; set; }
    public int? DelayHours { get; set; }
    public decimal? MinOrderValue { get; set; }
    public List<long>? ProductIds { get; set; }
    public List<long>? ExcludedProductIds { get; set; }
    public List<string>? CustomerTags { get; set; }
    public List<string>? ExcludedCustomerTags { get; set; }
    public bool? ExcludeRepeatedCustomers { get; set; }
    public int? RepeatedCustomerExclusionDays { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int? EmailTemplateId { get; set; }
}

/// <summary>
/// DTO for email log display.
/// </summary>
public class ReviewEmailLogDto
{
    public int Id { get; set; }
    public int AutomationId { get; set; }
    public string AutomationName { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? ReviewSubmittedAt { get; set; }
    public int? ReviewId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Review Settings DTOs

/// <summary>
/// DTO for review settings display.
/// </summary>
public class ReviewSettingsDto
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string WidgetTheme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#000000";
    public string AccentColor { get; set; } = "#f5a623";
    public string StarColor { get; set; } = "#ffc107";
    public string WidgetLayout { get; set; } = "list";
    public int ReviewsPerPage { get; set; } = 10;
    public bool ShowReviewerName { get; set; } = true;
    public bool ShowReviewDate { get; set; } = true;
    public bool ShowVerifiedBadge { get; set; } = true;
    public bool ShowPhotoGallery { get; set; } = true;
    public bool AllowCustomerReviews { get; set; } = true;
    public bool RequireApproval { get; set; } = true;
    public bool AutoApproveReviews { get; set; }
    public int? AutoApproveMinRating { get; set; }
    public bool AutoApproveVerifiedOnly { get; set; }
    public bool TranslateImportedReviews { get; set; }
    public string? TranslateToLanguage { get; set; }
    public bool RemoveSourceBranding { get; set; }
    public bool ImportPhotos { get; set; }
    public string WidgetApiKey { get; set; } = string.Empty;
    public string? DefaultEmailFromName { get; set; }
    public string? DefaultEmailFromAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating review settings.
/// </summary>
public class UpdateReviewSettingsDto
{
    public string? WidgetTheme { get; set; }
    public string? PrimaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? StarColor { get; set; }
    public string? WidgetLayout { get; set; }
    public int? ReviewsPerPage { get; set; }
    public bool? ShowReviewerName { get; set; }
    public bool? ShowReviewDate { get; set; }
    public bool? ShowVerifiedBadge { get; set; }
    public bool? ShowPhotoGallery { get; set; }
    public bool? AllowCustomerReviews { get; set; }
    public bool? RequireApproval { get; set; }
    public bool? AutoApproveReviews { get; set; }
    public int? AutoApproveMinRating { get; set; }
    public bool? AutoApproveVerifiedOnly { get; set; }
    public bool? TranslateImportedReviews { get; set; }
    public string? TranslateToLanguage { get; set; }
    public bool? RemoveSourceBranding { get; set; }
    public bool? ImportPhotos { get; set; }
    public string? DefaultEmailFromName { get; set; }
    public string? DefaultEmailFromAddress { get; set; }
}

#endregion

#region Widget DTOs

/// <summary>
/// DTO for widget review data (public API).
/// </summary>
public class WidgetReviewDto
{
    public int Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public DateTime ReviewDate { get; set; }
    public int HelpfulVotes { get; set; }
    public List<WidgetReviewMediaDto> Media { get; set; } = new();
}

/// <summary>
/// DTO for widget review media.
/// </summary>
public class WidgetReviewMediaDto
{
    public string MediaType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// DTO for product review summary (widget).
/// </summary>
public class ProductReviewSummaryDto
{
    public long ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public int PhotoReviewCount { get; set; }
    public int VerifiedPurchaseCount { get; set; }
}

/// <summary>
/// DTO for widget configuration (passed to JS).
/// </summary>
public class WidgetConfigDto
{
    public string Theme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#000000";
    public string AccentColor { get; set; } = "#f5a623";
    public string StarColor { get; set; } = "#ffc107";
    public string Layout { get; set; } = "list";
    public int ReviewsPerPage { get; set; } = 10;
    public bool ShowReviewerName { get; set; } = true;
    public bool ShowReviewDate { get; set; } = true;
    public bool ShowVerifiedBadge { get; set; } = true;
    public bool ShowPhotoGallery { get; set; } = true;
    public bool AllowSubmission { get; set; } = true;
}

#endregion

#region Analytics DTOs

/// <summary>
/// DTO for review analytics summary.
/// </summary>
public class ReviewAnalyticsSummaryDto
{
    public int TotalReviews { get; set; }
    public int PendingReviews { get; set; }
    public int ApprovedReviews { get; set; }
    public int RejectedReviews { get; set; }
    public decimal AverageRating { get; set; }
    public int PhotoReviews { get; set; }
    public int VerifiedPurchaseReviews { get; set; }
    public int ImportedReviews { get; set; }
    public int EmailCollectedReviews { get; set; }
    public int ManualReviews { get; set; }
    public int ReviewsThisMonth { get; set; }
    public int ReviewsLastMonth { get; set; }
    public decimal MonthOverMonthGrowth { get; set; }
    public List<RatingDistributionDto> RatingDistribution { get; set; } = new();
    public List<ReviewSourceDistributionDto> SourceDistribution { get; set; } = new();
    public List<DailyReviewCountDto> DailyReviewCounts { get; set; } = new();
}

/// <summary>
/// DTO for rating distribution.
/// </summary>
public class RatingDistributionDto
{
    public int Rating { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for review source distribution.
/// </summary>
public class ReviewSourceDistributionDto
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for daily review counts.
/// </summary>
public class DailyReviewCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

#endregion
