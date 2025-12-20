using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

/// <summary>
/// Customer-facing return request page.
/// This page is PUBLIC (no authorization) - customers access via link or direct URL.
/// </summary>
public class RequestModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly ILogger<RequestModel> _logger;

    public RequestModel(
        IReturnService returnService,
        ILogger<RequestModel> logger)
    {
        _returnService = returnService;
        _logger = logger;
    }

    // Page state
    public string? ShopDomain { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public ReturnSettingsDto? Settings { get; set; }

    // Step 1: Order lookup
    public CustomerReturnEligibilityDto? Eligibility { get; set; }
    public List<ReturnReasonDto> Reasons { get; set; } = new();

    // Step 2: Return submitted
    public ReturnRequestDto? SubmittedReturn { get; set; }

    // Form inputs
    [BindProperty]
    public string OrderNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string ReasonCode { get; set; } = string.Empty;

    [BindProperty]
    public string? CustomerNote { get; set; }

    [BindProperty]
    public List<ReturnItemInput> ReturnItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            ErrorMessage = "Shop domain is required.";
            return Page();
        }

        ShopDomain = shop;

        try
        {
            Settings = await _returnService.GetSettingsAsync(shop);

            if (!Settings.IsEnabled || !Settings.AllowSelfService)
            {
                ErrorMessage = "Returns are not currently available for this store.";
                return Page();
            }

            Reasons = await _returnService.GetActiveReasonsAsync(shop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return request page for shop {Shop}", shop);
            ErrorMessage = "Unable to load return portal.";
        }

        return Page();
    }

    /// <summary>
    /// Step 1: Look up order by order number and email.
    /// </summary>
    public async Task<IActionResult> OnPostLookupAsync(string? shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            ErrorMessage = "Shop domain is required.";
            return Page();
        }

        ShopDomain = shop;
        Settings = await _returnService.GetSettingsAsync(shop);
        Reasons = await _returnService.GetActiveReasonsAsync(shop);

        if (!Settings.IsEnabled || !Settings.AllowSelfService)
        {
            ErrorMessage = "Returns are not currently available.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(OrderNumber) || string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your order number and email address.";
            return Page();
        }

        try
        {
            Eligibility = await _returnService.CheckReturnEligibilityByOrderNumberAsync(
                shop, OrderNumber.Trim(), Email.Trim().ToLower());

            if (Eligibility == null)
            {
                ErrorMessage = "Order not found. Please check your order number and email address.";
                return Page();
            }

            if (!Eligibility.IsEligible)
            {
                ErrorMessage = Eligibility.IneligibleReason ?? "This order is not eligible for returns.";
                return Page();
            }

            if (!Eligibility.EligibleItems.Any())
            {
                ErrorMessage = "All items in this order have already been returned.";
                return Page();
            }

            _logger.LogInformation("Customer looked up order {OrderNumber} for return", OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up order {OrderNumber} for shop {Shop}", OrderNumber, shop);
            ErrorMessage = "Unable to look up order. Please try again.";
        }

        return Page();
    }

    /// <summary>
    /// Step 2: Submit the return request.
    /// </summary>
    public async Task<IActionResult> OnPostSubmitAsync(string? shop, int orderId)
    {
        if (string.IsNullOrEmpty(shop))
        {
            ErrorMessage = "Shop domain is required.";
            return Page();
        }

        ShopDomain = shop;
        Settings = await _returnService.GetSettingsAsync(shop);
        Reasons = await _returnService.GetActiveReasonsAsync(shop);

        if (!Settings.IsEnabled || !Settings.AllowSelfService)
        {
            ErrorMessage = "Returns are not currently available.";
            return Page();
        }

        // Validate items selected
        var itemsToReturn = ReturnItems.Where(i => i.Quantity > 0).ToList();
        if (!itemsToReturn.Any())
        {
            ErrorMessage = "Please select at least one item to return.";
            // Reload eligibility
            Eligibility = await _returnService.CheckReturnEligibilityByOrderNumberAsync(
                shop, OrderNumber, Email);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ReasonCode))
        {
            ErrorMessage = "Please select a return reason.";
            Eligibility = await _returnService.CheckReturnEligibilityByOrderNumberAsync(
                shop, OrderNumber, Email);
            return Page();
        }

        try
        {
            var createDto = new CreateReturnRequestDto
            {
                OrderId = orderId,
                OrderNumber = OrderNumber,
                CustomerEmail = Email,
                ReasonCode = ReasonCode,
                CustomerNote = CustomerNote,
                Items = itemsToReturn.Select(i => new CreateReturnItemDto
                {
                    OrderLineId = i.OrderLineId,
                    Quantity = i.Quantity,
                    ReasonCode = ReasonCode
                }).ToList()
            };

            SubmittedReturn = await _returnService.CreateReturnRequestAsync(shop, createDto);

            SuccessMessage = "Your return request has been submitted successfully!";

            _logger.LogInformation("Customer submitted return request {RequestNumber} for order {OrderNumber}",
                SubmittedReturn.RequestNumber, OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting return for order {OrderId}", orderId);
            ErrorMessage = "Unable to submit return request. Please try again.";
            Eligibility = await _returnService.CheckReturnEligibilityByOrderNumberAsync(
                shop, OrderNumber, Email);
        }

        return Page();
    }
}

public class ReturnItemInput
{
    public int OrderLineId { get; set; }
    public int Quantity { get; set; }
}
