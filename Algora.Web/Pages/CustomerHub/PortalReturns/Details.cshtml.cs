using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.PortalReturns;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IPortalReturnAdminService? _portalReturnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IShopContext shopContext,
        ILogger<DetailsModel> logger,
        IPortalReturnAdminService? portalReturnService = null)
    {
        _shopContext = shopContext;
        _logger = logger;
        _portalReturnService = portalReturnService;
    }

    public PortalReturnDetailDto? ReturnRequest { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsConfigured => _portalReturnService != null;

    [BindProperty]
    public string? AdminNotes { get; set; }

    [BindProperty]
    public string? ReturnLabelUrl { get; set; }

    [BindProperty]
    public decimal? RefundAmount { get; set; }

    [BindProperty]
    public string? RejectReason { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured.";
            return Page();
        }

        ReturnRequest = await _portalReturnService.GetReturnRequestByIdAsync(_shopContext.ShopDomain, id);

        if (ReturnRequest == null)
        {
            return NotFound();
        }

        // Pre-populate form fields
        AdminNotes = ReturnRequest.AdminNotes;
        ReturnLabelUrl = ReturnRequest.ReturnLabelUrl;
        RefundAmount = ReturnRequest.RefundAmount ?? ReturnRequest.Items.Sum(i => i.TotalPrice);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured.";
            return await ReloadPage(id);
        }

        var success = await _portalReturnService.ApproveReturnRequestAsync(
            _shopContext.ShopDomain,
            id,
            AdminNotes,
            ReturnLabelUrl);

        if (success)
        {
            SuccessMessage = "Return request has been approved.";
        }
        else
        {
            ErrorMessage = "Failed to approve the return request. It may not be in pending status.";
        }

        return await ReloadPage(id);
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured.";
            return await ReloadPage(id);
        }

        if (string.IsNullOrWhiteSpace(RejectReason))
        {
            ErrorMessage = "Please provide a reason for rejection.";
            return await ReloadPage(id);
        }

        var success = await _portalReturnService.RejectReturnRequestAsync(
            _shopContext.ShopDomain,
            id,
            RejectReason);

        if (success)
        {
            SuccessMessage = "Return request has been rejected.";
        }
        else
        {
            ErrorMessage = "Failed to reject the return request.";
        }

        return await ReloadPage(id);
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured.";
            return await ReloadPage(id);
        }

        if (!RefundAmount.HasValue || RefundAmount.Value <= 0)
        {
            ErrorMessage = "Please enter a valid refund amount.";
            return await ReloadPage(id);
        }

        var success = await _portalReturnService.CompleteReturnRequestAsync(
            _shopContext.ShopDomain,
            id,
            RefundAmount.Value,
            AdminNotes);

        if (success)
        {
            SuccessMessage = $"Return request has been completed with a refund of {RefundAmount.Value:C}.";
        }
        else
        {
            ErrorMessage = "Failed to complete the return request. It may not be in the correct status.";
        }

        return await ReloadPage(id);
    }

    public async Task<IActionResult> OnPostUpdateNotesAsync(int id)
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured.";
            return await ReloadPage(id);
        }

        var success = await _portalReturnService.UpdateReturnRequestAsync(
            _shopContext.ShopDomain,
            id,
            new UpdatePortalReturnDto
            {
                AdminNotes = AdminNotes,
                ReturnLabelUrl = ReturnLabelUrl
            });

        if (success)
        {
            SuccessMessage = "Notes and label URL have been updated.";
        }
        else
        {
            ErrorMessage = "Failed to update the return request.";
        }

        return await ReloadPage(id);
    }

    private async Task<IActionResult> ReloadPage(int id)
    {
        if (_portalReturnService != null)
        {
            ReturnRequest = await _portalReturnService.GetReturnRequestByIdAsync(_shopContext.ShopDomain, id);
        }
        return Page();
    }

    public string GetStatusBadgeClass(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "from-yellow-500 to-orange-400",
            "approved" => "from-blue-600 to-cyan-400",
            "processing" => "from-purple-600 to-indigo-400",
            "completed" => "from-green-600 to-lime-400",
            "rejected" => "from-red-600 to-pink-400",
            "cancelled" => "from-gray-400 to-gray-600",
            _ => "from-gray-400 to-gray-600"
        };
    }
}
