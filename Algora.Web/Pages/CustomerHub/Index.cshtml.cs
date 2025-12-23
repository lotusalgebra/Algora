using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUnifiedInboxService _inboxService;
    private readonly IExchangeService _exchangeService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUnifiedInboxService inboxService,
        IExchangeService exchangeService,
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _inboxService = inboxService;
        _exchangeService = exchangeService;
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public int OpenConversationCount { get; set; }
    public int UnreadMessageCount { get; set; }
    public int PendingExchangeCount { get; set; }
    public int LoyaltyMemberCount { get; set; }
    public int ActiveMemberCount { get; set; }
    public long PointsIssuedThisMonth { get; set; }
    public List<ConversationThreadDto> RecentConversations { get; set; } = new();
    public List<ExchangeDto> RecentExchanges { get; set; } = new();
    public LoyaltyProgramDto? LoyaltyProgram { get; set; }
    public List<LoyaltyTierDto> LoyaltyTiers { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            // Get inbox summary
            var inboxSummary = await _inboxService.GetInboxSummaryAsync(shopDomain);
            OpenConversationCount = inboxSummary.OpenConversations;
            UnreadMessageCount = inboxSummary.UnreadMessages;

            // Get recent conversations
            var conversations = await _inboxService.GetConversationsAsync(shopDomain, new ConversationFilterDto { Take = 5 });
            RecentConversations = conversations.ToList();

            // Get pending exchanges
            var exchanges = await _exchangeService.GetExchangesAsync(shopDomain, new ExchangeFilterDto { Status = "pending", Take = 10 });
            PendingExchangeCount = exchanges.Count();
            RecentExchanges = exchanges.Take(5).ToList();

            // Get loyalty program info
            LoyaltyProgram = await _loyaltyService.GetProgramAsync(shopDomain);
            if (LoyaltyProgram != null)
            {
                LoyaltyTiers = (await _loyaltyService.GetTiersAsync(LoyaltyProgram.Id)).ToList();

                // Get loyalty analytics
                var analytics = await _loyaltyService.GetAnalyticsAsync(shopDomain, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
                LoyaltyMemberCount = analytics.TotalMembers;
                ActiveMemberCount = analytics.ActiveMembers;
                PointsIssuedThisMonth = analytics.TotalPointsIssued;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Customer Hub dashboard");
            ErrorMessage = "Failed to load dashboard data. Please try again.";
        }
    }
}
