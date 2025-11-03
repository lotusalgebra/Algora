using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record InvoiceDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
        public decimal TotalPrice { get; init; }
        public string Status { get; init; } = string.Empty; // open, completed, invoice_sent
        public DateTime CreatedAt { get; init; }
        public bool Paid { get; init; }
    }
}
