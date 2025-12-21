using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Service that produces product descriptions using an AI-driven model.
    /// Implementations should combine the provided product attributes into a
    /// concise, marketing-ready description and return it as a <see cref="ProductDescriptionDto"/>.
    /// </summary>
    public interface IAiProductDescriptionService
    {
        /// <summary>
        /// Generates a product description from the given product attributes.
        /// </summary>
        /// <param name="title">Product title or name (e.g. "Classic T-Shirt").</param>
        /// <param name="category">Product category (e.g. "Apparel", "Home").</param>
        /// <param name="color">Primary color or color options for the product.</param>
        /// <param name="material">Main material or composition (e.g. "100% cotton").</param>
        /// <param name="features">Comma-separated key features or a short features summary.</param>
        /// <returns>
        /// A task that resolves to a <see cref="ProductDescriptionDto"/> containing the generated
        /// description and the input metadata. The implementation should populate the
        /// <c>Description</c> property with the AI output.
        /// </returns>
        Task<ProductDescriptionDto> GenerateDescriptionAsync(string title, string category, string color, string material, string features);
    }
}
