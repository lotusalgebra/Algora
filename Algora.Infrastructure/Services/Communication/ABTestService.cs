using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Service for A/B testing automation emails.
/// </summary>
public class ABTestService : IABTestService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ABTestService> _logger;
    private readonly Random _random = new();

    private const decimal SignificanceThreshold = 0.95m; // 95% confidence

    public ABTestService(AppDbContext db, ILogger<ABTestService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== VARIANT MANAGEMENT ====================

    public async Task<List<ABTestVariantDto>> GetVariantsAsync(int automationId, int? stepId = null)
    {
        var query = _db.ABTestVariants.Where(v => v.AutomationId == automationId);

        if (stepId.HasValue)
            query = query.Where(v => v.StepId == stepId);

        var variants = await query
            .OrderBy(v => v.IsControl ? 0 : 1)
            .ThenBy(v => v.VariantName)
            .ToListAsync();

        return variants.Select(MapToDto).ToList();
    }

    public async Task<ABTestVariantDto?> CreateVariantAsync(CreateABTestVariantDto dto)
    {
        var variant = new ABTestVariant
        {
            AutomationId = dto.AutomationId,
            StepId = dto.StepId,
            VariantName = dto.VariantName,
            Subject = dto.Subject,
            Body = dto.Body,
            Weight = dto.Weight,
            IsControl = dto.IsControl,
            CreatedAt = DateTime.UtcNow
        };

        _db.ABTestVariants.Add(variant);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created A/B test variant {VariantName} for automation {AutomationId}",
            dto.VariantName, dto.AutomationId);

        return MapToDto(variant);
    }

    public async Task<bool> UpdateVariantAsync(int variantId, CreateABTestVariantDto dto)
    {
        var variant = await _db.ABTestVariants.FindAsync(variantId);
        if (variant == null)
            return false;

        variant.VariantName = dto.VariantName;
        variant.Subject = dto.Subject;
        variant.Body = dto.Body;
        variant.Weight = dto.Weight;
        variant.IsControl = dto.IsControl;
        variant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteVariantAsync(int variantId)
    {
        var variant = await _db.ABTestVariants.FindAsync(variantId);
        if (variant == null)
            return false;

        _db.ABTestVariants.Remove(variant);
        await _db.SaveChangesAsync();
        return true;
    }

    // ==================== VARIANT ASSIGNMENT ====================

    public async Task<ABTestVariantDto?> AssignVariantAsync(int enrollmentId, int automationId, int? stepId = null)
    {
        // Get all variants for this automation/step
        var variants = await _db.ABTestVariants
            .Where(v => v.AutomationId == automationId && v.StepId == stepId)
            .ToListAsync();

        if (!variants.Any())
            return null;

        // Check if already assigned
        var existingResult = await _db.ABTestResults
            .Include(r => r.Variant)
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.Variant.StepId == stepId);

        if (existingResult != null)
            return MapToDto(existingResult.Variant);

        // Weighted random selection
        var totalWeight = variants.Sum(v => v.Weight);
        var randomValue = _random.Next(totalWeight);
        var cumulativeWeight = 0;

        ABTestVariant? selectedVariant = null;
        foreach (var variant in variants)
        {
            cumulativeWeight += variant.Weight;
            if (randomValue < cumulativeWeight)
            {
                selectedVariant = variant;
                break;
            }
        }

        selectedVariant ??= variants.First();

        // Record assignment
        var result = new ABTestResult
        {
            EnrollmentId = enrollmentId,
            VariantId = selectedVariant.Id,
            AssignedAt = DateTime.UtcNow
        };

        _db.ABTestResults.Add(result);

        // Increment impressions
        selectedVariant.Impressions++;
        selectedVariant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogDebug("Assigned variant {VariantName} to enrollment {EnrollmentId}",
            selectedVariant.VariantName, enrollmentId);

        return MapToDto(selectedVariant);
    }

    public async Task<ABTestVariantDto?> GetAssignedVariantAsync(int enrollmentId, int? stepId = null)
    {
        var query = _db.ABTestResults
            .Include(r => r.Variant)
            .Where(r => r.EnrollmentId == enrollmentId);

        if (stepId.HasValue)
            query = query.Where(r => r.Variant.StepId == stepId);

        var result = await query.FirstOrDefaultAsync();
        return result != null ? MapToDto(result.Variant) : null;
    }

    // ==================== RESULT TRACKING ====================

    public async Task RecordOpenAsync(int enrollmentId, int variantId)
    {
        var result = await _db.ABTestResults
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.VariantId == variantId);

        if (result != null && !result.Opened)
        {
            result.Opened = true;
            result.OpenedAt = DateTime.UtcNow;

            var variant = await _db.ABTestVariants.FindAsync(variantId);
            if (variant != null)
            {
                variant.Opens++;
                variant.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task RecordClickAsync(int enrollmentId, int variantId)
    {
        var result = await _db.ABTestResults
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.VariantId == variantId);

        if (result != null && !result.Clicked)
        {
            result.Clicked = true;
            result.ClickedAt = DateTime.UtcNow;

            var variant = await _db.ABTestVariants.FindAsync(variantId);
            if (variant != null)
            {
                variant.Clicks++;
                variant.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task RecordConversionAsync(int enrollmentId, int variantId, decimal conversionValue)
    {
        var result = await _db.ABTestResults
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.VariantId == variantId);

        if (result != null && !result.Converted)
        {
            result.Converted = true;
            result.ConvertedAt = DateTime.UtcNow;
            result.ConversionValue = conversionValue;

            var variant = await _db.ABTestVariants.FindAsync(variantId);
            if (variant != null)
            {
                variant.Conversions++;
                variant.Revenue += conversionValue;
                variant.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }

    // ==================== STATISTICS ====================

    public async Task<List<ABTestStatisticsDto>> GetStatisticsAsync(int automationId, int? stepId = null)
    {
        var variants = await _db.ABTestVariants
            .Where(v => v.AutomationId == automationId && v.StepId == stepId)
            .ToListAsync();

        if (!variants.Any())
            return new List<ABTestStatisticsDto>();

        var control = variants.FirstOrDefault(v => v.IsControl);
        var controlConversionRate = control != null && control.Impressions > 0
            ? (decimal)control.Conversions / control.Impressions
            : 0;

        var stats = new List<ABTestStatisticsDto>();

        foreach (var variant in variants)
        {
            var conversionRate = variant.Impressions > 0
                ? (decimal)variant.Conversions / variant.Impressions
                : 0;

            var conversionRateChange = controlConversionRate > 0
                ? ((conversionRate - controlConversionRate) / controlConversionRate) * 100
                : 0;

            var significance = control != null && !variant.IsControl
                ? await CalculateStatisticalSignificanceAsync(control.Id, variant.Id)
                : 0;

            stats.Add(new ABTestStatisticsDto(
                VariantId: variant.Id,
                VariantName: variant.VariantName,
                IsControl: variant.IsControl,
                SampleSize: variant.Impressions,
                ConversionRate: conversionRate * 100,
                ConversionRateChange: conversionRateChange,
                StatisticalSignificance: significance,
                IsSignificant: significance >= SignificanceThreshold,
                Revenue: variant.Revenue,
                RevenuePerRecipient: variant.Impressions > 0 ? variant.Revenue / variant.Impressions : 0
            ));
        }

        return stats.OrderBy(s => s.IsControl ? 0 : 1).ThenBy(s => s.VariantName).ToList();
    }

    public async Task<decimal> CalculateStatisticalSignificanceAsync(int controlVariantId, int testVariantId)
    {
        var control = await _db.ABTestVariants.FindAsync(controlVariantId);
        var test = await _db.ABTestVariants.FindAsync(testVariantId);

        if (control == null || test == null)
            return 0;

        if (control.Impressions < 100 || test.Impressions < 100)
            return 0; // Not enough data

        // Calculate using z-test for proportions
        var p1 = (double)control.Conversions / control.Impressions;
        var p2 = (double)test.Conversions / test.Impressions;
        var n1 = control.Impressions;
        var n2 = test.Impressions;

        var pooledP = (double)(control.Conversions + test.Conversions) / (n1 + n2);
        var standardError = Math.Sqrt(pooledP * (1 - pooledP) * (1.0 / n1 + 1.0 / n2));

        if (standardError == 0)
            return 0;

        var zScore = Math.Abs(p1 - p2) / standardError;

        // Convert z-score to confidence level (approximate)
        // Using standard normal distribution
        var confidence = NormalCDF(zScore) * 2 - 1;

        return (decimal)Math.Min(confidence, 1.0);
    }

    private static double NormalCDF(double z)
    {
        // Approximation of the cumulative normal distribution function
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var sign = z < 0 ? -1 : 1;
        z = Math.Abs(z) / Math.Sqrt(2);

        var t = 1.0 / (1.0 + p * z);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-z * z);

        return 0.5 * (1.0 + sign * y);
    }

    public async Task<bool> IsTestSignificantAsync(int automationId, int? stepId = null)
    {
        var stats = await GetStatisticsAsync(automationId, stepId);
        return stats.Any(s => !s.IsControl && s.IsSignificant);
    }

    public async Task<ABTestVariantDto?> GetWinningVariantAsync(int automationId, int? stepId = null)
    {
        var stats = await GetStatisticsAsync(automationId, stepId);

        var significantVariants = stats
            .Where(s => !s.IsControl && s.IsSignificant && s.ConversionRateChange > 0)
            .OrderByDescending(s => s.ConversionRate)
            .ToList();

        if (!significantVariants.Any())
            return null;

        var winner = significantVariants.First();
        var variant = await _db.ABTestVariants.FindAsync(winner.VariantId);

        return variant != null ? MapToDto(variant) : null;
    }

    // ==================== TEST MANAGEMENT ====================

    public async Task<bool> EnableTestingAsync(int stepId)
    {
        var step = await _db.EmailAutomationSteps.FindAsync(stepId);
        if (step == null)
            return false;

        step.IsABTestEnabled = true;
        step.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisableTestingAsync(int stepId)
    {
        var step = await _db.EmailAutomationSteps.FindAsync(stepId);
        if (step == null)
            return false;

        step.IsABTestEnabled = false;
        step.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApplyWinnerAsync(int automationId, int? stepId = null)
    {
        var winner = await GetWinningVariantAsync(automationId, stepId);
        if (winner == null)
            return false;

        if (stepId.HasValue)
        {
            var step = await _db.EmailAutomationSteps.FindAsync(stepId.Value);
            if (step != null)
            {
                step.Subject = winner.Subject;
                step.Body = winner.Body;
                step.IsABTestEnabled = false;
                step.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Remove all variants except the winner
        var variantsToRemove = await _db.ABTestVariants
            .Where(v => v.AutomationId == automationId && v.StepId == stepId && v.Id != winner.Id)
            .ToListAsync();

        _db.ABTestVariants.RemoveRange(variantsToRemove);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Applied winning variant {VariantName} for automation {AutomationId}",
            winner.VariantName, automationId);

        return true;
    }

    private static ABTestVariantDto MapToDto(ABTestVariant v)
    {
        return new ABTestVariantDto(
            Id: v.Id,
            AutomationId: v.AutomationId,
            StepId: v.StepId,
            VariantName: v.VariantName,
            Subject: v.Subject,
            Body: v.Body,
            Weight: v.Weight,
            IsControl: v.IsControl,
            Impressions: v.Impressions,
            Opens: v.Opens,
            Clicks: v.Clicks,
            Conversions: v.Conversions,
            Revenue: v.Revenue,
            OpenRate: v.OpenRate,
            ClickRate: v.ClickRate,
            ConversionRate: v.ConversionRate
        );
    }
}
