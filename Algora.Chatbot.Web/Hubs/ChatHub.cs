using Microsoft.AspNetCore.SignalR;

namespace Algora.Chatbot.Web.Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        _logger.LogInformation("Client {ConnectionId} joined conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        _logger.LogInformation("Client {ConnectionId} left conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(string conversationId, string role, string content)
    {
        await Clients.Group($"conversation-{conversationId}").SendAsync("ReceiveMessage", new
        {
            role,
            content,
            timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

public static class ChatHubExtensions
{
    public static async Task SendMessageToConversation(
        this IHubContext<ChatHub> hubContext,
        int conversationId,
        string role,
        string content,
        int? messageId = null)
    {
        await hubContext.Clients.Group($"conversation-{conversationId}").SendAsync("ReceiveMessage", new
        {
            id = messageId,
            role,
            content,
            timestamp = DateTime.UtcNow
        });
    }

    public static async Task NotifyConversationUpdated(
        this IHubContext<ChatHub> hubContext,
        int conversationId,
        string status)
    {
        await hubContext.Clients.Group($"conversation-{conversationId}").SendAsync("ConversationUpdated", new
        {
            conversationId,
            status,
            timestamp = DateTime.UtcNow
        });
    }

    public static async Task NotifyAgentTyping(
        this IHubContext<ChatHub> hubContext,
        int conversationId,
        bool isTyping)
    {
        await hubContext.Clients.Group($"conversation-{conversationId}").SendAsync("AgentTyping", new
        {
            conversationId,
            isTyping,
            timestamp = DateTime.UtcNow
        });
    }
}
