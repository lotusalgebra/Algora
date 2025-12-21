using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Licensing
{
    /// <summary>
    /// Persistence-backed implementation of <see cref="ILicenseService"/>.
    /// Responsible for reading, creating/updating and deactivating license records for shops.
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<LicenseService> _logger;

        /// <summary>
        /// Creates a new <see cref="LicenseService"/>.
        /// </summary>
        /// <param name="db">EF Core database context.</param>
        /// <param name="logger">Logger instance.</param>
        public LicenseService(AppDbContext db, ILogger<LicenseService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves the active license for the given shop domain.
        /// Returns null when no active license exists.
        /// </summary>
        /// <param name="shopDomain">Shop domain (myshopify domain).</param>
        /// <returns>License DTO or null when not found or inactive/expired.</returns>
        public async Task<LicenseDto?> GetLicenseAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain)) return null;

            var normalized = NormalizeDomain(shopDomain);

            try
            {
                // Consider license expired if ExpiryDate is in the past; only return active and non-expired licenses.
                var now = DateTime.UtcNow;
                var license = await _db.Licenses
                    .AsNoTracking()
                    .Where(l => l.ShopDomain.ToLower() == normalized && l.IsActive && (l.ExpiryDate == default || l.ExpiryDate >= now))
                    .FirstOrDefaultAsync();

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read license for shop {ShopDomain}", shopDomain);
                return null;
            }
        }

        /// <summary>
        /// Creates a new license or updates an existing one.
        /// Validates inputs and persists changes to the database.
        /// </summary>
        /// <param name="shopDomain">Shop domain (myshopify domain).</param>
        /// <param name="planName">Plan identifier/name.</param>
        /// <param name="chargeId">Billing charge identifier.</param>
        /// <param name="expiry">UTC expiry date for the license (must be in the future).</param>
        /// <param name="isTrial">True when license is a trial.</param>
        /// <returns>True when operation succeeded; otherwise false.</returns>
        public async Task<bool> CreateOrUpdateLicenseAsync(string shopDomain, string planName, string chargeId, DateTime expiry, bool isTrial)
        {
            if (string.IsNullOrWhiteSpace(shopDomain)) throw new ArgumentException("shopDomain is required", nameof(shopDomain));
            if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("planName is required", nameof(planName));
            if (expiry <= DateTime.UtcNow) throw new ArgumentException("expiry must be a future UTC date/time", nameof(expiry));

            var normalized = NormalizeDomain(shopDomain);

            try
            {
                // Use a simple find by normalized domain (case-insensitive).
                var existing = await _db.Licenses.FirstOrDefaultAsync(x => x.ShopDomain.ToLower() == normalized);

                if (existing != null)
                {
                    existing.PlanName = planName;
                    existing.ChargeId = chargeId ?? string.Empty;
                    existing.ExpiryDate = expiry;
                    existing.Status = isTrial ? "trial" : "active";
                    existing.IsActive = true;

                    _db.Licenses.Update(existing);
                    _logger.LogInformation("Updated license for shop {ShopDomain}: Plan={Plan}", shopDomain, planName);
                }
                else
                {
                    var newLicense = new License
                    {
                        ShopDomain = shopDomain.Trim(),
                        PlanName = planName,
                        ChargeId = chargeId ?? string.Empty,
                        ExpiryDate = expiry,
                        Status = isTrial ? "trial" : "active",
                        IsActive = true,
                        StartDate = DateTime.UtcNow
                    };

                    await _db.Licenses.AddAsync(newLicense);
                    _logger.LogInformation("Created license for shop {ShopDomain}: Plan={Plan}", shopDomain, planName);
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or update license for shop {ShopDomain}", shopDomain);
                return false;
            }
        }

        /// <summary>
        /// Deactivates the license for the specified shop domain (for example on uninstall or cancelled billing).
        /// </summary>
        /// <param name="shopDomain">Shop domain (myshopify domain).</param>
        /// <returns>True if a license was found and deactivated; false otherwise.</returns>
        public async Task<bool> DeactivateLicenseAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain)) return false;

            var normalized = NormalizeDomain(shopDomain);

            try
            {
                var license = await _db.Licenses.FirstOrDefaultAsync(l => l.ShopDomain.ToLower() == normalized);
                if (license == null) return false;

                license.IsActive = false;
                license.Status = "cancelled";
                license.ExpiryDate = DateTime.UtcNow;

                _db.Licenses.Update(license);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Deactivated license for shop {ShopDomain}", shopDomain);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate license for shop {ShopDomain}", shopDomain);
                return false;
            }
        }

        /// <summary>
        /// Normalizes a shop domain for case-insensitive comparison (lowercase, trimmed).
        /// </summary>
        private static string NormalizeDomain(string shopDomain) =>
            shopDomain.Trim().ToLowerInvariant();
    }
}
