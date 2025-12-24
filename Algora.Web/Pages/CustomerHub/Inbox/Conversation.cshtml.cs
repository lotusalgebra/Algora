using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Inbox;

[Authorize]
public class ConversationModel : PageModel
{
    private readonly IUnifiedInboxService _inboxService;
    private readonly IAiResponseService _aiResponseService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ConversationModel> _logger;

    public ConversationModel(
        IUnifiedInboxService inboxService,
        IAiResponseService aiResponseService,
        IShopContext shopContext,
        ILogger<ConversationModel> logger)
    {
        _inboxService = inboxService;
        _aiResponseService = aiResponseService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ConversationThreadDto? Conversation { get; set; }
    public List<ConversationMessageDto> Messages { get; set; } = new();
    public List<AiSuggestionDto> AiSuggestions { get; set; } = new();
    public List<QuickReplyDto> QuickReplies { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string? ReplyContent { get; set; }

    [BindProperty]
    public string? NewStatus { get; set; }

    [BindProperty]
    public string? NewPriority { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadDataAsync(id);
        if (Conversation == null)
        {
            return NotFound();
        }

        // Mark as read when viewing
        try
        {
            await _inboxService.MarkAsReadAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark conversation {Id} as read", id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendReplyAsync(int id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ReplyContent))
            {
                ErrorMessage = "Please enter a message.";
                await LoadDataAsync(id);
                return Page();
            }

            await LoadDataAsync(id);
            if (Conversation == null)
            {
                return NotFound();
            }

            var sendDto = new SendMessageDto(
                ReplyContent,
                Conversation.Channel,
                null,
                "text",
                false
            );

            await _inboxService.SendMessageAsync(id, sendDto);
            SuccessMessage = "Reply sent successfully.";
            ReplyContent = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reply to conversation {Id}", id);
            ErrorMessage = "Failed to send reply.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id)
    {
        try
        {
            if (!string.IsNullOrEmpty(NewStatus))
            {
                await _inboxService.UpdateConversationStatusAsync(id, NewStatus);
                SuccessMessage = $"Status updated to {NewStatus}.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for conversation {Id}", id);
            ErrorMessage = "Failed to update status.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync(int id)
    {
        try
        {
            if (!string.IsNullOrEmpty(NewPriority))
            {
                await _inboxService.UpdateConversationPriorityAsync(id, NewPriority);
                SuccessMessage = $"Priority updated to {NewPriority}.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating priority for conversation {Id}", id);
            ErrorMessage = "Failed to update priority.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateSuggestionsAsync(int id)
    {
        try
        {
            AiSuggestions = (await _aiResponseService.GenerateSuggestionsAsync(id, 3)).ToList();
            SuccessMessage = $"Generated {AiSuggestions.Count} AI suggestions.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI suggestions for conversation {Id}", id);
            ErrorMessage = "Failed to generate AI suggestions.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUseSuggestionAsync(int id, int suggestionId)
    {
        try
        {
            var suggestion = await _aiResponseService.AcceptSuggestionAsync(suggestionId, false);
            ReplyContent = suggestion.SuggestionText;
            SuccessMessage = "Suggestion applied to reply. You can edit before sending.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using suggestion {SuggestionId}", suggestionId);
            ErrorMessage = "Failed to use suggestion.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUseQuickReplyAsync(int id, int quickReplyId)
    {
        try
        {
            var quickReplies = await _inboxService.GetQuickRepliesAsync(_shopContext.ShopDomain);
            var quickReply = quickReplies.FirstOrDefault(q => q.Id == quickReplyId);
            if (quickReply != null)
            {
                ReplyContent = quickReply.Content;
                await _inboxService.IncrementQuickReplyUsageAsync(quickReplyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using quick reply {QuickReplyId}", quickReplyId);
            ErrorMessage = "Failed to use quick reply.";
        }

        await LoadDataAsync(id);
        return Page();
    }

    private async Task LoadDataAsync(int id)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            Conversation = await _inboxService.GetConversationAsync(id);
            if (Conversation != null)
            {
                Messages = (await _inboxService.GetMessagesAsync(id)).OrderBy(m => m.SentAt).ToList();
                QuickReplies = (await _inboxService.GetQuickRepliesAsync(shopDomain))
                    .Where(q => q.IsActive)
                    .OrderByDescending(q => q.UsageCount)
                    .Take(10)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading conversation {Id}", id);
            ErrorMessage = "Failed to load conversation.";
        }
    }

    public string GetChannelIcon(string channel) => channel.ToLower() switch
    {
        "email" => "fas fa-envelope",
        "sms" => "fas fa-sms",
        "whatsapp" => "fab fa-whatsapp",
        "facebook" => "fab fa-facebook-messenger",
        "instagram" => "fab fa-instagram",
        _ => "fas fa-comment"
    };

    public string GetChannelColor(string channel) => channel.ToLower() switch
    {
        "email" => "from-blue-600 to-cyan-400",
        "sms" => "from-green-600 to-lime-400",
        "whatsapp" => "from-green-500 to-green-400",
        "facebook" => "from-blue-700 to-blue-500",
        "instagram" => "from-pink-600 to-purple-500",
        _ => "from-gray-600 to-gray-400"
    };

    public string GetStatusColor(string status) => status.ToLower() switch
    {
        "open" => "bg-green-100 text-green-700",
        "pending" => "bg-yellow-100 text-yellow-700",
        "resolved" => "bg-blue-100 text-blue-700",
        "closed" => "bg-gray-100 text-gray-700",
        _ => "bg-gray-100 text-gray-700"
    };

    public string GetPriorityColor(string priority) => priority.ToLower() switch
    {
        "urgent" => "bg-red-100 text-red-700",
        "high" => "bg-orange-100 text-orange-700",
        "normal" => "bg-blue-100 text-blue-700",
        "low" => "bg-gray-100 text-gray-700",
        _ => "bg-gray-100 text-gray-700"
    };
}
