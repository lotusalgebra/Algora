using Algora.Application.DTOs.Admin;
using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Admin
{
    [Authorize]
    public class ClientsModel : PageModel
    {
        private readonly IClientService _clientService;
        private readonly IPlanService _planService;
        private readonly ILogger<ClientsModel> _logger;

        public ClientsModel(
            IClientService clientService,
            IPlanService planService,
            ILogger<ClientsModel> logger)
        {
            _clientService = clientService;
            _planService = planService;
            _logger = logger;
        }

        public ClientListResultDto ClientResult { get; set; } = new();
        public ClientStatsDto Stats { get; set; } = new();
        public IEnumerable<PlanDto> AllPlans { get; set; } = [];
        public IEnumerable<string> AvailablePlans { get; set; } = [];

        public string? SearchTerm { get; set; }
        public string? SelectedPlan { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(string? search, string? plan, string? status, int page = 1)
        {
            try
            {
                SearchTerm = search;
                SelectedPlan = plan;
                SelectedStatus = status;

                var filter = new ClientFilterDto
                {
                    SearchTerm = search,
                    PlanName = plan,
                    IsActive = status switch
                    {
                        "active" => true,
                        "inactive" => false,
                        _ => null
                    },
                    Page = page,
                    PageSize = 25
                };

                ClientResult = await _clientService.GetClientsAsync(filter);
                Stats = await _clientService.GetClientStatsAsync();
                AllPlans = await _planService.GetAllPlansAsync();
                AvailablePlans = await _clientService.GetActivePlanNamesAsync();

                if (Request.Query.ContainsKey("updated"))
                {
                    SuccessMessage = "Client plan updated successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load clients");
                ErrorMessage = "Failed to load clients. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostChangePlanAsync(string shopDomain, string newPlanName, string? adminNotes)
        {
            try
            {
                var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";

                var dto = new UpdateClientPlanDto
                {
                    ShopDomain = shopDomain,
                    NewPlanName = newPlanName,
                    AdminNotes = adminNotes
                };

                var success = await _clientService.UpdateClientPlanAsync(dto, adminEmail);

                if (success)
                {
                    _logger.LogInformation("Admin {Admin} changed plan for {Shop} to {Plan}",
                        adminEmail, shopDomain, newPlanName);
                    return RedirectToPage(new { updated = true });
                }

                ErrorMessage = "Failed to update client plan.";
                await LoadPageDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change plan for {ShopDomain}", shopDomain);
                ErrorMessage = "An error occurred. Please try again.";
                await LoadPageDataAsync();
                return Page();
            }
        }

        private async Task LoadPageDataAsync()
        {
            var filter = new ClientFilterDto { Page = 1, PageSize = 25 };
            ClientResult = await _clientService.GetClientsAsync(filter);
            Stats = await _clientService.GetClientStatsAsync();
            AllPlans = await _planService.GetAllPlansAsync();
            AvailablePlans = await _clientService.GetActivePlanNamesAsync();
        }
    }
}
