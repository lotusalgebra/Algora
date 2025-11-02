using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs.Order
{
    public record AbandonedCartDto
    {
        public long Id { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public decimal TotalPrice { get; init; }
        public DateTime? AbandonedAt { get; init; }
        // Fixed default initializer: use Enumerable.Empty<T>() rather than invalid "[]"
        public IEnumerable<CartItemDto> Items { get; init; } = Enumerable.Empty<CartItemDto>();
    }
}
