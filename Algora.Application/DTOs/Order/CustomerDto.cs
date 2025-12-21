using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record CustomerDto
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool VerifiedEmail { get; init; }
        public string State { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? Tags { get; init; }
    }
}
