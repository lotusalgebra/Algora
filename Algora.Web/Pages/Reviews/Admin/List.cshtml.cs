using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reviews.Admin;

[Authorize]
public class ListModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ListModel> _logger;

    public ListModel(
        IReviewService reviewService,
        IShopContext shopContext,
        ILogger<ListModel> logger)
    {
        _reviewService = reviewService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public PaginatedResult<ReviewListDto> Reviews { get; set; } = new() { Items = [] };

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Source { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinRating { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var filter = new ReviewFilterDto
            {
                Status = Status,
                Source = Source,
                MinRating = MinRating,
                Search = Search,
                Page = Page,
                PageSize = 20,
                SortDescending = true
            };

            Reviews = await _reviewService.GetReviewsAsync(_shopContext.ShopDomain, filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reviews list");
            ErrorMessage = "Failed to load reviews. Please try again.";
        }
    }
}
