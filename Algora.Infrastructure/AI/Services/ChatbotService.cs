using System.Text.Json;
using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.AI.Services;

public class ChatbotService : IChatbotService
{
    private readonly AppDbContext _db;
    private readonly IAiTextProvider _aiProvider;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        AppDbContext db,
        IAiTextProvider aiProvider,
        ILogger<ChatbotService> logger)
    {
        _db = db;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<ChatbotResponse> SendMessageAsync(ChatbotRequest request, CancellationToken ct = default)
    {
        try
        {
            // Get or create conversation
            var conversation = request.ConversationId.HasValue
                ? await _db.Set<ChatbotConversation>().FindAsync(new object[] { request.ConversationId.Value }, ct)
                : await GetOrCreateConversationAsync(request.ShopDomain, request.SessionId, request.CustomerEmail, ct);

            if (conversation == null)
            {
                return new ChatbotResponse { Success = false, Error = "Could not find or create conversation" };
            }

            // Save user message
            var userMessage = new ChatbotMessage
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = request.Message
            };
            _db.Set<ChatbotMessage>().Add(userMessage);
            await _db.SaveChangesAsync(ct);

            // Build context and generate response
            var messages = await _db.Set<ChatbotMessage>()
                .Where(m => m.ConversationId == conversation.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(10)
                .ToListAsync(ct);

            var customer = conversation.CustomerId.HasValue
                ? await _db.Customers.FindAsync(new object[] { conversation.CustomerId.Value }, ct)
                : null;

            var recentOrders = customer != null
                ? await _db.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(3)
                    .ToListAsync(ct)
                : new List<Order>();

            var prompt = BuildChatbotPrompt(request.ShopDomain, customer, recentOrders, messages, request.Message);
            var aiResponse = await _aiProvider.GenerateTextAsync(prompt, ct);
            var parsed = ParseChatbotResponse(aiResponse);

            // Save assistant message
            var assistantMessage = new ChatbotMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = parsed.Response,
                Intent = parsed.Intent,
                Confidence = parsed.Confidence,
                SuggestedActions = parsed.SuggestedActions.Count > 0
                    ? JsonSerializer.Serialize(parsed.SuggestedActions)
                    : null
            };
            _db.Set<ChatbotMessage>().Add(assistantMessage);

            // Update conversation topic if detected
            if (!string.IsNullOrEmpty(parsed.Intent) && string.IsNullOrEmpty(conversation.Topic))
            {
                conversation.Topic = parsed.Intent;
            }

            await _db.SaveChangesAsync(ct);

            return new ChatbotResponse
            {
                Success = true,
                ConversationId = conversation.Id,
                Response = parsed.Response,
                Intent = parsed.Intent,
                Confidence = parsed.Confidence,
                SuggestedActions = parsed.SuggestedActions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message");
            return new ChatbotResponse
            {
                Success = false,
                Error = "I'm sorry, I encountered an error. Please try again."
            };
        }
    }

    public async Task<ChatbotConversationDto> StartConversationAsync(string shopDomain, string sessionId, string? customerEmail, CancellationToken ct = default)
    {
        var conversation = await GetOrCreateConversationAsync(shopDomain, sessionId, customerEmail, ct);

        return new ChatbotConversationDto
        {
            Id = conversation.Id,
            SessionId = conversation.SessionId,
            Status = conversation.Status,
            Topic = conversation.Topic,
            CreatedAt = conversation.CreatedAt,
            Messages = new()
        };
    }

    public async Task<ChatbotConversationDto?> GetConversationAsync(int conversationId, CancellationToken ct = default)
    {
        var conversation = await _db.Set<ChatbotConversation>()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation == null) return null;

        return MapToDto(conversation);
    }

    public async Task<ChatbotConversationDto?> GetConversationBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var conversation = await _db.Set<ChatbotConversation>()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.Status == "active", ct);

        if (conversation == null) return null;

        return MapToDto(conversation);
    }

    public async Task EndConversationAsync(int conversationId, bool wasHelpful, CancellationToken ct = default)
    {
        var conversation = await _db.Set<ChatbotConversation>().FindAsync(new object[] { conversationId }, ct);
        if (conversation != null)
        {
            conversation.Status = "resolved";
            conversation.WasHelpful = wasHelpful;
            conversation.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task EscalateToAgentAsync(int conversationId, CancellationToken ct = default)
    {
        var conversation = await _db.Set<ChatbotConversation>().FindAsync(new object[] { conversationId }, ct);
        if (conversation != null)
        {
            conversation.Status = "escalated";
            conversation.EscalatedToAgentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<ChatbotMessageDto>> GetMessagesAsync(int conversationId, CancellationToken ct = default)
    {
        var messages = await _db.Set<ChatbotMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return messages.Select(m => new ChatbotMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Intent = m.Intent,
            SuggestedActions = string.IsNullOrEmpty(m.SuggestedActions)
                ? null
                : JsonSerializer.Deserialize<List<ChatbotAction>>(m.SuggestedActions),
            CreatedAt = m.CreatedAt
        });
    }

    private async Task<ChatbotConversation> GetOrCreateConversationAsync(string shopDomain, string sessionId, string? customerEmail, CancellationToken ct)
    {
        var existing = await _db.Set<ChatbotConversation>()
            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.Status == "active", ct);

        if (existing != null) return existing;

        Customer? customer = null;
        if (!string.IsNullOrEmpty(customerEmail))
        {
            customer = await _db.Customers.FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == customerEmail, ct);
        }

        var conversation = new ChatbotConversation
        {
            ShopDomain = shopDomain,
            SessionId = sessionId,
            CustomerId = customer?.Id,
            CustomerEmail = customerEmail,
            Status = "active"
        };

        _db.Set<ChatbotConversation>().Add(conversation);
        await _db.SaveChangesAsync(ct);

        return conversation;
    }

    private static string BuildChatbotPrompt(string shopDomain, Customer? customer, List<Order> recentOrders, List<ChatbotMessage> history, string currentMessage)
    {
        var ordersInfo = recentOrders.Count > 0
            ? string.Join("\n", recentOrders.Select(o => $"- Order #{o.OrderNumber}: {o.GrandTotal:C} on {o.CreatedAt:MMM d, yyyy} - {o.FulfillmentStatus ?? "Processing"}"))
            : "No recent orders";

        var conversationHistory = history.Count > 0
            ? string.Join("\n", history.Select(m => $"{m.Role}: {m.Content}"))
            : "";

        return $@"You are a helpful customer service chatbot for an online store.

Customer Info:
- Name: {customer?.FirstName ?? "Guest"} {customer?.LastName ?? ""}
- Email: {customer?.Email ?? "Not provided"}

Recent Orders:
{ordersInfo}

Store Policies:
- Returns: 30-day return policy for unused items
- Shipping: Free shipping on orders over $50, standard 3-5 business days
- Contact: support@store.com or call 1-800-STORE

Instructions:
1. Be friendly, helpful, and concise
2. Answer questions about orders, shipping, returns, and products
3. If you can look up specific order info, use the orders listed above
4. For complex issues, suggest connecting with a human agent
5. Keep responses under 100 words

{(conversationHistory.Length > 0 ? $"Conversation so far:\n{conversationHistory}\n" : "")}

Customer says: {currentMessage}

Respond in JSON format:
{{
  ""response"": ""Your helpful response here"",
  ""intent"": ""order_status|return|shipping|product|general"",
  ""confidence"": 0.85,
  ""suggestedActions"": [
    {{""label"": ""Track Order"", ""type"": ""link"", ""value"": ""/portal/orders""}},
    {{""label"": ""Start Return"", ""type"": ""link"", ""value"": ""/portal/returns""}}
  ]
}}";
    }

    private static ChatbotResponse ParseChatbotResponse(string aiResponse)
    {
        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<ChatbotJsonResponse>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    return new ChatbotResponse
                    {
                        Success = true,
                        Response = parsed.Response ?? "I'm here to help!",
                        Intent = parsed.Intent,
                        Confidence = parsed.Confidence,
                        SuggestedActions = parsed.SuggestedActions ?? new()
                    };
                }
            }

            // Fallback: use raw response
            return new ChatbotResponse
            {
                Success = true,
                Response = aiResponse.Length > 500 ? aiResponse[..500] : aiResponse
            };
        }
        catch
        {
            return new ChatbotResponse
            {
                Success = true,
                Response = aiResponse.Length > 500 ? aiResponse[..500] : aiResponse
            };
        }
    }

    private static ChatbotConversationDto MapToDto(ChatbotConversation c) => new()
    {
        Id = c.Id,
        SessionId = c.SessionId,
        Status = c.Status,
        Topic = c.Topic,
        CreatedAt = c.CreatedAt,
        Messages = c.Messages.OrderBy(m => m.CreatedAt).Select(m => new ChatbotMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Intent = m.Intent,
            SuggestedActions = string.IsNullOrEmpty(m.SuggestedActions)
                ? null
                : JsonSerializer.Deserialize<List<ChatbotAction>>(m.SuggestedActions),
            CreatedAt = m.CreatedAt
        }).ToList()
    };

    private class ChatbotJsonResponse
    {
        public string? Response { get; set; }
        public string? Intent { get; set; }
        public decimal? Confidence { get; set; }
        public List<ChatbotAction>? SuggestedActions { get; set; }
    }
}
