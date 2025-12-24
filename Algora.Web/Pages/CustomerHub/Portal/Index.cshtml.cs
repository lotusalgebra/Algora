using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Algora.Domain.Entities.Customer;
using OrderEntity = Algora.Domain.Entities.Order;

namespace Algora.Web.Pages.CustomerHub.Portal;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        AppDbContext context,
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public CustomerEntity? Customer { get; set; }
    public List<OrderEntity> RecentOrders { get; set; } = new();
    public CustomerLoyaltyDto? LoyaltyInfo { get; set; }
    public LoyaltyProgramDto? LoyaltyProgram { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            return Page();
        }

        await LoadCustomerDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostLookupAsync(string email)
    {
        Email = email;
        await LoadCustomerDataAsync();
        return Page();
    }

    private async Task LoadCustomerDataAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            Customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == Email);

            if (Customer == null)
            {
                ErrorMessage = "No account found with that email address.";
                return;
            }

            // Load order statistics
            var orderStats = await _context.Orders
                .Where(o => o.ShopDomain == shopDomain && o.CustomerId == Customer.Id)
                .GroupBy(o => 1)
                .Select(g => new { Count = g.Count(), Total = g.Sum(o => o.GrandTotal) })
                .FirstOrDefaultAsync();

            TotalOrders = orderStats?.Count ?? 0;
            TotalSpent = orderStats?.Total ?? 0;

            // Load recent orders
            RecentOrders = await _context.Orders
                .Where(o => o.ShopDomain == shopDomain && o.CustomerId == Customer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Load loyalty info
            LoyaltyProgram = await _loyaltyService.GetProgramAsync(shopDomain);
            if (LoyaltyProgram != null)
            {
                LoyaltyInfo = await _loyaltyService.GetMemberAsync(Customer.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer portal data for {Email}", Email);
            ErrorMessage = "Unable to load your account information. Please try again.";
        }
    }
}
