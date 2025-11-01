using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record VariantDto
    (
        string Id,
        string Title,
        string? Sku,
        decimal? Price,
        string? Option1,
        string? Option2,
        string? Option3
    );
}
