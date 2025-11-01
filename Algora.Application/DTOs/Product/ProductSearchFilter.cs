using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record ProductSearchFilter
    (
        string? Name,
        string? Tag,
        decimal? MinPrice,
        decimal? MaxPrice
    );
}
