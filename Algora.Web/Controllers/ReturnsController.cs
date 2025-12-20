using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[ApiController]
[Route("api/returns")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;
    private readonly IShippoService _shippoService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ReturnsController> _logger;

    public ReturnsController(
        IReturnService returnService,
        IShippoService shippoService,
        IShopContext shopContext,
        ILogger<ReturnsController> logger)
    {
        _returnService = returnService;
        _shippoService = shippoService;
        _shopContext = shopContext;
        _logger = logger;
    }

    #region Public Endpoints (Customer Portal)

    /// <summary>
    /// Check if an order is eligible for return (by order number and email).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("check-eligibility")]
    public async Task<IActionResult> CheckEligibility([FromBody] CheckEligibilityRequest request)
    {
        if (string.IsNullOrEmpty(request.Shop))
            return BadRequest(new { error = "Shop domain is required" });

        try
        {
            var eligibility = await _returnService.CheckReturnEligibilityByOrderNumberAsync(
                request.Shop, request.OrderNumber, request.Email);

            if (eligibility == null)
                return NotFound(new { error = "Order not found" });

            return Ok(eligibility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking eligibility for order {OrderNumber}", request.OrderNumber);
            return StatusCode(500, new { error = "Failed to check eligibility" });
        }
    }

    /// <summary>
    /// Submit a return request (customer portal).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReturn([FromBody] SubmitReturnRequest request)
    {
        if (string.IsNullOrEmpty(request.Shop))
            return BadRequest(new { error = "Shop domain is required" });

        try
        {
            var returnRequest = await _returnService.CreateReturnRequestAsync(request.Shop, request.Data);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting return for order {OrderId}", request.Data?.OrderId);
            return StatusCode(500, new { error = "Failed to submit return" });
        }
    }

    /// <summary>
    /// Track return status by request number.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("track/{requestNumber}")]
    public async Task<IActionResult> TrackReturn(string requestNumber, [FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
            return BadRequest(new { error = "Shop domain is required" });

        try
        {
            var returnRequest = await _returnService.GetReturnRequestByNumberAsync(shop, requestNumber);
            if (returnRequest == null)
                return NotFound(new { error = "Return request not found" });

            return Ok(returnRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking return {RequestNumber}", requestNumber);
            return StatusCode(500, new { error = "Failed to track return" });
        }
    }

    /// <summary>
    /// Get available return reasons for customer portal.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("reasons")]
    public async Task<IActionResult> GetReasons([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
            return BadRequest(new { error = "Shop domain is required" });

        try
        {
            var reasons = await _returnService.GetActiveReasonsAsync(shop);
            return Ok(reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return reasons for shop {Shop}", shop);
            return StatusCode(500, new { error = "Failed to get reasons" });
        }
    }

    #endregion

    #region Admin Endpoints (Authenticated)

    /// <summary>
    /// Get all return requests.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetReturns(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _returnService.GetReturnRequestsAsync(
                _shopContext.ShopDomain, status, search, startDate, endDate, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting returns");
            return StatusCode(500, new { error = "Failed to get returns" });
        }
    }

    /// <summary>
    /// Get a specific return request by ID.
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReturn(int id)
    {
        try
        {
            var returnRequest = await _returnService.GetReturnRequestAsync(id);
            if (returnRequest == null)
                return NotFound(new { error = "Return request not found" });

            if (returnRequest.ShopDomain != _shopContext.ShopDomain)
                return NotFound(new { error = "Return request not found" });

            return Ok(returnRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return {ReturnId}", id);
            return StatusCode(500, new { error = "Failed to get return" });
        }
    }

    /// <summary>
    /// Get return summary for dashboard.
    /// </summary>
    [Authorize]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var summary = await _returnService.GetReturnSummaryAsync(_shopContext.ShopDomain);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return summary");
            return StatusCode(500, new { error = "Failed to get summary" });
        }
    }

    /// <summary>
    /// Get return analytics.
    /// </summary>
    [Authorize]
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var analytics = await _returnService.GetAnalyticsAsync(
                _shopContext.ShopDomain, startDate, endDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return analytics");
            return StatusCode(500, new { error = "Failed to get analytics" });
        }
    }

    /// <summary>
    /// Approve a return request.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveReturn(int id, [FromBody] ApproveReturnRequest? request = null)
    {
        try
        {
            var returnRequest = await _returnService.ApproveReturnAsync(id, request?.Note);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving return {ReturnId}", id);
            return StatusCode(500, new { error = "Failed to approve return" });
        }
    }

    /// <summary>
    /// Reject a return request.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectReturn(int id, [FromBody] RejectReturnRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason))
            return BadRequest(new { error = "Rejection reason is required" });

        try
        {
            var returnRequest = await _returnService.RejectReturnAsync(id, request.Reason);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting return {ReturnId}", id);
            return StatusCode(500, new { error = "Failed to reject return" });
        }
    }

    /// <summary>
    /// Mark a return as shipped by customer.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/shipped")]
    public async Task<IActionResult> MarkAsShipped(int id, [FromBody] MarkShippedRequest? request = null)
    {
        try
        {
            var returnRequest = await _returnService.MarkAsShippedAsync(
                id, request?.TrackingNumber, request?.Carrier);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking return {ReturnId} as shipped", id);
            return StatusCode(500, new { error = "Failed to mark as shipped" });
        }
    }

    /// <summary>
    /// Mark a return as received at warehouse.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/receive")]
    public async Task<IActionResult> MarkAsReceived(int id, [FromBody] ReceiveReturnRequest? request = null)
    {
        try
        {
            var returnRequest = await _returnService.MarkAsReceivedAsync(id, request?.ItemConditions);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking return {ReturnId} as received", id);
            return StatusCode(500, new { error = "Failed to mark as received" });
        }
    }

    /// <summary>
    /// Process the refund for a return.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/refund")]
    public async Task<IActionResult> ProcessRefund(int id)
    {
        try
        {
            var returnRequest = await _returnService.ProcessRefundAsync(id);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for return {ReturnId}", id);
            return StatusCode(500, new { error = "Failed to process refund" });
        }
    }

    /// <summary>
    /// Cancel a return request.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelReturn(int id, [FromBody] CancelReturnRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason))
            return BadRequest(new { error = "Cancellation reason is required" });

        try
        {
            var returnRequest = await _returnService.CancelReturnAsync(id, request.Reason);
            return Ok(returnRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling return {ReturnId}", id);
            return StatusCode(500, new { error = "Failed to cancel return" });
        }
    }

    #endregion

    #region Return Reasons (Admin)

    /// <summary>
    /// Get all return reasons (admin view).
    /// </summary>
    [Authorize]
    [HttpGet("reasons/all")]
    public async Task<IActionResult> GetAllReasons()
    {
        try
        {
            var reasons = await _returnService.GetAllReasonsAsync(_shopContext.ShopDomain);
            return Ok(reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all return reasons");
            return StatusCode(500, new { error = "Failed to get reasons" });
        }
    }

    /// <summary>
    /// Create a return reason.
    /// </summary>
    [Authorize]
    [HttpPost("reasons")]
    public async Task<IActionResult> CreateReason([FromBody] CreateReturnReasonDto dto)
    {
        try
        {
            var reason = await _returnService.CreateReasonAsync(_shopContext.ShopDomain, dto);
            return Ok(reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating return reason");
            return StatusCode(500, new { error = "Failed to create reason" });
        }
    }

    /// <summary>
    /// Update a return reason.
    /// </summary>
    [Authorize]
    [HttpPut("reasons/{id}")]
    public async Task<IActionResult> UpdateReason(int id, [FromBody] CreateReturnReasonDto dto)
    {
        try
        {
            var reason = await _returnService.UpdateReasonAsync(id, dto);
            return Ok(reason);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating return reason {ReasonId}", id);
            return StatusCode(500, new { error = "Failed to update reason" });
        }
    }

    /// <summary>
    /// Delete a return reason.
    /// </summary>
    [Authorize]
    [HttpDelete("reasons/{id}")]
    public async Task<IActionResult> DeleteReason(int id)
    {
        try
        {
            await _returnService.DeleteReasonAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting return reason {ReasonId}", id);
            return StatusCode(500, new { error = "Failed to delete reason" });
        }
    }

    /// <summary>
    /// Seed default return reasons.
    /// </summary>
    [Authorize]
    [HttpPost("reasons/seed-defaults")]
    public async Task<IActionResult> SeedDefaultReasons()
    {
        try
        {
            await _returnService.SeedDefaultReasonsAsync(_shopContext.ShopDomain);
            var reasons = await _returnService.GetAllReasonsAsync(_shopContext.ShopDomain);
            return Ok(reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default reasons");
            return StatusCode(500, new { error = "Failed to seed default reasons" });
        }
    }

    #endregion

    #region Settings (Admin)

    /// <summary>
    /// Get return settings.
    /// </summary>
    [Authorize]
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await _returnService.GetSettingsAsync(_shopContext.ShopDomain);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update return settings.
    /// </summary>
    [Authorize]
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateReturnSettingsDto dto)
    {
        try
        {
            var settings = await _returnService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating return settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    #endregion

    #region Shipping (Admin)

    /// <summary>
    /// Get shipping rates for a return.
    /// </summary>
    [Authorize]
    [HttpPost("shipping/rates")]
    public async Task<IActionResult> GetShippingRates([FromBody] GetRatesRequest request)
    {
        try
        {
            var rates = await _shippoService.GetRatesAsync(
                _shopContext.ShopDomain, request.FromAddress, request.ToAddress);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipping rates");
            return StatusCode(500, new { error = "Failed to get rates" });
        }
    }

    /// <summary>
    /// Void a shipping label.
    /// </summary>
    [Authorize]
    [HttpPost("labels/{id}/void")]
    public async Task<IActionResult> VoidLabel(int id)
    {
        try
        {
            var success = await _shippoService.VoidLabelAsync(id);
            if (!success)
                return BadRequest(new { error = "Failed to void label" });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding label {LabelId}", id);
            return StatusCode(500, new { error = "Failed to void label" });
        }
    }

    #endregion
}

#region Request DTOs

public record CheckEligibilityRequest
{
    public string Shop { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record SubmitReturnRequest
{
    public string Shop { get; init; } = string.Empty;
    public CreateReturnRequestDto Data { get; init; } = new();
}

public record ApproveReturnRequest
{
    public string? Note { get; init; }
}

public record RejectReturnRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record MarkShippedRequest
{
    public string? TrackingNumber { get; init; }
    public string? Carrier { get; init; }
}

public record ReceiveReturnRequest
{
    public List<ReturnItemConditionDto>? ItemConditions { get; init; }
}

public record CancelReturnRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record GetRatesRequest
{
    public ReturnAddressDto FromAddress { get; init; } = new();
    public ReturnAddressDto ToAddress { get; init; } = new();
}

#endregion
