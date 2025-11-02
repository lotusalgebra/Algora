using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface ILicenseService
    {
        Task<LicenseDto?> GetLicenseAsync(string shopDomain);
        Task<bool> CreateOrUpdateLicenseAsync(string shopDomain, string planName, string chargeId, DateTime expiry, bool isTrial);
        Task<bool> DeactivateLicenseAsync(string shopDomain);
    }
}
