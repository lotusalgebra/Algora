using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record ProductDescriptionDto
    {
        public long ProductId { get; init; }                  
        public string Title { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Color { get; init; } = string.Empty;
        public string Material { get; init; } = string.Empty;
        public string Features { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}
