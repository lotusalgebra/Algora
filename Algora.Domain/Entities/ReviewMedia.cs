namespace Algora.Domain.Entities;

/// <summary>
/// Represents a photo or video attached to a review.
/// </summary>
public class ReviewMedia
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    /// <summary>
    /// Media type: image, video
    /// </summary>
    public string MediaType { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
