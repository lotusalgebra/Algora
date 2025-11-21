using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    /// <summary>
    /// DTO that contains input attributes and the generated product description.
    /// Used by AI/product-description services to return a marketing-ready description
    /// together with the source metadata.
    /// </summary>
    public record ProductDescriptionDto
    {
        /// <summary>
        /// Optional numeric identifier of the product in the store.
        /// </summary>
        public long ProductId { get; init; }

        /// <summary>
        /// Product title or name (e.g. "Classic T-Shirt").
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Product category or collection (e.g. "Apparel", "Home").
        /// </summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// Primary color or color options for the product (e.g. "Red", "Black/White").
        /// </summary>
        public string Color { get; init; } = string.Empty;

        /// <summary>
        /// Main material or fabric (e.g. "100% cotton", "Stainless steel").
        /// </summary>
        public string Material { get; init; } = string.Empty;

        /// <summary>
        /// Short comma-separated list of key features or a concise features summary.
        /// The AI service can use this to emphasize selling points.
        /// </summary>
        public string Features { get; init; } = string.Empty;

        /// <summary>
        /// The generated, human-readable product description produced by the AI service.
        /// Should be suitable for use in product pages or marketing copy.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}
