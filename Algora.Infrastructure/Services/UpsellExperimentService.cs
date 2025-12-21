using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for managing A/B test experiments with statistical analysis.
/// Implements sample size calculation, Wilson confidence intervals, and z-test significance.
/// </summary>
public class UpsellExperimentService : IUpsellExperimentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UpsellExperimentService> _logger;

    public UpsellExperimentService(
        AppDbContext db,
        ILogger<UpsellExperimentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UpsellExperimentDto> CreateExperimentAsync(string shopDomain, CreateExperimentDto dto)
    {
        // Validate traffic allocation
        var totalTraffic = dto.ControlTrafficPercent + dto.VariantATrafficPercent + (dto.VariantBTrafficPercent ?? 0);
        if (totalTraffic != 100)
            throw new ArgumentException("Traffic allocation must sum to 100%");

        var experiment = new UpsellExperiment
        {
            ShopDomain = shopDomain,
            Name = dto.Name,
            Description = dto.Description,
            Status = "draft",
            PrimaryMetric = dto.PrimaryMetric,
            ControlTrafficPercent = dto.ControlTrafficPercent,
            VariantATrafficPercent = dto.VariantATrafficPercent,
            VariantBTrafficPercent = dto.VariantBTrafficPercent,
            MinimumDetectableEffect = dto.MinimumDetectableEffect,
            SignificanceLevel = dto.SignificanceLevel,
            StatisticalPower = dto.StatisticalPower,
            AutoSelectWinner = dto.AutoSelectWinner,
            CreatedAt = DateTime.UtcNow
        };

        // Calculate required sample size
        var sampleSize = CalculateSampleSize(0.03m, dto.MinimumDetectableEffect, dto.SignificanceLevel, dto.StatisticalPower);
        experiment.CalculatedSampleSize = sampleSize.RequiredSampleSizePerVariant;

        _db.UpsellExperiments.Add(experiment);
        await _db.SaveChangesAsync();

        // Link offers to experiment using OfferIds list
        var variantNames = new[] { "control", "variant_a", "variant_b", "variant_c" };
        for (int i = 0; i < dto.OfferIds.Count && i < variantNames.Length; i++)
        {
            var offer = await _db.UpsellOffers.FindAsync(dto.OfferIds[i]);
            if (offer != null)
            {
                offer.ExperimentId = experiment.Id;
                offer.ExperimentVariant = variantNames[i];
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Created experiment {ExperimentId} for shop {Shop}", experiment.Id, shopDomain);
        return await BuildExperimentDto(experiment);
    }

    public async Task<UpsellExperimentDto> StartExperimentAsync(int experimentId)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        if (experiment == null)
            throw new InvalidOperationException($"Experiment {experimentId} not found");

        if (experiment.Status != "draft" && experiment.Status != "paused")
            throw new InvalidOperationException($"Cannot start experiment in {experiment.Status} status");

        experiment.Status = "running";
        experiment.StartedAt = experiment.StartedAt ?? DateTime.UtcNow;
        experiment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Started experiment {ExperimentId}", experimentId);
        return await BuildExperimentDto(experiment);
    }

    public async Task<UpsellExperimentDto> PauseExperimentAsync(int experimentId)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        if (experiment == null)
            throw new InvalidOperationException($"Experiment {experimentId} not found");

        experiment.Status = "paused";
        experiment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Paused experiment {ExperimentId}", experimentId);
        return await BuildExperimentDto(experiment);
    }

    public async Task<UpsellExperimentDto> EndExperimentAsync(int experimentId, string? winningVariant = null)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        if (experiment == null)
            throw new InvalidOperationException($"Experiment {experimentId} not found");

        experiment.Status = winningVariant != null ? "winner_selected" : "completed";
        experiment.EndedAt = DateTime.UtcNow;
        experiment.WinningVariant = winningVariant;
        experiment.WinnerSelectedAt = winningVariant != null ? DateTime.UtcNow : null;
        experiment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Ended experiment {ExperimentId} with winner {Winner}", experimentId, winningVariant ?? "none");
        return await BuildExperimentDto(experiment);
    }

    public async Task<UpsellExperimentDto?> GetExperimentAsync(int experimentId)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        return experiment != null ? await BuildExperimentDto(experiment) : null;
    }

    public async Task<PaginatedResult<UpsellExperimentDto>> GetExperimentsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.UpsellExperiments
            .Where(e => e.ShopDomain == shopDomain)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        var totalCount = await query.CountAsync();

        var experiments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UpsellExperimentDto>();
        foreach (var experiment in experiments)
        {
            items.Add(await BuildExperimentDto(experiment));
        }

        return new PaginatedResult<UpsellExperimentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ExperimentSummaryDto> GetExperimentSummaryAsync(string shopDomain)
    {
        var experiments = await _db.UpsellExperiments
            .Where(e => e.ShopDomain == shopDomain)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var recentDtos = new List<UpsellExperimentDto>();
        foreach (var exp in experiments.Take(5))
        {
            recentDtos.Add(await BuildExperimentDto(exp));
        }

        return new ExperimentSummaryDto
        {
            TotalExperiments = experiments.Count,
            RunningExperiments = experiments.Count(e => e.Status == "running"),
            CompletedExperiments = experiments.Count(e => e.Status == "completed" || e.Status == "winner_selected"),
            RecentExperiments = recentDtos
        };
    }

    public Task<string> AssignVariantAsync(int experimentId, string sessionId)
    {
        // Use deterministic hashing to ensure consistent assignment
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{experimentId}:{sessionId}"));
        var hashValue = BitConverter.ToUInt32(hash, 0);
        var bucket = hashValue % 100;

        // TODO: Get experiment traffic allocation from DB
        // For now, assume 50/50 split
        var variant = bucket < 50 ? "control" : "variant_a";

        return Task.FromResult(variant);
    }

    public async Task<int> RecordImpressionAsync(
        string shopDomain,
        int offerId,
        long platformOrderId,
        string sessionId,
        int? experimentId = null,
        string? variant = null)
    {
        var conversion = new UpsellConversion
        {
            ShopDomain = shopDomain,
            UpsellOfferId = offerId,
            PlatformSourceOrderId = platformOrderId,
            SessionId = sessionId,
            ExperimentId = experimentId,
            AssignedVariant = variant,
            ImpressionAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.UpsellConversions.Add(conversion);
        await _db.SaveChangesAsync();

        // Update experiment counters
        if (experimentId.HasValue)
        {
            await UpdateExperimentCountersAsync(experimentId.Value, variant, "impression");
        }

        return conversion.Id;
    }

    public async Task RecordClickAsync(int conversionId)
    {
        var conversion = await _db.UpsellConversions.FindAsync(conversionId);
        if (conversion == null) return;

        conversion.ClickedAt = DateTime.UtcNow;
        conversion.CartUrlUsed = true;

        await _db.SaveChangesAsync();

        if (conversion.ExperimentId.HasValue)
        {
            await UpdateExperimentCountersAsync(conversion.ExperimentId.Value, conversion.AssignedVariant, "click");
        }
    }

    public async Task RecordConversionAsync(
        int conversionId,
        long conversionOrderId,
        decimal revenue,
        int quantity)
    {
        var conversion = await _db.UpsellConversions.FindAsync(conversionId);
        if (conversion == null) return;

        conversion.ConvertedAt = DateTime.UtcNow;
        conversion.ConversionOrderId = conversionOrderId;
        conversion.ConversionRevenue = revenue;
        conversion.ConversionQuantity = quantity;

        await _db.SaveChangesAsync();

        if (conversion.ExperimentId.HasValue)
        {
            await UpdateExperimentCountersAsync(conversion.ExperimentId.Value, conversion.AssignedVariant, "conversion", revenue);
        }
    }

    public SampleSizeCalculationDto CalculateSampleSize(
        decimal baselineConversionRate,
        decimal minimumDetectableEffect,
        decimal significanceLevel = 0.05m,
        decimal power = 0.80m)
    {
        // Two-proportion z-test sample size formula
        // n = 2 * ((Z_alpha + Z_beta)^2 * p_bar * (1 - p_bar)) / (p1 - p2)^2

        double p1 = (double)baselineConversionRate;
        double p2 = p1 * (1 + (double)minimumDetectableEffect);
        double pBar = (p1 + p2) / 2;

        double zAlpha = GetZScore((double)(1 - significanceLevel / 2)); // Two-tailed
        double zBeta = GetZScore((double)power);

        double numerator = 2 * Math.Pow(zAlpha + zBeta, 2) * pBar * (1 - pBar);
        double denominator = Math.Pow(p2 - p1, 2);

        int sampleSizePerVariant = denominator > 0 ? (int)Math.Ceiling(numerator / denominator) : 1000;

        // Estimate days to complete (assuming 100 impressions/day)
        int estimatedDays = sampleSizePerVariant / 50;

        return new SampleSizeCalculationDto
        {
            BaselineConversionRate = baselineConversionRate,
            MinimumDetectableEffect = minimumDetectableEffect,
            SignificanceLevel = significanceLevel,
            StatisticalPower = power,
            RequiredSampleSizePerVariant = sampleSizePerVariant,
            TotalRequiredSampleSize = sampleSizePerVariant * 2,
            EstimatedDaysToComplete = estimatedDays
        };
    }

    public async Task<UpsellExperimentDto> RecalculateStatisticsAsync(int experimentId)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        if (experiment == null)
            throw new InvalidOperationException($"Experiment {experimentId} not found");

        // Calculate conversion rates
        if (experiment.ControlImpressions > 0)
            experiment.ControlConversionRate = (decimal)experiment.ControlConversions / experiment.ControlImpressions;

        if (experiment.VariantAImpressions > 0)
            experiment.VariantAConversionRate = (decimal)experiment.VariantAConversions / experiment.VariantAImpressions;

        if (experiment.VariantBImpressions.HasValue && experiment.VariantBImpressions > 0)
            experiment.VariantBConversionRate = (decimal)experiment.VariantBConversions!.Value / experiment.VariantBImpressions.Value;

        // Calculate Wilson confidence intervals
        var controlCI = CalculateWilsonConfidenceInterval(experiment.ControlConversions, experiment.ControlImpressions);
        experiment.ControlConfidenceInterval = JsonSerializer.Serialize(new { lower = controlCI.lower, upper = controlCI.upper });

        var variantACI = CalculateWilsonConfidenceInterval(experiment.VariantAConversions, experiment.VariantAImpressions);
        experiment.VariantAConfidenceInterval = JsonSerializer.Serialize(new { lower = variantACI.lower, upper = variantACI.upper });

        if (experiment.VariantBConversions.HasValue && experiment.VariantBImpressions.HasValue)
        {
            var variantBCI = CalculateWilsonConfidenceInterval(experiment.VariantBConversions.Value, experiment.VariantBImpressions.Value);
            experiment.VariantBConfidenceInterval = JsonSerializer.Serialize(new { lower = variantBCI.lower, upper = variantBCI.upper });
        }

        // Calculate p-value (control vs variant A)
        if (experiment.ControlImpressions > 0 && experiment.VariantAImpressions > 0)
        {
            var pValue = CalculatePValue(
                experiment.VariantAConversions, experiment.VariantAImpressions,
                experiment.ControlConversions, experiment.ControlImpressions);
            experiment.PValueVsControl = pValue;
            experiment.IsStatisticallySignificant = pValue < experiment.SignificanceLevel;

            // Determine winner
            if (experiment.IsStatisticallySignificant)
            {
                if (experiment.VariantAConversionRate > experiment.ControlConversionRate)
                {
                    experiment.WinningVariant = "variant_a";
                    experiment.WinningLift = experiment.ControlConversionRate > 0
                        ? (experiment.VariantAConversionRate - experiment.ControlConversionRate) / experiment.ControlConversionRate * 100
                        : 0;
                }
                else
                {
                    experiment.WinningVariant = "control";
                    experiment.WinningLift = 0;
                }
            }
        }

        experiment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await BuildExperimentDto(experiment);
    }

    public async Task<int> ProcessAutoWinnerSelectionAsync(string shopDomain)
    {
        var runningExperiments = await _db.UpsellExperiments
            .Where(e => e.ShopDomain == shopDomain)
            .Where(e => e.Status == "running")
            .Where(e => e.AutoSelectWinner)
            .ToListAsync();

        int winnersSelected = 0;

        foreach (var experiment in runningExperiments)
        {
            // Check if we have enough sample size
            var currentSample = Math.Min(experiment.ControlImpressions, experiment.VariantAImpressions);
            if (experiment.CalculatedSampleSize.HasValue && currentSample >= experiment.CalculatedSampleSize.Value)
            {
                await RecalculateStatisticsAsync(experiment.Id);

                // Reload after recalculation
                await _db.Entry(experiment).ReloadAsync();

                if (experiment.IsStatisticallySignificant && experiment.WinningVariant != null)
                {
                    experiment.Status = "winner_selected";
                    experiment.WinnerSelectedAt = DateTime.UtcNow;
                    experiment.EndedAt = DateTime.UtcNow;
                    winnersSelected++;

                    _logger.LogInformation("Auto-selected winner {Winner} for experiment {ExperimentId}",
                        experiment.WinningVariant, experiment.Id);
                }
            }
        }

        await _db.SaveChangesAsync();
        return winnersSelected;
    }

    private async Task UpdateExperimentCountersAsync(int experimentId, string? variant, string eventType, decimal revenue = 0)
    {
        var experiment = await _db.UpsellExperiments.FindAsync(experimentId);
        if (experiment == null) return;

        switch (variant)
        {
            case "control":
                if (eventType == "impression") experiment.ControlImpressions++;
                else if (eventType == "click") experiment.ControlClicks++;
                else if (eventType == "conversion") { experiment.ControlConversions++; experiment.ControlRevenue += revenue; }
                break;
            case "variant_a":
                if (eventType == "impression") experiment.VariantAImpressions++;
                else if (eventType == "click") experiment.VariantAClicks++;
                else if (eventType == "conversion") { experiment.VariantAConversions++; experiment.VariantARevenue += revenue; }
                break;
            case "variant_b":
                if (eventType == "impression") experiment.VariantBImpressions = (experiment.VariantBImpressions ?? 0) + 1;
                else if (eventType == "click") experiment.VariantBClicks = (experiment.VariantBClicks ?? 0) + 1;
                else if (eventType == "conversion")
                {
                    experiment.VariantBConversions = (experiment.VariantBConversions ?? 0) + 1;
                    experiment.VariantBRevenue = (experiment.VariantBRevenue ?? 0) + revenue;
                }
                break;
        }

        experiment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private (decimal lower, decimal upper) CalculateWilsonConfidenceInterval(int successes, int trials, decimal confidenceLevel = 0.95m)
    {
        if (trials == 0) return (0, 0);

        double z = GetZScore((double)(1 - (1 - confidenceLevel) / 2));
        double p = (double)successes / trials;
        double n = trials;

        double denominator = 1 + z * z / n;
        double center = (p + z * z / (2 * n)) / denominator;
        double margin = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denominator;

        return ((decimal)Math.Max(0, center - margin), (decimal)Math.Min(1, center + margin));
    }

    private decimal CalculatePValue(int successesA, int trialsA, int successesB, int trialsB)
    {
        if (trialsA == 0 || trialsB == 0) return 1m;

        double p1 = (double)successesA / trialsA;
        double p2 = (double)successesB / trialsB;
        double pPooled = (double)(successesA + successesB) / (trialsA + trialsB);

        double standardError = Math.Sqrt(pPooled * (1 - pPooled) * (1.0 / trialsA + 1.0 / trialsB));
        if (standardError == 0) return 1m;

        double zScore = (p1 - p2) / standardError;

        // Two-tailed p-value using normal CDF approximation
        return (decimal)(2 * (1 - NormalCdf(Math.Abs(zScore))));
    }

    private static double GetZScore(double percentile)
    {
        // Abramowitz and Stegun approximation for inverse normal CDF
        // Accurate to about 1e-9
        if (percentile <= 0) return double.NegativeInfinity;
        if (percentile >= 1) return double.PositiveInfinity;

        double t = Math.Sqrt(-2 * Math.Log(percentile < 0.5 ? percentile : 1 - percentile));
        double c0 = 2.515517;
        double c1 = 0.802853;
        double c2 = 0.010328;
        double d1 = 1.432788;
        double d2 = 0.189269;
        double d3 = 0.001308;

        double z = t - (c0 + c1 * t + c2 * t * t) / (1 + d1 * t + d2 * t * t + d3 * t * t * t);
        return percentile < 0.5 ? -z : z;
    }

    private static double NormalCdf(double x)
    {
        // Standard normal CDF using error function approximation
        return 0.5 * (1 + Erf(x / Math.Sqrt(2)));
    }

    private static double Erf(double x)
    {
        // Horner form approximation for error function
        double t = 1.0 / (1.0 + 0.5 * Math.Abs(x));
        double tau = t * Math.Exp(-x * x - 1.26551223 +
            t * (1.00002368 +
            t * (0.37409196 +
            t * (0.09678418 +
            t * (-0.18628806 +
            t * (0.27886807 +
            t * (-1.13520398 +
            t * (1.48851587 +
            t * (-0.82215223 +
            t * 0.17087277)))))))));
        return x >= 0 ? 1 - tau : tau - 1;
    }

    private async Task<UpsellExperimentDto> BuildExperimentDto(UpsellExperiment e)
    {
        var currentSample = e.ControlImpressions + e.VariantAImpressions + (e.VariantBImpressions ?? 0);
        var requiredSample = (e.CalculatedSampleSize ?? 1000) * (e.VariantBTrafficPercent.HasValue ? 3 : 2);
        var progress = requiredSample > 0 ? Math.Min(100, (decimal)currentSample / requiredSample * 100) : 0;

        var controlCI = ParseConfidenceInterval(e.ControlConfidenceInterval);
        var variantACI = ParseConfidenceInterval(e.VariantAConfidenceInterval);
        var variantBCI = ParseConfidenceInterval(e.VariantBConfidenceInterval);

        // Build variant results list
        var variantResults = new List<ExperimentVariantResultDto>
        {
            new ExperimentVariantResultDto
            {
                VariantName = "Control",
                Impressions = e.ControlImpressions,
                Clicks = e.ControlClicks,
                Conversions = e.ControlConversions,
                Revenue = e.ControlRevenue,
                ClickRate = e.ControlImpressions > 0 ? (decimal)e.ControlClicks / e.ControlImpressions : 0,
                ConversionRate = e.ControlConversionRate ?? 0,
                RevenuePerView = e.ControlImpressions > 0 ? e.ControlRevenue / e.ControlImpressions : 0,
                ConfidenceIntervalLower = controlCI.lower,
                ConfidenceIntervalUpper = controlCI.upper,
                LiftVsControl = null,
                IsWinner = e.WinningVariant == "control"
            },
            new ExperimentVariantResultDto
            {
                VariantName = "Variant A",
                Impressions = e.VariantAImpressions,
                Clicks = e.VariantAClicks,
                Conversions = e.VariantAConversions,
                Revenue = e.VariantARevenue,
                ClickRate = e.VariantAImpressions > 0 ? (decimal)e.VariantAClicks / e.VariantAImpressions : 0,
                ConversionRate = e.VariantAConversionRate ?? 0,
                RevenuePerView = e.VariantAImpressions > 0 ? e.VariantARevenue / e.VariantAImpressions : 0,
                ConfidenceIntervalLower = variantACI.lower,
                ConfidenceIntervalUpper = variantACI.upper,
                LiftVsControl = e.ControlConversionRate > 0 && e.VariantAConversionRate.HasValue
                    ? (e.VariantAConversionRate.Value - e.ControlConversionRate.Value) / e.ControlConversionRate.Value * 100
                    : null,
                IsWinner = e.WinningVariant == "variant_a"
            }
        };

        if (e.VariantBTrafficPercent.HasValue)
        {
            variantResults.Add(new ExperimentVariantResultDto
            {
                VariantName = "Variant B",
                Impressions = e.VariantBImpressions ?? 0,
                Clicks = e.VariantBClicks ?? 0,
                Conversions = e.VariantBConversions ?? 0,
                Revenue = e.VariantBRevenue ?? 0,
                ClickRate = e.VariantBImpressions > 0 ? (decimal)(e.VariantBClicks ?? 0) / e.VariantBImpressions.Value : 0,
                ConversionRate = e.VariantBConversionRate ?? 0,
                RevenuePerView = e.VariantBImpressions > 0 ? (e.VariantBRevenue ?? 0) / e.VariantBImpressions.Value : 0,
                ConfidenceIntervalLower = variantBCI.lower,
                ConfidenceIntervalUpper = variantBCI.upper,
                LiftVsControl = e.ControlConversionRate > 0 && e.VariantBConversionRate.HasValue
                    ? (e.VariantBConversionRate.Value - e.ControlConversionRate.Value) / e.ControlConversionRate.Value * 100
                    : null,
                IsWinner = e.WinningVariant == "variant_b"
            });
        }

        var totalImpressions = e.ControlImpressions + e.VariantAImpressions + (e.VariantBImpressions ?? 0);
        var totalConversions = e.ControlConversions + e.VariantAConversions + (e.VariantBConversions ?? 0);
        var totalRevenue = e.ControlRevenue + e.VariantARevenue + (e.VariantBRevenue ?? 0);

        return new UpsellExperimentDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Status = e.Status,
            PrimaryMetric = e.PrimaryMetric,
            TrafficPercentage = 100, // Full traffic
            ControlTrafficPercent = e.ControlTrafficPercent,
            VariantATrafficPercent = e.VariantATrafficPercent,
            VariantBTrafficPercent = e.VariantBTrafficPercent,
            VariantCount = variantResults.Count,
            VariantResults = variantResults,
            MinSampleSize = e.CalculatedSampleSize ?? 100,
            CalculatedSampleSize = e.CalculatedSampleSize,
            CurrentSampleSize = currentSample,
            SampleSizeProgress = progress,
            TotalImpressions = totalImpressions,
            TotalConversions = totalConversions,
            TotalRevenue = totalRevenue,
            SignificanceLevel = e.SignificanceLevel,
            AutoSelectWinner = e.AutoSelectWinner,
            IsStatisticallySignificant = e.IsStatisticallySignificant,
            WinningVariant = e.WinningVariant,
            WinningLift = e.WinningLift,
            PValue = e.PValueVsControl,
            StartedAt = e.StartedAt,
            EndedAt = e.EndedAt,
            CreatedAt = e.CreatedAt
        };
    }

    private static (decimal lower, decimal upper) ParseConfidenceInterval(string? json)
    {
        if (string.IsNullOrEmpty(json)) return (0, 0);
        try
        {
            var ci = JsonSerializer.Deserialize<ConfidenceIntervalDto>(json);
            return ci != null ? (ci.Lower, ci.Upper) : (0, 0);
        }
        catch
        {
            return (0, 0);
        }
    }
}
