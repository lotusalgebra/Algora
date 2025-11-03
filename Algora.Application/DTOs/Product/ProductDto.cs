using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record ProductDto
    (
        string Id,
        long NumericId,
        string Title,
        string? Handle,
        IReadOnlyList<string> Tags,
        IReadOnlyList<VariantDto> Variants
    );
}
