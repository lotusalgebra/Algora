using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Algora.Chatbot.Web.Controllers;

[ApiController]
[Route("api/widget/v1")]
[EnableCors("WidgetPolicy")]
public class WidgetApiController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ChatbotDbContext _db;
    private readonly ILogger<WidgetApiController> _logger;

    public WidgetApiController(
        IChatService chatService,
        ChatbotDbContext db,
        ILogger<WidgetApiController> logger)
    {
        _chatService = chatService;
        _db = db;
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
