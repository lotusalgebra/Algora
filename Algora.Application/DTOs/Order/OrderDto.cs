using Algora.Application.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record OrderDto
    {
        public long Id { get; init; }
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? FinancialStatus { get; init; }
        public string? FulfillmentStatus { get; init; }
        public decimal TotalPrice { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? Note { get; init; }
        public string? Tags { get; init; }
        public CustomerDto? Customer { get; init; }
        public AddressDto? BillingAddress { get; init; }
        public AddressDto? ShippingAddress { get; init; }
        public IEnumerable<LineItemDto> LineItems { get; init; } = [];
    }
}
