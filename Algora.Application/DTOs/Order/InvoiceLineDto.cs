using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record InvoiceLineDto
    {
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
        public decimal Total => Quantity * Price;
    }
}
