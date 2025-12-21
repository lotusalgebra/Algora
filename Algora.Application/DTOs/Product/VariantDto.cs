using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    /// <summary>
    /// Represents a product variant returned from the store.
    /// Usually mapped from Shopify responses and contains identifying information,
    /// pricing and up to three option values (size, color, etc.).
    /// </summary>
    /// <param name="Id">
    /// Variant identifier as a string. For GraphQL this is typically the global id (GID)
    /// (example: "gid://shopify/ProductVariant/1234567890").
    /// </param>
    /// <param name="Title">Human-friendly variant title (for example "Large / Blue").</param>
    /// <param name="Sku">Optional SKU assigned to the variant.</param>
    /// <param name="Price">Optional price for the variant in shop currency.</param>
    /// <param name="Option1">Optional first option value (for example size).</param>
    /// <param name="Option2">Optional second option value (for example color).</param>
    /// <param name="Option3">Optional third option value.</param>
    /// <param name="InventoryQuantity">Optional inventory quantity (may be null if not available).</param>
    public record VariantDto
    (
        string Id,
        string Title,
        string? Sku,
        decimal? Price,
        string? Option1,
        string? Option2,
        string? Option3,
        int? InventoryQuantity = null
    );
}
