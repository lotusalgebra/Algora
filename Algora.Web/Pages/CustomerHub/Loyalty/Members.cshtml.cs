using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Loyalty;

[Authorize]
public class MembersModel : PageModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<MembersModel> _logger;
    private const int PageSize = 20;

    public MembersModel(
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<MembersModel> logger)
    {
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public LoyaltyProgramDto? Program { get; set; }
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
    public List<CustomerLoyaltyDto> Members { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalMembers { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterTier { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "points_desc";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public AdjustPointsFormModel AdjustForm { get; set; } = new();

    public class AdjustPointsFormModel
    {
        public int CustomerId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAdjustPointsAsync()
    {
        try
        {
            var dto = new AdjustPointsDto(AdjustForm.Points, AdjustForm.Reason);
            await _loyaltyService.AdjustPointsAsync(AdjustForm.CustomerId, dto);
            SuccessMessage = $"Successfully {(AdjustForm.Points > 0 ? "added" : "deducted")} {Math.Abs(AdjustForm.Points)} points.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting points for customer {CustomerId}", AdjustForm.CustomerId);
            ErrorMessage = "Failed to adjust points.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEvaluateTiersAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            await _loyaltyService.EvaluateTiersAsync(shopDomain);
            SuccessMessage = "Tier evaluation completed. Members have been updated to their correct tiers.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating tiers");
            ErrorMessage = "Failed to evaluate tiers.";
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;

            Program = await _loyaltyService.GetProgramAsync(shopDomain);
            if (Program == null) return;

            Tiers = (await _loyaltyService.GetTiersAsync(Program.Id)).ToList();

            // Get analytics for total count
            var analytics = await _loyaltyService.GetAnalyticsAsync(shopDomain);
            TotalMembers = analytics.TotalMembers;

            // Get top members (the service returns sorted by lifetime points)
            // In a real implementation, we'd have a proper paginated search endpoint
            var allMembers = await _loyaltyService.GetTopMembersAsync(shopDomain, 1000);
            var filteredMembers = allMembers.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.ToLower();
                filteredMembers = filteredMembers.Where(m =>
                    (m.CustomerName?.ToLower().Contains(search) ?? false) ||
                    (m.CustomerEmail?.ToLower().Contains(search) ?? false));
            }

            // Apply tier filter
            if (!string.IsNullOrWhiteSpace(FilterTier) && FilterTier != "all")
            {
                if (FilterTier == "none")
                {
                    filteredMembers = filteredMembers.Where(m => m.CurrentTierId == null);
                }
                else if (int.TryParse(FilterTier, out var tierId))
                {
                    filteredMembers = filteredMembers.Where(m => m.CurrentTierId == tierId);
                }
            }

            // Apply sorting
            filteredMembers = SortBy switch
            {
                "points_asc" => filteredMembers.OrderBy(m => m.PointsBalance),
                "points_desc" => filteredMembers.OrderByDescending(m => m.PointsBalance),
                "lifetime_asc" => filteredMembers.OrderBy(m => m.LifetimePoints),
                "lifetime_desc" => filteredMembers.OrderByDescending(m => m.LifetimePoints),
                "joined_asc" => filteredMembers.OrderBy(m => m.JoinedAt),
                "joined_desc" => filteredMembers.OrderByDescending(m => m.JoinedAt),
                "name_asc" => filteredMembers.OrderBy(m => m.CustomerName),
                "name_desc" => filteredMembers.OrderByDescending(m => m.CustomerName),
                _ => filteredMembers.OrderByDescending(m => m.PointsBalance)
            };

            var membersList = filteredMembers.ToList();
            TotalMembers = membersList.Count;
            TotalPages = (int)Math.Ceiling((double)TotalMembers / PageSize);

            // Apply pagination
            Members = membersList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loyalty members");
            ErrorMessage = "Failed to load members.";
        }
    }

    public string GetTierColor(int? tierId)
    {
        if (tierId == null) return "#6b7280";
        return Tiers.FirstOrDefault(t => t.Id == tierId)?.Color ?? "#6b7280";
    }

    public string GetTierName(int? tierId)
    {
        if (tierId == null) return "No Tier";
        return Tiers.FirstOrDefault(t => t.Id == tierId)?.Name ?? "Unknown";
    }
}
