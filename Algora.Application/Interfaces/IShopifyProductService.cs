using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Provides product and variant operations against the Shopify Admin API.
    /// Implementations typically use the Graph API and map results to DTOs.
    /// </summary>
    public interface IShopifyProductService
    {
        /// <summary>
        /// Queries products using the provided search filter.
        /// </summary>
        /// <param name="filter">
        /// A <see cref="ProductSearchFilter"/> describing search criteria (title, vendor, tags, etc.).
        /// Implementations should translate this filter to the appropriate GraphQL or REST query.
        /// </param>
        /// <param name="first">
        /// Maximum number of products to return (page size). Default is 25.
        /// </param>
        /// <returns>
        /// A task that resolves to a read-only list of <see cref="ProductDto"/> matching the filter.
        /// Implementations may return fewer items than requested.
        /// </returns>
        Task<IReadOnlyList<ProductDto>> GetProductsAsync(ProductSearchFilter filter, int first = 25);

        /// <summary>
        /// Creates a new variant for the specified product.
        /// </summary>
        /// <param name="productGid">
        /// The product GraphQL ID (for example: <c>gid://shopify/Product/1234567890</c>).
        /// </param>
        /// <param name="title">Variant title (for example: "Large / Red").</param>
        /// <param name="price">Price for the variant in shop currency.</param>
        /// <param name="sku">Optional SKU for the variant.</param>
        /// <param name="option1">Optional first option value (size, color, etc.).</param>
        /// <param name="option2">Optional second option value.</param>
        /// <param name="option3">Optional third option value.</param>
        /// <returns>
        /// A task that resolves to the created <see cref="VariantDto"/> containing assigned identifiers.
        /// Throws or returns an error if creation fails.
        /// </returns>
        Task<VariantDto> CreateVariantAsync(string productGid, string title, decimal price, string? sku, string? option1 = null, string? option2 = null, string? option3 = null);

        /// <summary>
        /// Updates an existing variant.
        /// </summary>
        /// <param name="variantGid">
        /// The variant GraphQL ID (for example: <c>gid://shopify/ProductVariant/987654321</c>).
        /// </param>
        /// <param name="title">Optional new title. If <c>null</c>, the title is not changed.</param>
        /// <param name="price">Optional new price. If <c>null</c>, the price is not changed.</param>
        /// <param name="sku">Optional new SKU. If <c>null</c>, the SKU is not changed.</param>
        /// <param name="option1">Optional new first option value. If <c>null</c>, the option is not changed.</param>
        /// <param name="option2">Optional new second option value. If <c>null</c>, the option is not changed.</param>
        /// <param name="option3">Optional new third option value. If <c>null</c>, the option is not changed.</param>
        /// <returns>
        /// A task that resolves to the updated <see cref="VariantDto"/>.
        /// Implementations should return the latest state after the update or throw on error.
        /// </returns>
        Task<VariantDto> UpdateVariantAsync(string variantGid, string? title = null, decimal? price = null, string? sku = null, string? option1 = null, string? option2 = null, string? option3 = null);
    }
}
