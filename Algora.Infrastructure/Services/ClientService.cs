using Algora.Application.DTOs.Admin;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services
{
    /// <summary>
    /// Manages client/shop information for admin purposes.
    /// </summary>
    public class ClientService : IClientService
    {
        private readonly AppDbContext _db;
        private readonly ILicenseService _licenseService;
        private readonly IPlanFeatureService _planFeatureService;
        private readonly ILogger<ClientService> _logger;

        public ClientService(
            AppDbContext db,
            ILicenseService licenseService,
            IPlanFeatureService planFeatureService,
            ILogger<ClientService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _planFeatureService = planFeatureService ?? throw new ArgumentNullException(nameof(planFeatureService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClientListResultDto> GetClientsAsync(ClientFilterDto filter)
        {
            var query = _db.Shops.AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.Domain.ToLower().Contains(term) ||
                    (s.ShopName != null && s.ShopName.ToLower().Contains(term)) ||
                    (s.Email != null && s.Email.ToLower().Contains(term)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var shops = await query
                .OrderByDescending(s => s.InstalledAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Get licenses for all shops
            var shopDomains = shops.Select(s => s.Domain).ToList();
            var licenses = await _db.Licenses
                .AsNoTracking()
                .Where(l => shopDomains.Contains(l.ShopDomain))
                .ToListAsync();

            var licenseDict = licenses.ToDictionary(l => l.ShopDomain.ToLower());

            // Apply plan filter if specified
            var clients = new List<ClientDto>();
            foreach (var shop in shops)
            {
                licenseDict.TryGetValue(shop.Domain.ToLower(), out var license);

                // Skip if plan filter doesn't match
                if (!string.IsNullOrWhiteSpace(filter.PlanName) &&
                    (license?.PlanName ?? "Free") != filter.PlanName)
                    continue;

                // Skip if license status filter doesn't match
                if (!string.IsNullOrWhiteSpace(filter.LicenseStatus) &&
                    (license?.Status ?? "none") != filter.LicenseStatus)
                    continue;

                clients.Add(MapToClientDto(shop, license));
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return new ClientListResultDto
            {
                Clients = clients,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<IEnumerable<ClientDto>> GetAllClientsAsync(bool activeOnly = false)
        {
            var query = _db.Shops.AsNoTracking();

            if (activeOnly)
                query = query.Where(s => s.IsActive);

            var shops = await query.OrderByDescending(s => s.InstalledAt).ToListAsync();

            var shopDomains = shops.Select(s => s.Domain).ToList();
            var licenses = await _db.Licenses
                .AsNoTracking()
                .Where(l => shopDomains.Contains(l.ShopDomain))
                .ToListAsync();

            var licenseDict = licenses.ToDictionary(l => l.ShopDomain.ToLower());

            return shops.Select(shop =>
            {
                licenseDict.TryGetValue(shop.Domain.ToLower(), out var license);
                return MapToClientDto(shop, license);
            });
        }

        public async Task<ClientDetailDto?> GetClientDetailAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain)) return null;

            var shop = await _db.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Domain.ToLower() == shopDomain.ToLower());

            if (shop == null) return null;

            var license = await _db.Licenses
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ShopDomain.ToLower() == shopDomain.ToLower());

            var planName = license?.PlanName ?? "Free";
            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == planName.ToLower());

            var features = await _planFeatureService.GetShopFeaturesAsync(shopDomain);

            return new ClientDetailDto
            {
                Id = shop.Id,
                Domain = shop.Domain,
                ShopName = shop.ShopName,
                Email = shop.Email,
                Country = shop.Country,
                Currency = shop.Currency,
                Timezone = shop.Timezone,
                PrimaryLocale = shop.PrimaryLocale,
                IsActive = shop.IsActive,
                InstalledAt = shop.InstalledAt,
                LastSyncedAt = shop.LastSyncedAt,
                UseCustomCredentials = shop.UseCustomCredentials,
                PlanName = planName,
                LicenseStatus = license?.Status,
                LicenseStartDate = license?.StartDate,
                LicenseExpiry = license?.ExpiryDate,
                HasActiveLicense = license?.IsActive ?? false,
                PlanPrice = plan?.MonthlyPrice,
                OrderLimit = plan?.OrderLimit,
                ProductLimit = plan?.ProductLimit,
                CustomerLimit = plan?.CustomerLimit,
                Features = features.ToList()
            };
        }

        public async Task<ClientDto?> GetClientByIdAsync(Guid shopId)
        {
            var shop = await _db.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == shopId);

            if (shop == null) return null;

            var license = await _db.Licenses
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ShopDomain.ToLower() == shop.Domain.ToLower());

            return MapToClientDto(shop, license);
        }

        public async Task<bool> UpdateClientPlanAsync(UpdateClientPlanDto dto, string adminEmail)
        {
            if (string.IsNullOrWhiteSpace(dto.ShopDomain) || string.IsNullOrWhiteSpace(dto.NewPlanName))
                return false;

            try
            {
                var plan = await _db.Plans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == dto.NewPlanName.ToLower());

                if (plan == null)
                {
                    _logger.LogWarning("Cannot update client plan: plan {PlanName} not found", dto.NewPlanName);
                    return false;
                }

                // Update or create license
                var expiry = DateTime.UtcNow.AddYears(10); // Admin override, no expiry
                await _licenseService.CreateOrUpdateLicenseAsync(
                    dto.ShopDomain,
                    dto.NewPlanName,
                    string.Empty,
                    expiry,
                    false);

                _logger.LogInformation("Admin {AdminEmail} updated client {ShopDomain} to plan {PlanName}. Notes: {Notes}",
                    adminEmail, dto.ShopDomain, dto.NewPlanName, dto.AdminNotes ?? "none");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update client plan for {ShopDomain}", dto.ShopDomain);
                return false;
            }
        }

        public async Task<bool> SetClientActiveStatusAsync(string shopDomain, bool isActive)
        {
            var shop = await _db.Shops
                .FirstOrDefaultAsync(s => s.Domain.ToLower() == shopDomain.ToLower());

            if (shop == null) return false;

            shop.IsActive = isActive;
            shop.UpdatedAt = DateTime.UtcNow;

            _db.Shops.Update(shop);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Set client {ShopDomain} active status to {IsActive}", shopDomain, isActive);

            return true;
        }

        public async Task<ClientStatsDto> GetClientStatsAsync()
        {
            var shops = await _db.Shops.AsNoTracking().ToListAsync();
            var licenses = await _db.Licenses.AsNoTracking().ToListAsync();

            var licenseDict = licenses.ToDictionary(l => l.ShopDomain.ToLower());

            var stats = new ClientStatsDto
            {
                TotalClients = shops.Count,
                ActiveClients = shops.Count(s => s.IsActive),
                InactiveClients = shops.Count(s => !s.IsActive),
                TrialClients = licenses.Count(l => l.Status == "trial"),
                PaidClients = licenses.Count(l => l.Status == "active" && l.PlanName != "Free"),
                ClientsByPlan = new Dictionary<string, int>()
            };

            // Count clients by plan
            foreach (var shop in shops)
            {
                var planName = licenseDict.TryGetValue(shop.Domain.ToLower(), out var lic)
                    ? lic.PlanName
                    : "Free";

                if (!stats.ClientsByPlan.ContainsKey(planName))
                    stats.ClientsByPlan[planName] = 0;

                stats.ClientsByPlan[planName]++;
            }

            return stats;
        }

        public async Task<IEnumerable<string>> GetActivePlanNamesAsync()
        {
            var planNames = await _db.Licenses
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Select(l => l.PlanName)
                .Distinct()
                .ToListAsync();

            // Always include Free even if no one is on it
            if (!planNames.Contains("Free"))
                planNames.Add("Free");

            return planNames.OrderBy(p => p);
        }

        private static ClientDto MapToClientDto(Domain.Entities.Shop shop, Domain.Entities.License? license)
        {
            return new ClientDto
            {
                Id = shop.Id,
                Domain = shop.Domain,
                ShopName = shop.ShopName,
                Email = shop.Email,
                Country = shop.Country,
                Currency = shop.Currency,
                IsActive = shop.IsActive,
                InstalledAt = shop.InstalledAt,
                LastSyncedAt = shop.LastSyncedAt,
                PlanName = license?.PlanName ?? "Free",
                LicenseStatus = license?.Status,
                LicenseExpiry = license?.ExpiryDate,
                HasActiveLicense = license?.IsActive ?? false
            };
        }
    }
}
