using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services
{
    /// <summary>
    /// Manages subscription plans and plan change requests.
    /// </summary>
    public class PlanService : IPlanService
    {
        private readonly AppDbContext _db;
        private readonly ILicenseService _licenseService;
        private readonly IShopifyBillingService _billingService;
        private readonly ILogger<PlanService> _logger;

        public PlanService(
            AppDbContext db,
            ILicenseService licenseService,
            IShopifyBillingService billingService,
            ILogger<PlanService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PlanDto>> GetAllPlansAsync()
        {
            var plans = await _db.Plans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            return plans.Select(p => MapToDto(p));
        }

        /// <inheritdoc/>
        public async Task<PlanDto?> GetPlanByNameAsync(string planName)
        {
            if (string.IsNullOrWhiteSpace(planName)) return null;

            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == planName.ToLower() && p.IsActive);

            return plan != null ? MapToDto(plan) : null;
        }

        /// <inheritdoc/>
        public async Task<PlanDto?> GetCurrentPlanAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain)) return null;

            var license = await _licenseService.GetLicenseAsync(shopDomain);
            var planName = license?.PlanName ?? "Free";

            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == planName.ToLower());

            if (plan == null)
            {
                // Return Free plan if the plan in license doesn't exist
                plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Free");
            }

            return plan != null ? MapToDto(plan, isCurrentPlan: true) : null;
        }

        /// <inheritdoc/>
        public async Task<bool> CanAccessFeatureAsync(string shopDomain, string featureName)
        {
            var plan = await GetCurrentPlanAsync(shopDomain);
            if (plan == null) return false;

            return featureName.ToLower() switch
            {
                "whatsapp" => plan.HasWhatsApp,
                "email_campaigns" => plan.HasEmailCampaigns,
                "sms" => plan.HasSms,
                "advanced_reports" => plan.HasAdvancedReports,
                "api_access" => plan.HasApiAccess,
                _ => false
            };
        }

        /// <inheritdoc/>
        public async Task<bool> IsWithinLimitAsync(string shopDomain, string limitType, int currentCount)
        {
            var plan = await GetCurrentPlanAsync(shopDomain);
            if (plan == null) return false;

            var limit = limitType.ToLower() switch
            {
                "orders" => plan.OrderLimit,
                "products" => plan.ProductLimit,
                "customers" => plan.CustomerLimit,
                _ => -1
            };

            // -1 means unlimited
            return limit == -1 || currentCount < limit;
        }

        /// <inheritdoc/>
        public async Task<string?> RequestPlanChangeAsync(string shopDomain, string accessToken, string newPlanName)
        {
            if (string.IsNullOrWhiteSpace(shopDomain) || string.IsNullOrWhiteSpace(newPlanName))
                return null;

            try
            {
                var currentPlan = await GetCurrentPlanAsync(shopDomain);
                var newPlan = await GetPlanByNameAsync(newPlanName);

                if (currentPlan == null || newPlan == null)
                {
                    _logger.LogWarning("Plan change failed: current or new plan not found for {ShopDomain}", shopDomain);
                    return null;
                }

                if (currentPlan.Name == newPlan.Name)
                {
                    _logger.LogInformation("Shop {ShopDomain} already on plan {PlanName}", shopDomain, newPlanName);
                    return "already_on_plan";
                }

                var isUpgrade = newPlan.MonthlyPrice > currentPlan.MonthlyPrice;

                if (isUpgrade)
                {
                    // Upgrades go directly to Shopify billing
                    if (newPlan.MonthlyPrice > 0)
                    {
                        var confirmationUrl = await _billingService.CreateRecurringChargeAsync(
                            shopDomain,
                            accessToken,
                            newPlan.Name,
                            newPlan.MonthlyPrice,
                            newPlan.TrialDays);

                        _logger.LogInformation("Created billing charge for shop {ShopDomain} upgrading to {PlanName}", shopDomain, newPlanName);
                        return confirmationUrl;
                    }
                    else
                    {
                        // Free upgrade (shouldn't normally happen but handle gracefully)
                        await UpdateLicensePlanAsync(shopDomain, newPlanName);
                        return "upgraded";
                    }
                }
                else
                {
                    // Downgrades require admin approval
                    var existingRequest = await _db.PlanChangeRequests
                        .FirstOrDefaultAsync(r => r.ShopDomain.ToLower() == shopDomain.ToLower()
                            && r.Status == "pending");

                    if (existingRequest != null)
                    {
                        _logger.LogInformation("Shop {ShopDomain} already has a pending plan change request", shopDomain);
                        return "pending_request_exists";
                    }

                    var request = new PlanChangeRequest
                    {
                        ShopDomain = shopDomain,
                        CurrentPlanName = currentPlan.Name,
                        RequestedPlanName = newPlanName,
                        RequestType = "downgrade",
                        Status = "pending",
                        RequestedAt = DateTime.UtcNow
                    };

                    await _db.PlanChangeRequests.AddAsync(request);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Created downgrade request for shop {ShopDomain} from {CurrentPlan} to {NewPlan}",
                        shopDomain, currentPlan.Name, newPlanName);

                    return "pending";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process plan change for shop {ShopDomain}", shopDomain);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PlanChangeRequestDto>> GetPendingRequestsAsync()
        {
            var requests = await _db.PlanChangeRequests
                .AsNoTracking()
                .Where(r => r.Status == "pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapRequestToDto);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PlanChangeRequestDto>> GetRequestsForShopAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain))
                return Enumerable.Empty<PlanChangeRequestDto>();

            var normalized = shopDomain.Trim().ToLowerInvariant();

            var requests = await _db.PlanChangeRequests
                .AsNoTracking()
                .Where(r => r.ShopDomain.ToLower() == normalized)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapRequestToDto);
        }

        /// <inheritdoc/>
        public async Task<bool> ApproveRequestAsync(int requestId, string adminEmail, string? adminNotes)
        {
            try
            {
                var request = await _db.PlanChangeRequests.FindAsync(requestId);
                if (request == null || request.Status != "pending")
                {
                    _logger.LogWarning("Cannot approve request {RequestId}: not found or not pending", requestId);
                    return false;
                }

                // Update the license to the new plan
                await UpdateLicensePlanAsync(request.ShopDomain, request.RequestedPlanName);

                // Mark request as approved
                request.Status = "approved";
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedBy = adminEmail;
                request.AdminNotes = adminNotes;

                _db.PlanChangeRequests.Update(request);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Approved plan change request {RequestId} for shop {ShopDomain}",
                    requestId, request.ShopDomain);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve request {RequestId}", requestId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RejectRequestAsync(int requestId, string adminEmail, string? adminNotes)
        {
            try
            {
                var request = await _db.PlanChangeRequests.FindAsync(requestId);
                if (request == null || request.Status != "pending")
                {
                    _logger.LogWarning("Cannot reject request {RequestId}: not found or not pending", requestId);
                    return false;
                }

                request.Status = "rejected";
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedBy = adminEmail;
                request.AdminNotes = adminNotes;

                _db.PlanChangeRequests.Update(request);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Rejected plan change request {RequestId} for shop {ShopDomain}",
                    requestId, request.ShopDomain);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject request {RequestId}", requestId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task SeedDefaultPlansAsync()
        {
            var existingPlans = await _db.Plans.AnyAsync();
            if (existingPlans) return;

            var plans = new List<Plan>
            {
                new Plan
                {
                    Name = "Free",
                    Description = "Get started with basic features",
                    MonthlyPrice = 0,
                    OrderLimit = 100,
                    ProductLimit = 50,
                    CustomerLimit = 100,
                    HasWhatsApp = false,
                    HasEmailCampaigns = false,
                    HasSms = false,
                    HasAdvancedReports = false,
                    HasApiAccess = false,
                    SortOrder = 1,
                    IsActive = true,
                    TrialDays = 0
                },
                new Plan
                {
                    Name = "Premium",
                    Description = "For growing businesses with advanced marketing",
                    MonthlyPrice = 29,
                    OrderLimit = 1000,
                    ProductLimit = 500,
                    CustomerLimit = 1000,
                    HasWhatsApp = true,
                    HasEmailCampaigns = true,
                    HasSms = false,
                    HasAdvancedReports = true,
                    HasApiAccess = false,
                    SortOrder = 2,
                    IsActive = true,
                    TrialDays = 14
                },
                new Plan
                {
                    Name = "Enterprise",
                    Description = "Unlimited access for large-scale operations",
                    MonthlyPrice = 99,
                    OrderLimit = -1, // Unlimited
                    ProductLimit = -1,
                    CustomerLimit = -1,
                    HasWhatsApp = true,
                    HasEmailCampaigns = true,
                    HasSms = true,
                    HasAdvancedReports = true,
                    HasApiAccess = true,
                    SortOrder = 3,
                    IsActive = true,
                    TrialDays = 14
                }
            };

            await _db.Plans.AddRangeAsync(plans);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} default plans", plans.Count);
        }

        private async Task UpdateLicensePlanAsync(string shopDomain, string planName)
        {
            var expiry = DateTime.UtcNow.AddYears(10); // For free/downgrade, set far future expiry
            await _licenseService.CreateOrUpdateLicenseAsync(shopDomain, planName, string.Empty, expiry, false);
        }

        private static PlanDto MapToDto(Plan plan, bool isCurrentPlan = false)
        {
            return new PlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyPrice = plan.MonthlyPrice,
                OrderLimit = plan.OrderLimit,
                ProductLimit = plan.ProductLimit,
                CustomerLimit = plan.CustomerLimit,
                HasWhatsApp = plan.HasWhatsApp,
                HasEmailCampaigns = plan.HasEmailCampaigns,
                HasSms = plan.HasSms,
                HasAdvancedReports = plan.HasAdvancedReports,
                HasApiAccess = plan.HasApiAccess,
                TrialDays = plan.TrialDays,
                IsCurrentPlan = isCurrentPlan
            };
        }

        private static PlanChangeRequestDto MapRequestToDto(PlanChangeRequest request)
        {
            return new PlanChangeRequestDto
            {
                Id = request.Id,
                ShopDomain = request.ShopDomain,
                CurrentPlanName = request.CurrentPlanName,
                RequestedPlanName = request.RequestedPlanName,
                RequestType = request.RequestType,
                Status = request.Status,
                AdminNotes = request.AdminNotes,
                RequestedAt = request.RequestedAt,
                ProcessedAt = request.ProcessedAt,
                ProcessedBy = request.ProcessedBy
            };
        }
    }
}
