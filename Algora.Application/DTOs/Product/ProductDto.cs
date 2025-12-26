using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    /// <summary>
    /// Data transfer object that represents a product returned from the store.
    /// Typically mapped from Shopify product responses (GraphQL or REST).
    /// </summary>
    /// <param name="Id">
    /// The product identifier as a string. For Graph API this is usually the global id (GID)
    /// (for example: "gid://shopify/Product/1234567890"). For REST responses this may be the stringified id.
    /// </param>
    /// <param name="NumericId">
    /// Numeric product identifier (Shopify's numeric id). Use this for numeric comparisons and routing.
    /// </param>
    /// <param name="Title">Human-friendly product title.</param>
    /// <param name="Handle">
    /// Optional product handle/slug used in storefront URLs (for example: "classic-t-shirt").
    /// </param>
    /// <param name="Tags">
    /// Read-only list of tags associated with the product. Each tag is a single string value.
    /// </param>
    /// <param name="Variants">
    /// Read-only list of variants for the product. Each entry describes a product variant (price, sku, options).
    /// </param>
    /// <param name="Images">
    /// Read-only list of images for the product.
    /// </param>
    /// <param name="Description">
    /// Product description (HTML).
    /// </param>
    /// <param name="Vendor">
    /// Product vendor.
    /// </param>
    /// <param name="ProductType">
    /// Product type category.
    /// </param>
    public record ProductDto
    (
        string Id,
        long NumericId,
        string Title,
        string? Handle,
        IReadOnlyList<string> Tags,
        IReadOnlyList<VariantDto> Variants,
        IReadOnlyList<ProductImageDto>? Images = null,
        string? Description = null,
        string? Vendor = null,
        string? ProductType = null
    );
}
