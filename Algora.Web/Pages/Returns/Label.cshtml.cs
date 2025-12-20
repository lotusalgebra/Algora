using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

/// <summary>
/// Customer-facing return label download page.
/// This page is PUBLIC (no authorization) - customers access via link or direct URL.
/// </summary>
public class LabelModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly ILogger<LabelModel> _logger;

    public LabelModel(
        IReturnService returnService,
        ILogger<LabelModel> logger)
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
                ErrorMessage = "Return request not found.";
                return Page();
            }

            if (ReturnRequest.Label == null)
            {
                if (ReturnRequest.Status.ToLower() == "pending")
                {
                    ErrorMessage = "Your return is still pending approval. A shipping label will be available once approved.";
                }
                else if (ReturnRequest.Status.ToLower() == "rejected")
                {
                    ErrorMessage = "This return request was not approved.";
                }
                else
                {
                    ErrorMessage = "No shipping label is available for this return.";
                }
                return Page();
            }

            _logger.LogInformation("Customer viewed return label for {RequestNumber}", requestNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return label for {RequestNumber}", requestNumber);
            ErrorMessage = "Unable to load return label.";
        }

        return Page();
    }
}
