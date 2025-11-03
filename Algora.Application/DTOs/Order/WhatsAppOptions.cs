using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs.Order
{
    public record WhatsAppOptionsDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string PhoneNumberId { get; init; } = string.Empty;
        public string? TemplateNamespace { get; init; }
        public string? ClientId { get; init; }
    }
}
