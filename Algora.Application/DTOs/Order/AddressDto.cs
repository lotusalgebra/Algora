using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs.Order
{
    public record AddressDto
    {
        public string? Name { get; init; }
        public string? Address1 { get; init; }
        public string? Address2 { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? Country { get; init; }
        public string? Zip { get; init; }
        public string? Phone { get; init; }
    }
}
