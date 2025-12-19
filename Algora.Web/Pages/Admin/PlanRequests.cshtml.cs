using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Admin
{
    [Authorize]
    public class PlanRequestsModel : PageModel
    {
        private readonly IPlanService _planService;
        private readonly ILogger<PlanRequestsModel> _logger;

        public PlanRequestsModel(
            IPlanService planService,
            ILogger<PlanRequestsModel> logger)
        {
            _planService = planService;
            _logger = logger;
        }

        public IEnumerable<PlanChangeRequestDto> PendingRequests { get; set; } = [];
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                PendingRequests = await _planService.GetPendingRequestsAsync();

                if (Request.Query.ContainsKey("approved"))
                {
                    SuccessMessage = "Request approved successfully.";
                }
                else if (Request.Query.ContainsKey("rejected"))
                {
                    SuccessMessage = "Request rejected successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending plan requests");
                ErrorMessage = "Failed to load requests. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? adminNotes)
        {
            try
            {
                var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";
                var success = await _planService.ApproveRequestAsync(requestId, adminEmail, adminNotes);

                if (success)
                {
                    return RedirectToPage(new { approved = true });
                }

                ErrorMessage = "Failed to approve request.";
                await LoadPendingRequestsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve request {RequestId}", requestId);
                ErrorMessage = "An error occurred. Please try again.";
                await LoadPendingRequestsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? adminNotes)
        {
            try
            {
                var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";
                var success = await _planService.RejectRequestAsync(requestId, adminEmail, adminNotes);

                if (success)
                {
                    return RedirectToPage(new { rejected = true });
                }

                ErrorMessage = "Failed to reject request.";
                await LoadPendingRequestsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject request {RequestId}", requestId);
                ErrorMessage = "An error occurred. Please try again.";
                await LoadPendingRequestsAsync();
                return Page();
            }
        }

        private async Task LoadPendingRequestsAsync()
        {
            PendingRequests = await _planService.GetPendingRequestsAsync();
        }
    }
}
