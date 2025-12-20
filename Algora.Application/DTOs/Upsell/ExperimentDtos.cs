namespace Algora.Application.DTOs.Upsell;

/// <summary>
/// DTO for upsell experiment details.
/// </summary>
public record UpsellExperimentDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = "draft";
    public string PrimaryMetric { get; init; } = "conversion_rate";

    // Traffic allocation
    public int TrafficPercentage { get; init; } = 100;
    public int ControlTrafficPercent { get; init; }
    public int VariantATrafficPercent { get; init; }
    public int? VariantBTrafficPercent { get; init; }

    // Variants
    public int VariantCount { get; init; }
    public List<ExperimentVariantResultDto> VariantResults { get; init; } = new();

    // Sample size
    public int MinSampleSize { get; init; }
    public int? CalculatedSampleSize { get; init; }
    public int CurrentSampleSize { get; init; }
    public decimal SampleSizeProgress { get; init; }

    // Totals
    public int TotalImpressions { get; init; }
    public int TotalConversions { get; init; }
    public decimal TotalRevenue { get; init; }

    // Statistical analysis
    public decimal SignificanceLevel { get; init; } = 0.05m;
    public bool AutoSelectWinner { get; init; } = true;
    public bool IsStatisticallySignificant { get; init; }
    public string? WinningVariant { get; init; }
    public decimal? WinningLift { get; init; }
    public decimal? PValue { get; init; }

    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for experiment variant results.
/// </summary>
public record ExperimentVariantResultDto
{
    public string VariantName { get; init; } = string.Empty;
    public int OfferId { get; init; }
    public string? OfferTitle { get; init; }
    public int Impressions { get; init; }
    public int Clicks { get; init; }
    public int Conversions { get; init; }
    public decimal Revenue { get; init; }

    public decimal ClickRate { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal RevenuePerView { get; init; }

    // Confidence interval
    public decimal ConfidenceIntervalLower { get; init; }
    public decimal ConfidenceIntervalUpper { get; init; }

    // Comparison to control
    public decimal? LiftVsControl { get; init; }
    public bool IsWinner { get; init; }
}

/// <summary>
/// DTO for creating a new experiment.
/// </summary>
public record CreateExperimentDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string PrimaryMetric { get; init; } = "conversion_rate";

    public List<int> OfferIds { get; set; } = new();
    public int TrafficPercentage { get; init; } = 100;

    public int ControlTrafficPercent { get; init; } = 50;
    public int VariantATrafficPercent { get; init; } = 50;
    public int? VariantBTrafficPercent { get; init; }

    public int MinSampleSize { get; init; } = 100;
    public decimal MinimumDetectableEffect { get; init; } = 0.05m;
    public decimal SignificanceLevel { get; init; } = 0.05m;
    public decimal StatisticalPower { get; init; } = 0.80m;
    public bool AutoSelectWinner { get; init; } = true;
}

/// <summary>
/// DTO for sample size calculation results.
/// </summary>
public record SampleSizeCalculationDto
{
    public decimal BaselineConversionRate { get; init; }
    public decimal MinimumDetectableEffect { get; init; }
    public decimal SignificanceLevel { get; init; }
    public decimal StatisticalPower { get; init; }
    public int RequiredSampleSizePerVariant { get; init; }
    public int TotalRequiredSampleSize { get; init; }
    public int EstimatedDaysToComplete { get; init; }
}

/// <summary>
/// DTO for experiment summary on dashboard.
/// </summary>
public record ExperimentSummaryDto
{
    public int TotalExperiments { get; init; }
    public int RunningExperiments { get; init; }
    public int CompletedExperiments { get; init; }
    public int TotalConversions { get; init; }
    public decimal TotalRevenue { get; init; }
    public List<UpsellExperimentDto> RecentExperiments { get; init; } = new();
}

/// <summary>
/// DTO for tracking click events.
/// </summary>
public record TrackClickDto
{
    public int ConversionId { get; init; }
    public string SessionId { get; init; } = string.Empty;
}

/// <summary>
/// DTO for tracking conversion events.
/// </summary>
public record TrackConversionDto
{
    public int ConversionId { get; init; }
    public long ConversionOrderId { get; init; }
    public decimal Revenue { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// DTO for impression tracking data.
/// </summary>
public record ImpressionDataDto
{
    public int OfferId { get; init; }
    public long PlatformOrderId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public int? ExperimentId { get; init; }
    public string? Variant { get; init; }
}

/// <summary>
/// Confidence interval data.
/// </summary>
public record ConfidenceIntervalDto
{
    public decimal Lower { get; init; }
    public decimal Upper { get; init; }
}
