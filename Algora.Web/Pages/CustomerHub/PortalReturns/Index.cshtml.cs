using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.PortalReturns;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPortalReturnAdminService? _portalReturnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IShopContext shopContext,
        ILogger<IndexModel> logger,
        IPortalReturnAdminService? portalReturnService = null)
    {
        _shopContext = shopContext;
        _logger = logger;
        _portalReturnService = portalReturnService;
    }

    public PortalReturnPaginatedResult Returns { get; set; } = new();
    public PortalReturnStatsDto? Stats { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsConfigured => _portalReturnService != null;

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public async Task OnGetAsync()
    {
        if (_portalReturnService == null)
        {
            ErrorMessage = "Portal database connection is not configured. Add 'Portal' connection string to appsettings.json.";
            return;
        }

        try
        {
            Returns = await _portalReturnService.GetReturnRequestsAsync(
                _shopContext.ShopDomain,
                Status,
                Search,
                StartDate,
                EndDate,
                Page,
                PageSize);

            Stats = await _portalReturnService.GetReturnStatsAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading portal returns list");
            ErrorMessage = "Failed to load portal returns. Please check the database connection.";
        }
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
