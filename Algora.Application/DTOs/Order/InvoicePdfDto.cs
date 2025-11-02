using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record InvoicePdfDto
    {
        public string InvoiceNumber { get; init; } = string.Empty;
        public DateTime InvoiceDate { get; init; } = DateTime.Now;
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
        public string BillingAddress { get; init; } = string.Empty;
        public string ShippingAddress { get; init; } = string.Empty;
        public decimal Subtotal { get; init; }
        public decimal Tax { get; init; }
        public decimal Total { get; init; }
        public IEnumerable<InvoiceLineDto> Lines { get; init; } = [];
    }
}
