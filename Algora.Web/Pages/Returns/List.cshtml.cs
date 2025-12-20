using Algora.Application.DTOs.Inventory;
using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

[Authorize]
public class ListModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ListModel> _logger;

    public ListModel(
        IReturnService returnService,
        IShopContext shopContext,
        ILogger<ListModel> logger)
    {
        _returnService = returnService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public PaginatedResult<ReturnRequestDto> Returns { get; set; } = new();
    public string? ErrorMessage { get; set; }

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
        try
        {
            Returns = await _returnService.GetReturnRequestsAsync(
                _shopContext.ShopDomain,
                Status,
                Search,
                StartDate,
                EndDate,
                Page,
                PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading returns list");
            ErrorMessage = "Failed to load returns. Please try again.";
        }
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
