using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Inbox;

[Authorize]
[RequireFeature(FeatureCodes.UnifiedInbox)]
public class IndexModel : PageModel
{
    private readonly IUnifiedInboxService _inboxService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;
    private const int PageSize = 20;

    public IndexModel(
        IUnifiedInboxService inboxService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _inboxService = inboxService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ConversationThreadDto> Conversations { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterChannel { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;

            var filter = new ConversationFilterDto
            {
                Status = FilterStatus,
                Channel = FilterChannel,
                SearchTerm = SearchTerm,
                Skip = (CurrentPage - 1) * PageSize,
                Take = PageSize
            };

            var conversations = await _inboxService.GetConversationsAsync(shopDomain, filter);
            Conversations = conversations.ToList();

            // Get total count for pagination
            var summary = await _inboxService.GetInboxSummaryAsync(shopDomain);
            TotalCount = FilterStatus switch
            {
                "open" => summary.OpenConversations,
                "pending" => summary.PendingConversations,
                "resolved" => summary.ResolvedToday,
                _ => summary.TotalConversations
            };
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inbox");
        }
    }
}
