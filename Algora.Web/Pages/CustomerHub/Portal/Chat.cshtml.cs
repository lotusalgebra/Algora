using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Portal;

[IgnoreAntiforgeryToken]
public class ChatModel : PageModel
{
    private readonly IChatbotService _chatbotService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(
        IChatbotService chatbotService,
        IShopContext shopContext,
        ILogger<ChatModel> logger)
    {
        _chatbotService = chatbotService;
        _shopContext = shopContext;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public string SessionId { get; set; } = "";

    public void OnGet()
    {
        SessionId = Guid.NewGuid().ToString("N");
    }

    public async Task<IActionResult> OnPostSendAsync([FromBody] SendRequest request)
    {
        try
        {
            var chatRequest = new ChatbotRequest
            {
                ShopDomain = _shopContext.ShopDomain,
                SessionId = request.SessionId,
                CustomerEmail = request.Email,
                Message = request.Message,
                ConversationId = request.ConversationId
            };

            var response = await _chatbotService.SendMessageAsync(chatRequest);

            return new JsonResult(new
            {
                success = response.Success,
                conversationId = response.ConversationId,
                response = response.Response,
                intent = response.Intent,
                suggestedActions = response.SuggestedActions,
                error = response.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return new JsonResult(new { success = false, error = "An error occurred" });
        }
    }

    public async Task<IActionResult> OnPostEscalateAsync([FromBody] EscalateRequest request)
    {
        try
        {
            await _chatbotService.EscalateToAgentAsync(request.ConversationId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating chat");
            return new JsonResult(new { success = false });
        }
    }

    public async Task<IActionResult> OnPostEndAsync([FromBody] EndRequest request)
    {
        try
        {
            await _chatbotService.EndConversationAsync(request.ConversationId, request.WasHelpful);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending chat");
            return new JsonResult(new { success = false });
        }
    }

    public class SendRequest
    {
        public string SessionId { get; set; } = "";
        public string? Email { get; set; }
        public string Message { get; set; } = "";
        public int? ConversationId { get; set; }
    }

    public class EscalateRequest
    {
        public int ConversationId { get; set; }
    }

    public class EndRequest
    {
        public int ConversationId { get; set; }
        public bool WasHelpful { get; set; }
    }
}
