using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Inbox;

[Authorize]
public class LiveChatModel : PageModel
{
    private readonly IChatbotBridgeService _chatbotBridge;
    private readonly IShopContext _shopContext;
    private readonly ILogger<LiveChatModel> _logger;
    private const int PageSize = 20;

    public LiveChatModel(
        IChatbotBridgeService chatbotBridge,
        IShopContext shopContext,
        ILogger<LiveChatModel> logger)
    {
        _chatbotBridge = chatbotBridge;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ChatbotConversationDto> Conversations { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public int EscalatedCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;

            ChatbotConversationListResult result;

            if (FilterStatus == "escalated")
            {
                result = await _chatbotBridge.GetEscalatedConversationsAsync(shopDomain, CurrentPage, PageSize);
            }
            else
            {
                result = await _chatbotBridge.GetConversationsAsync(shopDomain, FilterStatus, CurrentPage, PageSize);
            }

            Conversations = result.Conversations;
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;

            // Get escalated count for badge
            EscalatedCount = await _chatbotBridge.GetEscalatedCountAsync(shopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading live chat conversations");
        }
    }

    public string GetStatusColor(string status) => status.ToLower() switch
    {
        "active" => "bg-green-100 text-green-700",
        "escalated" => "bg-orange-100 text-orange-700",
        "waitingforcustomer" => "bg-blue-100 text-blue-700",
        "waitingforagent" => "bg-purple-100 text-purple-700",
        "resolved" => "bg-gray-100 text-gray-600",
        "abandoned" => "bg-red-100 text-red-700",
        _ => "bg-gray-100 text-gray-600"
    };

    public string GetStatusLabel(string status) => status.ToLower() switch
    {
        "active" => "Active",
        "escalated" => "Escalated",
        "waitingforcustomer" => "Awaiting Customer",
        "waitingforagent" => "Awaiting Agent",
        "resolved" => "Resolved",
        "abandoned" => "Abandoned",
        _ => status
    };
}
