using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryPredictionController : ControllerBase
{
    private readonly IInventoryPredictionService _predictionService;
    private readonly IInventoryAlertService _alertService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<InventoryPredictionController> _logger;

    public InventoryPredictionController(
        IInventoryPredictionService predictionService,
        IInventoryAlertService alertService,
        IShopContext shopContext,
        ILogger<InventoryPredictionController> logger)
    {
        _predictionService = predictionService;
        _alertService = alertService;
        _shopContext = shopContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory predictions for the current shop.
    /// </summary>
    [HttpGet("predictions")]
    public async Task<IActionResult> GetPredictions(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _predictionService.GetPredictionsAsync(
                _shopContext.ShopDomain, status, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions");
            return StatusCode(500, new { error = "Failed to get predictions" });
        }
    }

    /// <summary>
    /// Get inventory prediction summary (dashboard stats).
    /// </summary>
    [HttpGet("predictions/summary")]
    public async Task<IActionResult> GetPredictionSummary()
    {
        try
        {
            var result = await _predictionService.GetPredictionSummaryAsync(_shopContext.ShopDomain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction summary");
            return StatusCode(500, new { error = "Failed to get summary" });
        }
    }

    /// <summary>
    /// Get products at risk of stockout within specified days.
    /// </summary>
    [HttpGet("predictions/at-risk")]
    public async Task<IActionResult> GetAtRiskProducts([FromQuery] int withinDays = 14)
    {
        try
        {
            var result = await _predictionService.GetAtRiskProductsAsync(
                _shopContext.ShopDomain, withinDays);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting at-risk products");
            return StatusCode(500, new { error = "Failed to get at-risk products" });
        }
    }

    /// <summary>
    /// Trigger manual recalculation of predictions.
    /// </summary>
    [HttpPost("predictions/recalculate")]
    public async Task<IActionResult> RecalculatePredictions([FromQuery] int lookbackDays = 90)
    {
        try
        {
            var count = await _predictionService.CalculatePredictionsAsync(
                _shopContext.ShopDomain, lookbackDays);
            return Ok(new { predictionsUpdated = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating predictions");
            return StatusCode(500, new { error = "Failed to recalculate predictions" });
        }
    }

    /// <summary>
    /// Get sales velocity for a specific product.
    /// </summary>
    [HttpGet("products/{productId}/velocity")]
    public async Task<IActionResult> GetSalesVelocity(long productId, [FromQuery] long? variantId = null)
    {
        try
        {
            var result = await _predictionService.GetSalesVelocityAsync(
                _shopContext.ShopDomain, productId, variantId);
            if (result == null)
                return NotFound(new { error = "No sales data found for this product" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales velocity for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to get sales velocity" });
        }
    }

    /// <summary>
    /// Get all alerts for the current shop.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _alertService.GetAlertsAsync(
                _shopContext.ShopDomain, status, severity, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, new { error = "Failed to get alerts" });
        }
    }

    /// <summary>
    /// Get alert counts by severity.
    /// </summary>
    [HttpGet("alerts/counts")]
    public async Task<IActionResult> GetAlertCounts()
    {
        try
        {
            var result = await _alertService.GetAlertCountsBySeverityAsync(_shopContext.ShopDomain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert counts");
            return StatusCode(500, new { error = "Failed to get alert counts" });
        }
    }

    /// <summary>
    /// Acknowledge an alert.
    /// </summary>
    [HttpPost("alerts/{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int id)
    {
        try
        {
            await _alertService.AcknowledgeAlertAsync(id);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return StatusCode(500, new { error = "Failed to acknowledge alert" });
        }
    }

    /// <summary>
    /// Dismiss an alert.
    /// </summary>
    [HttpPost("alerts/{id}/dismiss")]
    public async Task<IActionResult> DismissAlert(int id, [FromBody] DismissAlertDto? dto = null)
    {
        try
        {
            await _alertService.DismissAlertAsync(id, dto?.Reason);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", id);
            return StatusCode(500, new { error = "Failed to dismiss alert" });
        }
    }

    /// <summary>
    /// Resolve an alert (mark as resolved).
    /// </summary>
    [HttpPost("alerts/{id}/resolve")]
    public async Task<IActionResult> ResolveAlert(int id)
    {
        try
        {
            await _alertService.ResolveAlertAsync(id);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return StatusCode(500, new { error = "Failed to resolve alert" });
        }
    }

    /// <summary>
    /// Get alert settings for the current shop.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var result = await _alertService.GetSettingsAsync(_shopContext.ShopDomain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update alert settings for the current shop.
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateInventoryAlertSettingsDto dto)
    {
        try
        {
            var result = await _alertService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }
}
