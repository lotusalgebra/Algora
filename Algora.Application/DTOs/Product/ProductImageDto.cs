namespace Algora.Application.DTOs
{
    /// <summary>
    /// Represents a product image from the store.
    /// </summary>
    /// <param name="Id">Image identifier (GID for GraphQL).</param>
    /// <param name="Src">URL of the image.</param>
    /// <param name="Alt">Alternative text for the image.</param>
    /// <param name="Position">Position/order of the image.</param>
    /// <param name="Width">Image width in pixels.</param>
    /// <param name="Height">Image height in pixels.</param>
    public record ProductImageDto
    (
        string Id,
        string Src,
        string? Alt,
        int? Position,
        int? Width,
        int? Height
    );

    /// <summary>
    /// Input for uploading a new product image.
    /// </summary>
    public class UploadProductImageInput
    {
        /// <summary>
        /// Product ID (numeric) to add the image to.
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// Base64 encoded image data.
        /// </summary>
        public string? Base64Data { get; set; }

        /// <summary>
        /// Original filename with extension.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// MIME type (e.g., image/jpeg, image/png).
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Alternative text for the image.
        /// </summary>
        public string? Alt { get; set; }

        /// <summary>
        /// Optional image URL (if uploading from URL instead of file).
        /// </summary>
        public string? ImageUrl { get; set; }
    }
}
