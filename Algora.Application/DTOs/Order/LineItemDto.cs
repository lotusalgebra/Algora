using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record LineItemDto
    {
        public string? Title { get; init; }
        public int Quantity { get; init; }
        public decimal Price { get; init; }
        public string? VariantId { get; init; }
        public long? ProductId { get; init; }
    }
}
