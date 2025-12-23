using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for A/B testing automation emails.
/// </summary>
public interface IABTestService
{
    // ==================== VARIANT MANAGEMENT ====================

    /// <summary>
    /// Get all variants for an automation or step.
    /// </summary>
    Task<List<ABTestVariantDto>> GetVariantsAsync(int automationId, int? stepId = null);

    /// <summary>
    /// Create a new A/B test variant.
    /// </summary>
    Task<ABTestVariantDto?> CreateVariantAsync(CreateABTestVariantDto dto);

    /// <summary>
    /// Update a variant.
    /// </summary>
    Task<bool> UpdateVariantAsync(int variantId, CreateABTestVariantDto dto);

    /// <summary>
    /// Delete a variant.
    /// </summary>
    Task<bool> DeleteVariantAsync(int variantId);

    // ==================== VARIANT ASSIGNMENT ====================

    /// <summary>
    /// Assign a variant to an enrollment based on weighted distribution.
    /// </summary>
    Task<ABTestVariantDto?> AssignVariantAsync(int enrollmentId, int automationId, int? stepId = null);

    /// <summary>
    /// Get the assigned variant for an enrollment.
    /// </summary>
    Task<ABTestVariantDto?> GetAssignedVariantAsync(int enrollmentId, int? stepId = null);

    // ==================== RESULT TRACKING ====================

    /// <summary>
    /// Record that a variant email was opened.
    /// </summary>
    Task RecordOpenAsync(int enrollmentId, int variantId);

    /// <summary>
    /// Record that a variant email was clicked.
    /// </summary>
    Task RecordClickAsync(int enrollmentId, int variantId);

    /// <summary>
    /// Record a conversion for a variant.
    /// </summary>
    Task RecordConversionAsync(int enrollmentId, int variantId, decimal conversionValue);

    // ==================== STATISTICS ====================

    /// <summary>
    /// Get A/B test statistics for all variants of an automation.
    /// </summary>
    Task<List<ABTestStatisticsDto>> GetStatisticsAsync(int automationId, int? stepId = null);

    /// <summary>
    /// Calculate statistical significance between control and variant.
    /// Uses chi-squared test or z-test for proportions.
    /// </summary>
    Task<decimal> CalculateStatisticalSignificanceAsync(int controlVariantId, int testVariantId);

    /// <summary>
    /// Determine if a test has reached statistical significance.
    /// Typically at 95% confidence level.
    /// </summary>
    Task<bool> IsTestSignificantAsync(int automationId, int? stepId = null);

    /// <summary>
    /// Get the winning variant (if test is significant).
    /// </summary>
    Task<ABTestVariantDto?> GetWinningVariantAsync(int automationId, int? stepId = null);

    // ==================== TEST MANAGEMENT ====================

    /// <summary>
    /// Enable A/B testing for a step.
    /// </summary>
    Task<bool> EnableTestingAsync(int stepId);

    /// <summary>
    /// Disable A/B testing for a step.
    /// </summary>
    Task<bool> DisableTestingAsync(int stepId);

    /// <summary>
    /// Apply winning variant as the default content (end test).
    /// </summary>
    Task<bool> ApplyWinnerAsync(int automationId, int? stepId = null);
}
