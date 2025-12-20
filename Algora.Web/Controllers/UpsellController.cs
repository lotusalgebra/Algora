using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/upsell")]
public class UpsellController : ControllerBase
{
    private readonly IUpsellRecommendationService _recommendationService;
    private readonly IUpsellExperimentService _experimentService;
    private readonly IProductAffinityService _affinityService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<UpsellController> _logger;

    public UpsellController(
        IUpsellRecommendationService recommendationService,
        IUpsellExperimentService experimentService,
        IProductAffinityService affinityService,
        IShopContext shopContext,
        ILogger<UpsellController> logger)
    {
        _recommendationService = recommendationService;
        _experimentService = experimentService;
        _affinityService = affinityService;
        _shopContext = shopContext;
        _logger = logger;
    }

    #region Offers

    /// <summary>
    /// Get all upsell offers.
    /// </summary>
    [HttpGet("offers")]
    public async Task<IActionResult> GetOffers(
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _recommendationService.GetOffersAsync(
                _shopContext.ShopDomain, isActive, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting offers");
            return StatusCode(500, new { error = "Failed to get offers" });
        }
    }

    /// <summary>
    /// Get a specific offer by ID.
    /// </summary>
    [HttpGet("offers/{id}")]
    public async Task<IActionResult> GetOffer(int id)
    {
        try
        {
            var offer = await _recommendationService.GetOfferByIdAsync(id);
            if (offer == null)
                return NotFound(new { error = "Offer not found" });
            return Ok(offer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting offer {OfferId}", id);
            return StatusCode(500, new { error = "Failed to get offer" });
        }
    }

    /// <summary>
    /// Create a new upsell offer.
    /// </summary>
    [HttpPost("offers")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateUpsellOfferDto dto)
    {
        try
        {
            var offer = await _recommendationService.CreateOfferAsync(_shopContext.ShopDomain, dto);
            return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, offer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating offer");
            return StatusCode(500, new { error = "Failed to create offer" });
        }
    }

    /// <summary>
    /// Update an existing offer.
    /// </summary>
    [HttpPut("offers/{id}")]
    public async Task<IActionResult> UpdateOffer(int id, [FromBody] CreateUpsellOfferDto dto)
    {
        try
        {
            var offer = await _recommendationService.UpdateOfferAsync(id, dto);
            return Ok(offer);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offer {OfferId}", id);
            return StatusCode(500, new { error = "Failed to update offer" });
        }
    }

    /// <summary>
    /// Delete an offer.
    /// </summary>
    [HttpDelete("offers/{id}")]
    public async Task<IActionResult> DeleteOffer(int id)
    {
        try
        {
            await _recommendationService.DeleteOfferAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting offer {OfferId}", id);
            return StatusCode(500, new { error = "Failed to delete offer" });
        }
    }

    /// <summary>
    /// Preview offers for specific products.
    /// </summary>
    [HttpPost("offers/preview")]
    public async Task<IActionResult> PreviewOffers([FromBody] List<long> productIds, [FromQuery] int maxOffers = 3)
    {
        try
        {
            var offers = await _recommendationService.GetOffersForProductsAsync(
                _shopContext.ShopDomain, productIds, maxOffers);
            return Ok(offers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing offers");
            return StatusCode(500, new { error = "Failed to preview offers" });
        }
    }

    #endregion

    #region Experiments

    /// <summary>
    /// Get all experiments.
    /// </summary>
    [HttpGet("experiments")]
    public async Task<IActionResult> GetExperiments(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _experimentService.GetExperimentsAsync(
                _shopContext.ShopDomain, status, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting experiments");
            return StatusCode(500, new { error = "Failed to get experiments" });
        }
    }

    /// <summary>
    /// Get experiment summary for dashboard.
    /// </summary>
    [HttpGet("experiments/summary")]
    public async Task<IActionResult> GetExperimentSummary()
    {
        try
        {
            var result = await _experimentService.GetExperimentSummaryAsync(_shopContext.ShopDomain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting experiment summary");
            return StatusCode(500, new { error = "Failed to get summary" });
        }
    }

    /// <summary>
    /// Get a specific experiment.
    /// </summary>
    [HttpGet("experiments/{id}")]
    public async Task<IActionResult> GetExperiment(int id)
    {
        try
        {
            var experiment = await _experimentService.GetExperimentAsync(id);
            if (experiment == null)
                return NotFound(new { error = "Experiment not found" });
            return Ok(experiment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting experiment {ExperimentId}", id);
            return StatusCode(500, new { error = "Failed to get experiment" });
        }
    }

    /// <summary>
    /// Create a new experiment.
    /// </summary>
    [HttpPost("experiments")]
    public async Task<IActionResult> CreateExperiment([FromBody] CreateExperimentDto dto)
    {
        try
        {
            var experiment = await _experimentService.CreateExperimentAsync(_shopContext.ShopDomain, dto);
            return CreatedAtAction(nameof(GetExperiment), new { id = experiment.Id }, experiment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating experiment");
            return StatusCode(500, new { error = "Failed to create experiment" });
        }
    }

    /// <summary>
    /// Start an experiment.
    /// </summary>
    [HttpPost("experiments/{id}/start")]
    public async Task<IActionResult> StartExperiment(int id)
    {
        try
        {
            var experiment = await _experimentService.StartExperimentAsync(id);
            return Ok(experiment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting experiment {ExperimentId}", id);
            return StatusCode(500, new { error = "Failed to start experiment" });
        }
    }

    /// <summary>
    /// Pause an experiment.
    /// </summary>
    [HttpPost("experiments/{id}/pause")]
    public async Task<IActionResult> PauseExperiment(int id)
    {
        try
        {
            var experiment = await _experimentService.PauseExperimentAsync(id);
            return Ok(experiment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing experiment {ExperimentId}", id);
            return StatusCode(500, new { error = "Failed to pause experiment" });
        }
    }

    /// <summary>
    /// End an experiment.
    /// </summary>
    [HttpPost("experiments/{id}/end")]
    public async Task<IActionResult> EndExperiment(int id, [FromQuery] string? winningVariant = null)
    {
        try
        {
            var experiment = await _experimentService.EndExperimentAsync(id, winningVariant);
            return Ok(experiment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending experiment {ExperimentId}", id);
            return StatusCode(500, new { error = "Failed to end experiment" });
        }
    }

    /// <summary>
    /// Recalculate statistics for an experiment.
    /// </summary>
    [HttpPost("experiments/{id}/recalculate")]
    public async Task<IActionResult> RecalculateStatistics(int id)
    {
        try
        {
            var experiment = await _experimentService.RecalculateStatisticsAsync(id);
            return Ok(experiment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating statistics for experiment {ExperimentId}", id);
            return StatusCode(500, new { error = "Failed to recalculate statistics" });
        }
    }

    /// <summary>
    /// Calculate sample size for experiment planning.
    /// </summary>
    [HttpPost("experiments/calculate-sample-size")]
    public IActionResult CalculateSampleSize([FromBody] SampleSizeRequest request)
    {
        try
        {
            var result = _experimentService.CalculateSampleSize(
                request.BaselineConversionRate,
                request.MinimumDetectableEffect,
                request.SignificanceLevel,
                request.StatisticalPower);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating sample size");
            return StatusCode(500, new { error = "Failed to calculate sample size" });
        }
    }

    #endregion

    #region Affinities

    /// <summary>
    /// Get all product affinities.
    /// </summary>
    [HttpGet("affinities")]
    public async Task<IActionResult> GetAffinities(
        [FromQuery] decimal? minConfidence = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _affinityService.GetAllAffinitiesAsync(
                _shopContext.ShopDomain, minConfidence, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting affinities");
            return StatusCode(500, new { error = "Failed to get affinities" });
        }
    }

    /// <summary>
    /// Get affinity summary.
    /// </summary>
    [HttpGet("affinities/summary")]
    public async Task<IActionResult> GetAffinitySummary()
    {
        try
        {
            var result = await _affinityService.GetAffinitySummaryAsync(_shopContext.ShopDomain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting affinity summary");
            return StatusCode(500, new { error = "Failed to get summary" });
        }
    }

    /// <summary>
    /// Get affinities for a specific product.
    /// </summary>
    [HttpGet("affinities/product/{productId}")]
    public async Task<IActionResult> GetProductAffinities(long productId, [FromQuery] int limit = 10)
    {
        try
        {
            var result = await _affinityService.GetAffinitiesForProductAsync(
                _shopContext.ShopDomain, productId, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting affinities for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to get product affinities" });
        }
    }

    /// <summary>
    /// Trigger manual recalculation of affinities.
    /// </summary>
    [HttpPost("affinities/recalculate")]
    public async Task<IActionResult> RecalculateAffinities([FromQuery] int lookbackDays = 90)
    {
        try
        {
            var count = await _affinityService.CalculateAffinitiesAsync(
                _shopContext.ShopDomain, lookbackDays);
            return Ok(new { affinitiesCalculated = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating affinities");
            return StatusCode(500, new { error = "Failed to recalculate affinities" });
        }
    }

    #endregion

    #region Tracking (Public Endpoints)

    /// <summary>
    /// Record a click event.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("track/click")]
    public async Task<IActionResult> TrackClick([FromBody] TrackClickDto dto)
    {
        try
        {
            await _experimentService.RecordClickAsync(dto.ConversionId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking click");
            return StatusCode(500, new { error = "Failed to track click" });
        }
    }

    /// <summary>
    /// Record a conversion event.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("track/conversion")]
    public async Task<IActionResult> TrackConversion([FromBody] TrackConversionDto dto)
    {
        try
        {
            await _experimentService.RecordConversionAsync(
                dto.ConversionId, dto.ConversionOrderId, dto.Revenue, dto.Quantity);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking conversion");
            return StatusCode(500, new { error = "Failed to track conversion" });
        }
    }

    #endregion

    #region Settings

    /// <summary>
    /// Get upsell settings.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await _recommendationService.GetSettingsAsync(_shopContext.ShopDomain);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update upsell settings.
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateUpsellSettingsDto dto)
    {
        try
        {
            var settings = await _recommendationService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    #endregion
}

public record SampleSizeRequest
{
    public decimal BaselineConversionRate { get; init; }
    public decimal MinimumDetectableEffect { get; init; }
    public decimal SignificanceLevel { get; init; } = 0.05m;
    public decimal StatisticalPower { get; init; } = 0.80m;
}
