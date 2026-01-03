using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Infrastructure.Data;
using Algora.Chatbot.Web.Hubs;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Algora.Chatbot.Web.Controllers;

[ApiController]
[Route("api/widget/v1")]
[EnableCors("WidgetPolicy")]
public class WidgetApiController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ChatbotDbContext _db;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ILogger<WidgetApiController> _logger;

    public WidgetApiController(
        IChatService chatService,
        ChatbotDbContext db,
        IHubContext<ChatHub> chatHub,
        ILogger<WidgetApiController> logger)
    {
        _chatService = chatService;
        _db = db;
        _chatHub = chatHub;
        _logger = logger;
    }

    [HttpPost("conversations/start")]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationApiRequest request)
    {
        try
        {
            var conversation = await _chatService.StartConversationAsync(new StartConversationRequest
            {
                ShopDomain = request.Shop,
                SessionId = request.SessionId,
                VisitorId = request.VisitorId,
                CustomerEmail = request.Email,
                CustomerName = request.Name,
                CurrentPageUrl = request.PageUrl,
                UserAgent = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            var messages = await _chatService.GetMessagesAsync(conversation.Id);

            return Ok(new
            {
                success = true,
                conversationId = conversation.Id,
                messages = messages.Select(m => new
                {
                    role = m.Role.ToString().ToLower(),
                    content = m.Content,
                    createdAt = m.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            return Ok(new { success = false, error = "Failed to start conversation" });
        }
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageApiRequest request)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(new ChatRequest
            {
                ShopDomain = request.Shop,
                SessionId = request.SessionId,
                ConversationId = id,
                Message = request.Message,
                CustomerEmail = request.Email
            });

            // Broadcast customer message via SignalR
            await _chatHub.SendMessageToConversation(id, "user", request.Message);

            // Broadcast bot response via SignalR
            if (response.Success && !string.IsNullOrEmpty(response.Response))
            {
                await _chatHub.SendMessageToConversation(id, "assistant", response.Response);
            }

            return Ok(new
            {
                success = response.Success,
                response = response.Response,
                intent = response.Intent,
                suggestedActions = response.SuggestedActions,
                error = response.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return Ok(new { success = false, error = "Failed to send message" });
        }
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(int id)
    {
        var conversation = await _chatService.GetConversationAsync(id);
        if (conversation == null)
        {
            return NotFound(new { success = false, error = "Conversation not found" });
        }

        return Ok(new
        {
            success = true,
            conversationId = conversation.Id,
            status = conversation.Status.ToString().ToLower(),
            messages = conversation.Messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.Content,
                suggestedActions = m.SuggestedActionsJson,
                createdAt = m.CreatedAt
            })
        });
    }

    [HttpPost("conversations/{id}/end")]
    public async Task<IActionResult> EndConversation(int id, [FromBody] EndConversationApiRequest request)
    {
        try
        {
            await _chatService.EndConversationAsync(id, request.WasHelpful, request.Rating, request.Feedback);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending conversation");
            return Ok(new { success = false, error = "Failed to end conversation" });
        }
    }

    [HttpPost("conversations/{id}/escalate")]
    public async Task<IActionResult> EscalateToHuman(int id, [FromBody] EscalateApiRequest request)
    {
        try
        {
            var success = await _chatService.EscalateToHumanAsync(id, request.Reason);
            if (success)
            {
                // Notify via SignalR that conversation was escalated
                await _chatHub.NotifyConversationUpdated(id, "escalated");

                return Ok(new
                {
                    success = true,
                    message = "Your request has been escalated to a human support agent. An agent will respond shortly."
                });
            }
            return Ok(new { success = false, error = "Failed to escalate conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation {Id}", id);
            return Ok(new { success = false, error = "Failed to escalate conversation" });
        }
    }

    [HttpGet("conversations/{id}/poll")]
    public async Task<IActionResult> PollMessages(int id, [FromQuery] DateTime? since)
    {
        try
        {
            var conversation = await _chatService.GetConversationAsync(id);
            if (conversation == null)
            {
                return NotFound(new { success = false, error = "Conversation not found" });
            }

            var messages = conversation.Messages
                .Where(m => since == null || m.CreatedAt > since)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    role = m.Role.ToString().ToLower(),
                    content = m.Content,
                    createdAt = m.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                success = true,
                status = conversation.Status.ToString().ToLower(),
                isEscalated = conversation.IsEscalated,
                hasAgent = !string.IsNullOrEmpty(conversation.AssignedAgentEmail),
                messages = messages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling messages for conversation {Id}", id);
            return Ok(new { success = false, error = "Failed to get messages" });
        }
    }

    [HttpGet("config/{shopDomain}")]
    public async Task<IActionResult> GetWidgetConfig(string shopDomain)
    {
        var settings = await _db.ChatbotSettings
            .Include(s => s.WidgetConfiguration)
            .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain);

        var widgetConfig = settings?.WidgetConfiguration ?? await _db.WidgetConfigurations
            .FirstOrDefaultAsync(w => w.ShopDomain == shopDomain);

        if (widgetConfig == null)
        {
            // Return defaults
            return Ok(new
            {
                success = true,
                config = new
                {
                    botName = settings?.BotName ?? "Support Assistant",
                    welcomeMessage = settings?.WelcomeMessage ?? "Hi! How can I help you today?",
                    position = "bottom-right",
                    primaryColor = "#7c3aed",
                    headerTitle = "Chat with us",
                    triggerText = "Need help?",
                    showPoweredBy = true
                }
            });
        }

        return Ok(new
        {
            success = true,
            config = new
            {
                botName = settings?.BotName ?? "Support Assistant",
                welcomeMessage = settings?.WelcomeMessage,
                position = widgetConfig.Position,
                offsetX = widgetConfig.OffsetX,
                offsetY = widgetConfig.OffsetY,
                triggerStyle = widgetConfig.TriggerStyle,
                primaryColor = widgetConfig.PrimaryColor,
                secondaryColor = widgetConfig.SecondaryColor,
                textColor = widgetConfig.TextColor,
                headerBackgroundColor = widgetConfig.HeaderBackgroundColor,
                headerTextColor = widgetConfig.HeaderTextColor,
                logoUrl = widgetConfig.LogoUrl,
                avatarUrl = widgetConfig.AvatarUrl,
                headerTitle = widgetConfig.HeaderTitle,
                triggerText = widgetConfig.TriggerText,
                autoOpenOnFirstVisit = widgetConfig.AutoOpenOnFirstVisit,
                autoOpenDelaySeconds = widgetConfig.AutoOpenDelaySeconds,
                showTypingIndicator = widgetConfig.ShowTypingIndicator,
                enableSoundNotifications = widgetConfig.EnableSoundNotifications,
                placeholderText = widgetConfig.PlaceholderText,
                showPoweredBy = widgetConfig.ShowPoweredBy,
                customCss = widgetConfig.CustomCss
            }
        });
    }
}

public class StartConversationApiRequest
{
    public string Shop { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string? VisitorId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? PageUrl { get; set; }
}

public class SendMessageApiRequest
{
    public string Shop { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string? Email { get; set; }
    public string Message { get; set; } = "";
}

public class EndConversationApiRequest
{
    public bool WasHelpful { get; set; }
    public int? Rating { get; set; }
    public string? Feedback { get; set; }
}

public class EscalateApiRequest
{
    public string? Reason { get; set; }
}
