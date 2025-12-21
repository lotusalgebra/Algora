using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record CartItemDto
    {
        public string? Title { get; init; }
        public int Quantity { get; init; }
        public decimal Price { get; init; }
    }
}
