using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing A/B test experiments for upsell offers.
/// </summary>
public interface IUpsellExperimentService
{
    /// <summary>
    /// Create a new A/B test experiment.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="dto">Experiment configuration</param>
    /// <returns>Created experiment</returns>
    Task<UpsellExperimentDto> CreateExperimentAsync(string shopDomain, CreateExperimentDto dto);

    /// <summary>
    /// Start an experiment (change status from draft to running).
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Updated experiment</returns>
    Task<UpsellExperimentDto> StartExperimentAsync(int experimentId);

    /// <summary>
    /// Pause a running experiment.
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Updated experiment</returns>
    Task<UpsellExperimentDto> PauseExperimentAsync(int experimentId);

    /// <summary>
    /// End an experiment and optionally declare a winner.
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="winningVariant">Optional winning variant to declare</param>
    /// <returns>Updated experiment</returns>
    Task<UpsellExperimentDto> EndExperimentAsync(int experimentId, string? winningVariant = null);

    /// <summary>
    /// Get experiment details with current statistics.
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Experiment details or null if not found</returns>
    Task<UpsellExperimentDto?> GetExperimentAsync(int experimentId);

    /// <summary>
    /// Get all experiments for a shop.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="status">Filter by status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of experiments</returns>
    Task<PaginatedResult<UpsellExperimentDto>> GetExperimentsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get experiment summary for dashboard.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <returns>Summary statistics</returns>
    Task<ExperimentSummaryDto> GetExperimentSummaryAsync(string shopDomain);

    /// <summary>
    /// Assign a session to a variant in an experiment (for consistent user experience).
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Assigned variant name</returns>
    Task<string> AssignVariantAsync(int experimentId, string sessionId);

    /// <summary>
    /// Record an impression event.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <param name="offerId">Offer ID that was shown</param>
    /// <param name="platformOrderId">Source order ID</param>
    /// <param name="sessionId">Session ID</param>
    /// <param name="experimentId">Optional experiment ID</param>
    /// <param name="variant">Optional variant name</param>
    /// <returns>Conversion tracking ID</returns>
    Task<int> RecordImpressionAsync(
        string shopDomain,
        int offerId,
        long platformOrderId,
        string sessionId,
        int? experimentId = null,
        string? variant = null);

    /// <summary>
    /// Record a click event.
    /// </summary>
    /// <param name="conversionId">Conversion tracking ID</param>
    Task RecordClickAsync(int conversionId);

    /// <summary>
    /// Record a conversion event (order placed via upsell).
    /// </summary>
    /// <param name="conversionId">Conversion tracking ID</param>
    /// <param name="conversionOrderId">Platform order ID of the conversion</param>
    /// <param name="revenue">Revenue from conversion</param>
    /// <param name="quantity">Quantity purchased</param>
    Task RecordConversionAsync(
        int conversionId,
        long conversionOrderId,
        decimal revenue,
        int quantity);

    /// <summary>
    /// Calculate required sample size for experiment design.
    /// </summary>
    /// <param name="baselineConversionRate">Current baseline conversion rate</param>
    /// <param name="minimumDetectableEffect">Minimum effect to detect (e.g., 0.05 for 5%)</param>
    /// <param name="significanceLevel">Statistical significance level (e.g., 0.05 for 95%)</param>
    /// <param name="power">Statistical power (e.g., 0.80 for 80%)</param>
    /// <returns>Sample size calculation results</returns>
    SampleSizeCalculationDto CalculateSampleSize(
        decimal baselineConversionRate,
        decimal minimumDetectableEffect,
        decimal significanceLevel = 0.05m,
        decimal power = 0.80m);

    /// <summary>
    /// Recalculate statistical significance for an experiment.
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Updated experiment with recalculated statistics</returns>
    Task<UpsellExperimentDto> RecalculateStatisticsAsync(int experimentId);

    /// <summary>
    /// Check experiments for auto-winner selection.
    /// </summary>
    /// <param name="shopDomain">The shop domain</param>
    /// <returns>Number of experiments with winners selected</returns>
    Task<int> ProcessAutoWinnerSelectionAsync(string shopDomain);
}
