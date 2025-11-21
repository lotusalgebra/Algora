using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    /// <summary>
    /// Filter used to search products.
    /// Implementations should apply non-null properties as filtering criteria when querying products.
    /// </summary>
    /// <param name="Name">Optional product name or partial title to match (case-insensitive substring search is common).</param>
    /// <param name="Tag">Optional product tag to filter by (exact match or tag contains depending on implementation).</param>
    /// <param name="MinPrice">Optional minimum price (inclusive) to filter product variants or prices; null means no minimum.</param>
    /// <param name="MaxPrice">Optional maximum price (inclusive) to filter product variants or prices; null means no maximum.</param>
    public record ProductSearchFilter
    (
        string? Name,
        string? Tag,
        decimal? MinPrice,
        decimal? MaxPrice
    );
}
