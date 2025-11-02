using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Licensing;

public class LicenseService : ILicenseService
{
    private readonly AppDbContext _db;

    public LicenseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LicenseDto?> GetLicenseAsync(string shopDomain)
    {
        if (string.IsNullOrWhiteSpace(shopDomain)) return null;

        var license = await _db.Licenses
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ShopDomain == shopDomain && l.IsActive);

        if (license == null) return null;

        return new LicenseDto
        {
            ShopDomain = license.ShopDomain,
            PlanName = license.PlanName,
            ExpiryDate = license.ExpiryDate,
            IsActive = license.IsActive,
            Status = license.Status
        };
    }

    public async Task<bool> CreateOrUpdateLicenseAsync(string shopDomain, string planName, string chargeId, DateTime expiry, bool isTrial)
    {
        if (string.IsNullOrWhiteSpace(shopDomain)) throw new ArgumentException("shopDomain is required", nameof(shopDomain));

        var existing = await _db.Licenses.FirstOrDefaultAsync(x => x.ShopDomain == shopDomain);
        if (existing != null)
        {
            existing.PlanName = planName;
            existing.ChargeId = chargeId;
            existing.ExpiryDate = expiry;
            existing.Status = isTrial ? "trial" : "active";
            existing.IsActive = true;

            _db.Licenses.Update(existing);
        }
        else
        {
            await _db.Licenses.AddAsync(new License
            {
                ShopDomain = shopDomain,
                PlanName = planName,
                ChargeId = chargeId,
                ExpiryDate = expiry,
                Status = isTrial ? "trial" : "active",
                IsActive = true,
                StartDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateLicenseAsync(string shopDomain)
    {
        if (string.IsNullOrWhiteSpace(shopDomain)) return false;

        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.ShopDomain == shopDomain);
        if (license == null) return false;

        license.IsActive = false;
        license.Status = "cancelled";

        _db.Licenses.Update(license);
        await _db.SaveChangesAsync();
        return true;
    }
}
