using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

/// <summary>
/// Customer-facing return status tracking page.
/// This page is PUBLIC (no authorization) - customers access via link or direct URL.
/// </summary>
public class StatusModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly ILogger<StatusModel> _logger;

    public StatusModel(
        IReturnService returnService,
        ILogger<StatusModel> logger)
    {
        _returnService = returnService;
        _logger = logger;
    }

    public string? ShopDomain { get; set; }
    public string? ErrorMessage { get; set; }
    public ReturnSettingsDto? Settings { get; set; }
    public ReturnRequestDto? ReturnRequest { get; set; }

    public async Task<IActionResult> OnGetAsync(string requestNumber, string? shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            ErrorMessage = "Shop domain is required.";
            return Page();
        }

        if (string.IsNullOrEmpty(requestNumber))
        {
            ErrorMessage = "Return request number is required.";
            return Page();
        }

        ShopDomain = shop;

        try
        {
            Settings = await _returnService.GetSettingsAsync(shop);
            ReturnRequest = await _returnService.GetReturnRequestByNumberAsync(shop, requestNumber);

            if (ReturnRequest == null)
            {
                ErrorMessage = "Return request not found. Please check your return number.";
                return Page();
            }

            _logger.LogInformation("Customer viewed return status for {RequestNumber}", requestNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return status for {RequestNumber}", requestNumber);
            ErrorMessage = "Unable to load return status.";
        }

        return Page();
    }

    public string GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "yellow",
            "approved" => "blue",
            "shipped" => "purple",
            "received" => "indigo",
            "refunded" => "green",
            "rejected" => "red",
            "cancelled" => "gray",
            _ => "gray"
        };
    }

    public string GetStatusLabel(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "Pending Review",
            "approved" => "Approved",
            "shipped" => "In Transit",
            "received" => "Received",
            "refunded" => "Refunded",
            "rejected" => "Rejected",
            "cancelled" => "Cancelled",
            _ => status
        };
    }

    public string GetStatusDescription(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "Your return request is being reviewed by our team.",
            "approved" => "Your return has been approved. Please ship your items using the provided label.",
            "shipped" => "Your return is on its way back to us.",
            "received" => "We've received your return and are processing it.",
            "refunded" => "Your refund has been processed.",
            "rejected" => "Your return request was not approved.",
            "cancelled" => "This return has been cancelled.",
            _ => ""
        };
    }

    public int GetStepNumber(string status)
    {
        return status.ToLower() switch
        {
            "pending" => 1,
            "approved" => 2,
            "shipped" => 3,
            "received" => 4,
            "refunded" => 5,
            _ => 0
        };
    }
}
