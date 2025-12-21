using Algora.Application.DTOs.Bundles;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers;

[ApiController]
[Route("api/bundles")]
public class BundlesController : ControllerBase
{
    private readonly IBundleService _bundleService;
    private readonly IBundleShopifyService _shopifyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<BundlesController> _logger;

    public BundlesController(
        IBundleService bundleService,
        IBundleShopifyService shopifyService,
        IShopContext shopContext,
        ILogger<BundlesController> logger)
    {
        _bundleService = bundleService;
        _shopifyService = shopifyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    #region Public Endpoints (Anonymous)

    /// <summary>
    /// Get all active bundles for the storefront.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveBundles()
    {
        try
        {
            var bundles = await _bundleService.GetActiveBundlesAsync(_shopContext.ShopDomain);
            return Ok(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active bundles");
            return StatusCode(500, new { error = "Failed to get bundles" });
        }
    }

    /// <summary>
    /// Get bundle details by ID or slug.
    /// </summary>
    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBundle(string idOrSlug)
    {
        try
        {
            BundleDto? bundle;

            if (int.TryParse(idOrSlug, out var id))
            {
                bundle = await _bundleService.GetBundleByIdAsync(id);
            }
            else
            {
                bundle = await _bundleService.GetBundleBySlugAsync(_shopContext.ShopDomain, idOrSlug);
            }

            if (bundle == null)
                return NotFound(new { error = "Bundle not found" });

            return Ok(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle {IdOrSlug}", idOrSlug);
            return StatusCode(500, new { error = "Failed to get bundle" });
        }
    }

    /// <summary>
    /// Calculate price for a mix-and-match bundle selection.
    /// </summary>
    [HttpPost("{id}/calculate")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculatePrice(int id, [FromBody] CustomerBundleSelectionDto selection)
    {
        try
        {
            selection.BundleId = id;
            var result = await _bundleService.CalculateBundlePriceAsync(selection);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bundle {BundleId} price", id);
            return StatusCode(500, new { error = "Failed to calculate price" });
        }
    }

    /// <summary>
    /// Generate a cart URL for bundle products.
    /// </summary>
    [HttpPost("{id}/cart-url")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateCartUrl(int id, [FromBody] CustomerBundleSelectionDto selection)
    {
        try
        {
            selection.BundleId = id;
            var result = await _bundleService.GenerateCartUrlAsync(_shopContext.ShopDomain, selection);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cart URL for bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to generate cart URL" });
        }
    }

    #endregion

    #region Admin Endpoints (Authenticated)

    /// <summary>
    /// Get paginated list of all bundles for admin.
    /// </summary>
    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetBundlesAdmin(
        [FromQuery] string? bundleType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _bundleService.GetBundlesAsync(
                _shopContext.ShopDomain, bundleType, status, search, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundles for admin");
            return StatusCode(500, new { error = "Failed to get bundles" });
        }
    }

    /// <summary>
    /// Create a new bundle.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleDto dto)
    {
        try
        {
            var bundle = await _bundleService.CreateBundleAsync(_shopContext.ShopDomain, dto);
            return CreatedAtAction(nameof(GetBundle), new { idOrSlug = bundle.Id.ToString() }, bundle);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bundle");
            return StatusCode(500, new { error = "Failed to create bundle" });
        }
    }

    /// <summary>
    /// Update an existing bundle.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateBundle(int id, [FromBody] UpdateBundleDto dto)
    {
        try
        {
            var bundle = await _bundleService.UpdateBundleAsync(id, dto);
            if (bundle == null)
                return NotFound(new { error = "Bundle not found" });
            return Ok(bundle);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to update bundle" });
        }
    }

    /// <summary>
    /// Delete a bundle.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteBundle(int id)
    {
        try
        {
            var success = await _bundleService.DeleteBundleAsync(id);
            if (!success)
                return NotFound(new { error = "Bundle not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to delete bundle" });
        }
    }

    /// <summary>
    /// Archive a bundle.
    /// </summary>
    [HttpPost("{id}/archive")]
    [Authorize]
    public async Task<IActionResult> ArchiveBundle(int id)
    {
        try
        {
            var success = await _bundleService.ArchiveBundleAsync(id);
            if (!success)
                return NotFound(new { error = "Bundle not found" });
            return Ok(new { message = "Bundle archived successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to archive bundle" });
        }
    }

    /// <summary>
    /// Sync bundle to Shopify as a product.
    /// </summary>
    [HttpPost("{id}/sync-shopify")]
    [Authorize]
    public async Task<IActionResult> SyncToShopify(int id)
    {
        try
        {
            var bundle = await _shopifyService.SyncBundleToShopifyAsync(id);
            if (bundle == null)
                return NotFound(new { error = "Bundle not found" });
            return Ok(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bundle {BundleId} to Shopify", id);
            return StatusCode(500, new { error = "Failed to sync to Shopify" });
        }
    }

    /// <summary>
    /// Remove bundle product from Shopify.
    /// </summary>
    [HttpPost("{id}/unsync-shopify")]
    [Authorize]
    public async Task<IActionResult> RemoveFromShopify(int id)
    {
        try
        {
            var success = await _shopifyService.RemoveBundleFromShopifyAsync(id);
            if (!success)
                return NotFound(new { error = "Bundle not found or not synced to Shopify" });
            return Ok(new { message = "Bundle removed from Shopify" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle {BundleId} from Shopify", id);
            return StatusCode(500, new { error = "Failed to remove from Shopify" });
        }
    }

    /// <summary>
    /// Get Shopify product URL for a synced bundle.
    /// </summary>
    [HttpGet("{id}/shopify-url")]
    [Authorize]
    public async Task<IActionResult> GetShopifyUrl(int id)
    {
        try
        {
            var url = await _shopifyService.GetShopifyProductUrlAsync(id);
            if (url == null)
                return NotFound(new { error = "Bundle not synced to Shopify" });
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Shopify URL for bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to get Shopify URL" });
        }
    }

    /// <summary>
    /// Get available quantity for a bundle.
    /// </summary>
    [HttpGet("{id}/inventory")]
    [Authorize]
    public async Task<IActionResult> GetBundleInventory(int id)
    {
        try
        {
            var quantity = await _bundleService.CalculateAvailableQuantityAsync(id);
            return Ok(new { bundleId = id, availableQuantity = quantity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for bundle {BundleId}", id);
            return StatusCode(500, new { error = "Failed to get inventory" });
        }
    }

    #endregion

    #region Bundle Items

    /// <summary>
    /// Add an item to a bundle.
    /// </summary>
    [HttpPost("{bundleId}/items")]
    [Authorize]
    public async Task<IActionResult> AddBundleItem(int bundleId, [FromBody] CreateBundleItemDto dto)
    {
        try
        {
            var item = await _bundleService.AddBundleItemAsync(bundleId, dto);
            if (item == null)
                return NotFound(new { error = "Bundle not found" });
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to bundle {BundleId}", bundleId);
            return StatusCode(500, new { error = "Failed to add item" });
        }
    }

    /// <summary>
    /// Update a bundle item.
    /// </summary>
    [HttpPut("items/{itemId}")]
    [Authorize]
    public async Task<IActionResult> UpdateBundleItem(
        int itemId,
        [FromQuery] int? quantity = null,
        [FromQuery] int? displayOrder = null)
    {
        try
        {
            var item = await _bundleService.UpdateBundleItemAsync(itemId, quantity, displayOrder);
            if (item == null)
                return NotFound(new { error = "Bundle item not found" });
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle item {ItemId}", itemId);
            return StatusCode(500, new { error = "Failed to update item" });
        }
    }

    /// <summary>
    /// Remove an item from a bundle.
    /// </summary>
    [HttpDelete("items/{itemId}")]
    [Authorize]
    public async Task<IActionResult> RemoveBundleItem(int itemId)
    {
        try
        {
            var success = await _bundleService.RemoveBundleItemAsync(itemId);
            if (!success)
                return NotFound(new { error = "Bundle item not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle item {ItemId}", itemId);
            return StatusCode(500, new { error = "Failed to remove item" });
        }
    }

    #endregion

    #region Bundle Rules

    /// <summary>
    /// Add a rule to a mix-and-match bundle.
    /// </summary>
    [HttpPost("{bundleId}/rules")]
    [Authorize]
    public async Task<IActionResult> AddBundleRule(int bundleId, [FromBody] CreateBundleRuleDto dto)
    {
        try
        {
            var rule = await _bundleService.AddBundleRuleAsync(bundleId, dto);
            if (rule == null)
                return NotFound(new { error = "Bundle not found" });
            return Ok(rule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding rule to bundle {BundleId}", bundleId);
            return StatusCode(500, new { error = "Failed to add rule" });
        }
    }

    /// <summary>
    /// Remove a rule from a bundle.
    /// </summary>
    [HttpDelete("rules/{ruleId}")]
    [Authorize]
    public async Task<IActionResult> RemoveBundleRule(int ruleId)
    {
        try
        {
            var success = await _bundleService.RemoveBundleRuleAsync(ruleId);
            if (!success)
                return NotFound(new { error = "Bundle rule not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bundle rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Failed to remove rule" });
        }
    }

    #endregion

    #region Settings

    /// <summary>
    /// Get bundle settings for the shop.
    /// </summary>
    [HttpGet("settings")]
    [Authorize]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await _bundleService.GetSettingsAsync(_shopContext.ShopDomain);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle settings");
            return StatusCode(500, new { error = "Failed to get settings" });
        }
    }

    /// <summary>
    /// Update bundle settings.
    /// </summary>
    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateBundleSettingsDto dto)
    {
        try
        {
            var settings = await _bundleService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bundle settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get bundle analytics summary.
    /// </summary>
    [HttpGet("analytics")]
    [Authorize]
    public async Task<IActionResult> GetAnalytics()
    {
        try
        {
            var analytics = await _bundleService.GetAnalyticsSummaryAsync(_shopContext.ShopDomain);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle analytics");
            return StatusCode(500, new { error = "Failed to get analytics" });
        }
    }

    /// <summary>
    /// Get performance data for a specific bundle.
    /// </summary>
    [HttpGet("{id}/performance")]
    [Authorize]
    public async Task<IActionResult> GetBundlePerformance(int id)
    {
        try
        {
            var performance = await _bundleService.GetBundlePerformanceAsync(id);
            if (performance == null)
                return NotFound(new { error = "Bundle not found" });
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle {BundleId} performance", id);
            return StatusCode(500, new { error = "Failed to get performance" });
        }
    }

    #endregion
}
