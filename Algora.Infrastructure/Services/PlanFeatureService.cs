using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services
{
    /// <summary>
    /// Manages plan features and feature assignments to plans.
    /// </summary>
    public class PlanFeatureService : IPlanFeatureService
    {
        private readonly AppDbContext _db;
        private readonly ILicenseService _licenseService;
        private readonly ILogger<PlanFeatureService> _logger;

        public PlanFeatureService(
            AppDbContext db,
            ILicenseService licenseService,
            ILogger<PlanFeatureService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ===== Feature CRUD =====

        public async Task<IEnumerable<PlanFeatureDto>> GetAllFeaturesAsync(bool activeOnly = false)
        {
            var query = _db.PlanFeatures.AsNoTracking();

            if (activeOnly)
                query = query.Where(f => f.IsActive);

            var features = await query
                .OrderBy(f => f.Category)
                .ThenBy(f => f.SortOrder)
                .ToListAsync();

            return features.Select(MapToDto);
        }

        public async Task<PlanFeatureDto?> GetFeatureByIdAsync(int featureId)
        {
            var feature = await _db.PlanFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == featureId);

            return feature != null ? MapToDto(feature) : null;
        }

        public async Task<PlanFeatureDto?> GetFeatureByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var feature = await _db.PlanFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Code.ToLower() == code.ToLower());

            return feature != null ? MapToDto(feature) : null;
        }

        public async Task<Dictionary<string, List<PlanFeatureDto>>> GetFeaturesByCategoryAsync(bool activeOnly = true)
        {
            var query = _db.PlanFeatures.AsNoTracking();

            if (activeOnly)
                query = query.Where(f => f.IsActive);

            var features = await query
                .OrderBy(f => f.SortOrder)
                .ToListAsync();

            return features
                .GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.Select(MapToDto).ToList());
        }

        public async Task<PlanFeatureDto> CreateFeatureAsync(CreatePlanFeatureDto dto)
        {
            var feature = new PlanFeature
            {
                Code = dto.Code.ToLower().Trim(),
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                IconClass = dto.IconClass,
                SortOrder = dto.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _db.PlanFeatures.AddAsync(feature);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created plan feature: {Code} - {Name}", feature.Code, feature.Name);

            return MapToDto(feature);
        }

        public async Task<PlanFeatureDto?> UpdateFeatureAsync(int featureId, UpdatePlanFeatureDto dto)
        {
            var feature = await _db.PlanFeatures.FindAsync(featureId);
            if (feature == null) return null;

            feature.Name = dto.Name;
            feature.Description = dto.Description;
            feature.Category = dto.Category;
            feature.IconClass = dto.IconClass;
            feature.SortOrder = dto.SortOrder;
            feature.IsActive = dto.IsActive;

            _db.PlanFeatures.Update(feature);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated plan feature: {Id} - {Name}", featureId, feature.Name);

            return MapToDto(feature);
        }

        public async Task<bool> DeleteFeatureAsync(int featureId)
        {
            var feature = await _db.PlanFeatures
                .Include(f => f.PlanAssignments)
                .FirstOrDefaultAsync(f => f.Id == featureId);

            if (feature == null) return false;

            // Remove all assignments first
            _db.PlanFeatureAssignments.RemoveRange(feature.PlanAssignments);
            _db.PlanFeatures.Remove(feature);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted plan feature: {Id} - {Code}", featureId, feature.Code);

            return true;
        }

        public async Task<bool> ToggleFeatureActiveAsync(int featureId)
        {
            var feature = await _db.PlanFeatures.FindAsync(featureId);
            if (feature == null) return false;

            feature.IsActive = !feature.IsActive;
            _db.PlanFeatures.Update(feature);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Toggled feature {Id} active status to {IsActive}", featureId, feature.IsActive);

            return true;
        }

        // ===== Feature Assignment =====

        public async Task<IEnumerable<PlanWithFeaturesDto>> GetAllPlansWithFeaturesAsync()
        {
            var plans = await _db.Plans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            var result = new List<PlanWithFeaturesDto>();

            foreach (var plan in plans)
            {
                var features = await GetFeaturesForPlanAsync(plan.Id);
                result.Add(new PlanWithFeaturesDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    MonthlyPrice = plan.MonthlyPrice,
                    OrderLimit = plan.OrderLimit,
                    ProductLimit = plan.ProductLimit,
                    CustomerLimit = plan.CustomerLimit,
                    IsActive = plan.IsActive,
                    Features = features.ToList()
                });
            }

            return result;
        }

        public async Task<PlanWithFeaturesDto?> GetPlanWithFeaturesAsync(int planId)
        {
            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return null;

            var features = await GetFeaturesForPlanAsync(planId);

            return new PlanWithFeaturesDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyPrice = plan.MonthlyPrice,
                OrderLimit = plan.OrderLimit,
                ProductLimit = plan.ProductLimit,
                CustomerLimit = plan.CustomerLimit,
                IsActive = plan.IsActive,
                Features = features.ToList()
            };
        }

        public async Task<IEnumerable<PlanFeatureDto>> GetFeaturesForPlanAsync(int planId)
        {
            var features = await _db.PlanFeatureAssignments
                .AsNoTracking()
                .Where(a => a.PlanId == planId)
                .Include(a => a.PlanFeature)
                .Where(a => a.PlanFeature.IsActive)
                .OrderBy(a => a.PlanFeature.Category)
                .ThenBy(a => a.PlanFeature.SortOrder)
                .Select(a => a.PlanFeature)
                .ToListAsync();

            return features.Select(MapToDto);
        }

        public async Task<bool> AssignFeatureToPlanAsync(AssignFeatureToPlanDto dto)
        {
            // Check if already assigned
            var exists = await _db.PlanFeatureAssignments
                .AnyAsync(a => a.PlanId == dto.PlanId && a.PlanFeatureId == dto.FeatureId);

            if (exists)
            {
                _logger.LogWarning("Feature {FeatureId} already assigned to plan {PlanId}", dto.FeatureId, dto.PlanId);
                return false;
            }

            var assignment = new PlanFeatureAssignment
            {
                PlanId = dto.PlanId,
                PlanFeatureId = dto.FeatureId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = dto.AssignedBy
            };

            await _db.PlanFeatureAssignments.AddAsync(assignment);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Assigned feature {FeatureId} to plan {PlanId} by {AssignedBy}",
                dto.FeatureId, dto.PlanId, dto.AssignedBy ?? "system");

            return true;
        }

        public async Task<bool> RemoveFeatureFromPlanAsync(int planId, int featureId)
        {
            var assignment = await _db.PlanFeatureAssignments
                .FirstOrDefaultAsync(a => a.PlanId == planId && a.PlanFeatureId == featureId);

            if (assignment == null) return false;

            _db.PlanFeatureAssignments.Remove(assignment);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Removed feature {FeatureId} from plan {PlanId}", featureId, planId);

            return true;
        }

        public async Task<bool> BulkAssignFeaturesAsync(BulkAssignFeaturesDto dto)
        {
            try
            {
                // Remove existing assignments for this plan
                var existingAssignments = await _db.PlanFeatureAssignments
                    .Where(a => a.PlanId == dto.PlanId)
                    .ToListAsync();

                _db.PlanFeatureAssignments.RemoveRange(existingAssignments);

                // Add new assignments
                var newAssignments = dto.FeatureIds.Select(featureId => new PlanFeatureAssignment
                {
                    PlanId = dto.PlanId,
                    PlanFeatureId = featureId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = dto.AssignedBy
                });

                await _db.PlanFeatureAssignments.AddRangeAsync(newAssignments);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Bulk assigned {Count} features to plan {PlanId} by {AssignedBy}",
                    dto.FeatureIds.Count, dto.PlanId, dto.AssignedBy ?? "system");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk assign features to plan {PlanId}", dto.PlanId);
                return false;
            }
        }

        // ===== Feature Access Check =====

        public async Task<bool> ShopHasFeatureAsync(string shopDomain, string featureCode)
        {
            if (string.IsNullOrWhiteSpace(shopDomain) || string.IsNullOrWhiteSpace(featureCode))
                return false;

            var license = await _licenseService.GetLicenseAsync(shopDomain);
            var planName = license?.PlanName ?? "Free";

            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == planName.ToLower());

            if (plan == null) return false;

            // Check if feature is assigned to this plan
            var hasFeature = await _db.PlanFeatureAssignments
                .AsNoTracking()
                .AnyAsync(a => a.PlanId == plan.Id &&
                              a.PlanFeature.Code.ToLower() == featureCode.ToLower() &&
                              a.PlanFeature.IsActive);

            return hasFeature;
        }

        public async Task<IEnumerable<string>> GetShopFeaturesAsync(string shopDomain)
        {
            if (string.IsNullOrWhiteSpace(shopDomain))
                return Enumerable.Empty<string>();

            var license = await _licenseService.GetLicenseAsync(shopDomain);
            var planName = license?.PlanName ?? "Free";

            var plan = await _db.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == planName.ToLower());

            if (plan == null)
                return Enumerable.Empty<string>();

            var featureCodes = await _db.PlanFeatureAssignments
                .AsNoTracking()
                .Where(a => a.PlanId == plan.Id && a.PlanFeature.IsActive)
                .Select(a => a.PlanFeature.Code)
                .ToListAsync();

            return featureCodes;
        }

        // ===== Seed Default Features =====

        public async Task SeedDefaultFeaturesAsync()
        {
            var existingFeatures = await _db.PlanFeatures.AnyAsync();
            if (existingFeatures) return;

            var features = new List<PlanFeature>
            {
                // Communication features
                new PlanFeature { Code = "whatsapp", Name = "WhatsApp Integration", Description = "Send messages via WhatsApp Business API", Category = "Communication", IconClass = "fab fa-whatsapp", SortOrder = 1 },
                new PlanFeature { Code = "email_campaigns", Name = "Email Campaigns", Description = "Create and send marketing email campaigns", Category = "Communication", IconClass = "fas fa-envelope", SortOrder = 2 },
                new PlanFeature { Code = "sms", Name = "SMS Messaging", Description = "Send SMS notifications and marketing messages", Category = "Communication", IconClass = "fas fa-sms", SortOrder = 3 },

                // AI features
                new PlanFeature { Code = "ai_descriptions", Name = "AI Product Descriptions", Description = "Generate product descriptions using AI", Category = "AI Tools", IconClass = "fas fa-robot", SortOrder = 1 },
                new PlanFeature { Code = "ai_seo", Name = "AI SEO Optimizer", Description = "AI-powered SEO meta tag generation", Category = "AI Tools", IconClass = "fas fa-search", SortOrder = 2 },
                new PlanFeature { Code = "ai_pricing", Name = "AI Pricing Optimizer", Description = "AI-powered pricing suggestions", Category = "AI Tools", IconClass = "fas fa-dollar-sign", SortOrder = 3 },
                new PlanFeature { Code = "ai_chatbot", Name = "AI Customer Chatbot", Description = "Automated customer support chatbot", Category = "AI Tools", IconClass = "fas fa-comments", SortOrder = 4 },
                new PlanFeature { Code = "ai_alt_text", Name = "AI Alt-Text Generator", Description = "Generate alt text for product images", Category = "AI Tools", IconClass = "fas fa-image", SortOrder = 5 },

                // Analytics features
                new PlanFeature { Code = "advanced_reports", Name = "Advanced Reports", Description = "Access to detailed analytics and reports", Category = "Analytics", IconClass = "fas fa-chart-bar", SortOrder = 1 },
                new PlanFeature { Code = "inventory_predictions", Name = "Inventory Predictions", Description = "AI-based inventory forecasting", Category = "Analytics", IconClass = "fas fa-chart-line", SortOrder = 2 },

                // Operations features
                new PlanFeature { Code = "purchase_orders", Name = "Purchase Orders", Description = "Create and manage purchase orders", Category = "Operations", IconClass = "fas fa-file-invoice", SortOrder = 1 },
                new PlanFeature { Code = "supplier_management", Name = "Supplier Management", Description = "Manage suppliers and vendor relationships", Category = "Operations", IconClass = "fas fa-truck", SortOrder = 2 },
                new PlanFeature { Code = "label_designer", Name = "Label Designer", Description = "Design and print product labels", Category = "Operations", IconClass = "fas fa-tags", SortOrder = 3 },
                new PlanFeature { Code = "barcode_generator", Name = "Barcode Generator", Description = "Generate barcodes for products", Category = "Operations", IconClass = "fas fa-barcode", SortOrder = 4 },

                // Customer Hub features
                new PlanFeature { Code = "unified_inbox", Name = "Unified Inbox", Description = "Manage all customer messages in one place", Category = "Customer Hub", IconClass = "fas fa-inbox", SortOrder = 1 },
                new PlanFeature { Code = "loyalty_program", Name = "Loyalty Program", Description = "Customer loyalty points and rewards", Category = "Customer Hub", IconClass = "fas fa-gift", SortOrder = 2 },
                new PlanFeature { Code = "exchanges", Name = "Exchange Management", Description = "Handle product exchanges", Category = "Customer Hub", IconClass = "fas fa-exchange-alt", SortOrder = 3 },

                // API & Integration features
                new PlanFeature { Code = "api_access", Name = "API Access", Description = "Access to REST API for integrations", Category = "Integrations", IconClass = "fas fa-code", SortOrder = 1 },
                new PlanFeature { Code = "webhooks", Name = "Custom Webhooks", Description = "Configure custom webhook endpoints", Category = "Integrations", IconClass = "fas fa-plug", SortOrder = 2 },

                // Upsell features
                new PlanFeature { Code = "upsell_offers", Name = "Upsell Offers", Description = "Create upsell and cross-sell offers", Category = "Marketing", IconClass = "fas fa-arrow-up", SortOrder = 1 },
                new PlanFeature { Code = "ab_testing", Name = "A/B Testing", Description = "Run experiments on offers", Category = "Marketing", IconClass = "fas fa-flask", SortOrder = 2 },
                new PlanFeature { Code = "abandoned_cart", Name = "Abandoned Cart Recovery", Description = "Recover abandoned carts with automation", Category = "Marketing", IconClass = "fas fa-shopping-cart", SortOrder = 3 }
            };

            await _db.PlanFeatures.AddRangeAsync(features);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} default plan features", features.Count);

            // Assign features to default plans
            await AssignDefaultFeaturesToPlansAsync();
        }

        private async Task AssignDefaultFeaturesToPlansAsync()
        {
            var plans = await _db.Plans.ToListAsync();
            var features = await _db.PlanFeatures.ToListAsync();

            var featuresByCode = features.ToDictionary(f => f.Code, f => f.Id);

            foreach (var plan in plans)
            {
                var featureCodes = plan.Name switch
                {
                    "Free" => new[] { "ai_descriptions" }, // Minimal features
                    "Basic" => new[] { "email_campaigns", "ai_descriptions", "ai_seo", "upsell_offers", "abandoned_cart" },
                    "Premium" => new[] { "whatsapp", "email_campaigns", "sms", "ai_descriptions", "ai_seo", "ai_pricing", "ai_alt_text",
                                        "advanced_reports", "upsell_offers", "ab_testing", "abandoned_cart", "unified_inbox" },
                    "Enterprise" => features.Select(f => f.Code).ToArray(), // All features
                    _ => Array.Empty<string>()
                };

                foreach (var code in featureCodes)
                {
                    if (featuresByCode.TryGetValue(code, out var featureId))
                    {
                        var assignment = new PlanFeatureAssignment
                        {
                            PlanId = plan.Id,
                            PlanFeatureId = featureId,
                            AssignedAt = DateTime.UtcNow,
                            AssignedBy = "system"
                        };
                        await _db.PlanFeatureAssignments.AddAsync(assignment);
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Assigned default features to plans");
        }

        private static PlanFeatureDto MapToDto(PlanFeature feature)
        {
            return new PlanFeatureDto
            {
                Id = feature.Id,
                Code = feature.Code,
                Name = feature.Name,
                Description = feature.Description,
                Category = feature.Category,
                IconClass = feature.IconClass,
                SortOrder = feature.SortOrder,
                IsActive = feature.IsActive,
                CreatedAt = feature.CreatedAt
            };
        }
    }
}
