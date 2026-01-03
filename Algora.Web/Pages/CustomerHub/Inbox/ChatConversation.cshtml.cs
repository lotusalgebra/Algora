using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Inbox;

[Authorize]
public class ChatConversationModel : PageModel
{
    private readonly IChatbotBridgeService _chatbotBridge;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ChatConversationModel> _logger;

    public ChatConversationModel(
        IChatbotBridgeService chatbotBridge,
        IShopContext shopContext,
        ILogger<ChatConversationModel> logger)
    {
        _chatbotBridge = chatbotBridge;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ChatbotConversationDetailDto? Conversation { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string? ReplyMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            Conversation = await _chatbotBridge.GetConversationAsync(id, shopDomain);

            if (Conversation == null)
            {
                return NotFound();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat conversation {Id}", id);
            ErrorMessage = "Failed to load conversation";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSendReplyAsync(int id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ReplyMessage))
            {
                ErrorMessage = "Please enter a message.";
                await LoadConversationAsync(id);
                return Page();
            }

            var shopDomain = _shopContext.ShopDomain;
            var agentEmail = User.Identity?.Name ?? "agent@unknown.com";

            var message = new SendAgentMessageDto(
                ReplyMessage,
                agentEmail,
                null
            );

            var success = await _chatbotBridge.SendAgentMessageAsync(id, shopDomain, message);

            if (success)
            {
                SuccessMessage = "Message sent successfully.";
                ReplyMessage = null;
            }
            else
            {
                ErrorMessage = "Failed to send message.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reply to chat conversation {Id}", id);
            ErrorMessage = "Failed to send message.";
        }

        await LoadConversationAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            var success = await _chatbotBridge.ResolveConversationAsync(id, shopDomain);

            if (success)
            {
                SuccessMessage = "Conversation resolved successfully.";
            }
            else
            {
                ErrorMessage = "Failed to resolve conversation.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving chat conversation {Id}", id);
            ErrorMessage = "Failed to resolve conversation.";
        }

        await LoadConversationAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            var agentEmail = User.Identity?.Name ?? "agent@unknown.com";

            var success = await _chatbotBridge.AssignAgentAsync(id, shopDomain, agentEmail);

            if (success)
            {
                SuccessMessage = "Conversation assigned to you.";
            }
            else
            {
                ErrorMessage = "Failed to assign conversation.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning chat conversation {Id}", id);
            ErrorMessage = "Failed to assign conversation.";
        }

        await LoadConversationAsync(id);
        return Page();
    }

    private async Task LoadConversationAsync(int id)
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            Conversation = await _chatbotBridge.GetConversationAsync(id, shopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat conversation {Id}", id);
        }
    }

    public string GetRoleDisplayName(string role) => role.ToLower() switch
    {
        "user" => "Customer",
        "assistant" => "Bot",
        "agent" => "You",
        "system" => "System",
        _ => role
    };

    public string GetRoleColor(string role) => role.ToLower() switch
    {
        "user" => "bg-slate-100 text-slate-700",
        "assistant" => "bg-fuchsia-50 text-fuchsia-700",
        "agent" => "bg-blue-50 text-blue-700",
        "system" => "bg-amber-50 text-amber-700",
        _ => "bg-gray-100 text-gray-700"
    };

    public string GetRoleIcon(string role) => role.ToLower() switch
    {
        "user" => "fas fa-user",
        "assistant" => "fas fa-robot",
        "agent" => "fas fa-headset",
        "system" => "fas fa-info-circle",
        _ => "fas fa-comment"
    };
}
