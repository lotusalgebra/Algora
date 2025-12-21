using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.DTOs
{
    public record LicenseDto
    {
        public string ShopDomain { get; init; } = string.Empty;
        public string PlanName { get; init; } = string.Empty;
        public DateTime ExpiryDate { get; init; }
        public bool IsActive { get; init; }
        public string Status { get; init; } = "active";
    }
}
