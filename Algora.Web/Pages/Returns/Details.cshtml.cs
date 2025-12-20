using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IReturnService returnService,
        IShopContext shopContext,
        ILogger<DetailsModel> logger)
    {
        _returnService = returnService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ReturnRequestDto? Return { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Return = await _returnService.GetReturnRequestAsync(id);

            if (Return == null)
            {
                ErrorMessage = "Return request not found.";
                return Page();
            }

            if (Return.ShopDomain != _shopContext.ShopDomain)
            {
                ErrorMessage = "Return request not found.";
                Return = null;
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return details for {ReturnId}", id);
            ErrorMessage = "Failed to load return details.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, string? note)
    {
        try
        {
            await _returnService.ApproveReturnAsync(id, note);
            SuccessMessage = "Return approved successfully. A shipping label has been generated.";
            Return = await _returnService.GetReturnRequestAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving return {ReturnId}", id);
            ErrorMessage = "Failed to approve return: " + ex.Message;
            Return = await _returnService.GetReturnRequestAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ErrorMessage = "A rejection reason is required.";
            Return = await _returnService.GetReturnRequestAsync(id);
            return Page();
        }

        try
        {
            await _returnService.RejectReturnAsync(id, reason);
            SuccessMessage = "Return rejected successfully.";
            Return = await _returnService.GetReturnRequestAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting return {ReturnId}", id);
            ErrorMessage = "Failed to reject return: " + ex.Message;
            Return = await _returnService.GetReturnRequestAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMarkReceivedAsync(int id)
    {
        try
        {
            await _returnService.MarkAsReceivedAsync(id);
            SuccessMessage = "Return marked as received.";
            Return = await _returnService.GetReturnRequestAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking return {ReturnId} as received", id);
            ErrorMessage = "Failed to mark return as received: " + ex.Message;
            Return = await _returnService.GetReturnRequestAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostProcessRefundAsync(int id)
    {
        try
        {
            await _returnService.ProcessRefundAsync(id);
            SuccessMessage = "Refund processed successfully.";
            Return = await _returnService.GetReturnRequestAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for return {ReturnId}", id);
            ErrorMessage = "Failed to process refund: " + ex.Message;
            Return = await _returnService.GetReturnRequestAsync(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ErrorMessage = "A cancellation reason is required.";
            Return = await _returnService.GetReturnRequestAsync(id);
            return Page();
        }

        try
        {
            await _returnService.CancelReturnAsync(id, reason);
            SuccessMessage = "Return cancelled successfully.";
            Return = await _returnService.GetReturnRequestAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling return {ReturnId}", id);
            ErrorMessage = "Failed to cancel return: " + ex.Message;
            Return = await _returnService.GetReturnRequestAsync(id);
        }

        return Page();
    }

    public string GetStatusBadgeClass(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "from-yellow-500 to-orange-400",
            "approved" => "from-blue-600 to-cyan-400",
            "shipped" => "from-purple-600 to-indigo-400",
            "received" => "from-indigo-600 to-blue-400",
            "refunded" => "from-green-600 to-lime-400",
            "rejected" => "from-red-600 to-pink-400",
            "cancelled" => "from-gray-400 to-gray-600",
            _ => "from-gray-400 to-gray-600"
        };
    }
}
