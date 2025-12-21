namespace Algora.Domain.Entities;

/// <summary>
/// Represents an A/B test experiment for upsell offers.
/// Tracks variants, traffic allocation, and statistical analysis.
/// </summary>
public class UpsellExperiment
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this experiment belongs to.
    /// </summary>
    public string ShopDomain { get; set; } = string.Empty;

    // Experiment configuration
    /// <summary>
    /// Name of the experiment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what is being tested.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status: draft, running, paused, completed, winner_selected.
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Metric being optimized: conversion_rate, revenue_per_view, click_rate.
    /// </summary>
    public string PrimaryMetric { get; set; } = "conversion_rate";

    // Traffic allocation (must sum to 100)
    /// <summary>
    /// Percentage of traffic allocated to control (0-100).
    /// </summary>
    public int ControlTrafficPercent { get; set; } = 50;

    /// <summary>
    /// Percentage of traffic allocated to variant A (0-100).
    /// </summary>
    public int VariantATrafficPercent { get; set; } = 50;

    /// <summary>
    /// Percentage of traffic allocated to variant B (0-100), optional.
    /// </summary>
    public int? VariantBTrafficPercent { get; set; }

    // Statistical settings
    /// <summary>
    /// Minimum detectable effect (e.g., 0.05 for 5% MDE).
    /// </summary>
    public decimal MinimumDetectableEffect { get; set; } = 0.05m;

    /// <summary>
    /// Significance level (e.g., 0.05 for 95% confidence).
    /// </summary>
    public decimal SignificanceLevel { get; set; } = 0.05m;

    /// <summary>
    /// Statistical power (e.g., 0.80 for 80% power).
    /// </summary>
    public decimal StatisticalPower { get; set; } = 0.80m;

    /// <summary>
    /// Calculated required sample size per variant.
    /// </summary>
    public int? CalculatedSampleSize { get; set; }

    // Current statistics - Control
    /// <summary>
    /// Number of impressions for control.
    /// </summary>
    public int ControlImpressions { get; set; }

    /// <summary>
    /// Number of clicks for control.
    /// </summary>
    public int ControlClicks { get; set; }

    /// <summary>
    /// Number of conversions for control.
    /// </summary>
    public int ControlConversions { get; set; }

    /// <summary>
    /// Total revenue from control conversions.
    /// </summary>
    public decimal ControlRevenue { get; set; }

    // Current statistics - Variant A
    /// <summary>
    /// Number of impressions for variant A.
    /// </summary>
    public int VariantAImpressions { get; set; }

    /// <summary>
    /// Number of clicks for variant A.
    /// </summary>
    public int VariantAClicks { get; set; }

    /// <summary>
    /// Number of conversions for variant A.
    /// </summary>
    public int VariantAConversions { get; set; }

    /// <summary>
    /// Total revenue from variant A conversions.
    /// </summary>
    public decimal VariantARevenue { get; set; }

    // Current statistics - Variant B (optional)
    /// <summary>
    /// Number of impressions for variant B.
    /// </summary>
    public int? VariantBImpressions { get; set; }

    /// <summary>
    /// Number of clicks for variant B.
    /// </summary>
    public int? VariantBClicks { get; set; }

    /// <summary>
    /// Number of conversions for variant B.
    /// </summary>
    public int? VariantBConversions { get; set; }

    /// <summary>
    /// Total revenue from variant B conversions.
    /// </summary>
    public decimal? VariantBRevenue { get; set; }

    // Calculated rates
    /// <summary>
    /// Control conversion rate (conversions / impressions).
    /// </summary>
    public decimal? ControlConversionRate { get; set; }

    /// <summary>
    /// Variant A conversion rate.
    /// </summary>
    public decimal? VariantAConversionRate { get; set; }

    /// <summary>
    /// Variant B conversion rate.
    /// </summary>
    public decimal? VariantBConversionRate { get; set; }

    // Confidence intervals (stored as JSON: {"lower": 0.05, "upper": 0.08})
    /// <summary>
    /// Control confidence interval as JSON.
    /// </summary>
    public string? ControlConfidenceInterval { get; set; }

    /// <summary>
    /// Variant A confidence interval as JSON.
    /// </summary>
    public string? VariantAConfidenceInterval { get; set; }

    /// <summary>
    /// Variant B confidence interval as JSON.
    /// </summary>
    public string? VariantBConfidenceInterval { get; set; }

    // Statistical significance
    /// <summary>
    /// P-value comparing variants to control.
    /// </summary>
    public decimal? PValueVsControl { get; set; }

    /// <summary>
    /// Whether the result is statistically significant.
    /// </summary>
    public bool IsStatisticallySignificant { get; set; }

    /// <summary>
    /// The winning variant: control, variant_a, variant_b.
    /// </summary>
    public string? WinningVariant { get; set; }

    /// <summary>
    /// Percentage lift of winner over control.
    /// </summary>
    public decimal? WinningLift { get; set; }

    // Auto-winner selection
    /// <summary>
    /// Whether to automatically select winner when significant.
    /// </summary>
    public bool AutoSelectWinner { get; set; } = true;

    /// <summary>
    /// When the winner was selected.
    /// </summary>
    public DateTime? WinnerSelectedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Offers associated with this experiment.
    /// </summary>
    public ICollection<UpsellOffer> Offers { get; set; } = new List<UpsellOffer>();

    /// <summary>
    /// Conversion events for this experiment.
    /// </summary>
    public ICollection<UpsellConversion> Conversions { get; set; } = new List<UpsellConversion>();

    // Timestamps
    /// <summary>
    /// When the experiment was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the experiment ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
