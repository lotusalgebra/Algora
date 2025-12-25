using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.AI;
using Algora.Chatbot.Domain.Entities;
using Algora.Chatbot.Domain.Enums;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Chatbot.Infrastructure.AI.Services;

public class ChatbotOrchestrator : IChatbotOrchestrator
{
    private readonly IEnumerable<IChatbotAiProvider> _providers;
    private readonly ChatbotDbContext _db;
    private readonly ILogger<ChatbotOrchestrator> _logger;

    public ChatbotOrchestrator(
        IEnumerable<IChatbotAiProvider> providers,
        ChatbotDbContext db,
        ILogger<ChatbotOrchestrator> logger)
    {
        _providers = providers;
        _db = db;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessMessageAsync(
        string shopDomain,
        int conversationId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load conversation
            var conversation = await _db.Conversations
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(10))
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation == null)
            {
                return new ChatResponse { Success = false, Error = "Conversation not found" };
            }

            // Load settings
            var settings = await _db.ChatbotSettings
                .FirstOrDefaultAsync(s => s.ShopDomain == shopDomain, cancellationToken);

            // Build context
            var context = await BuildContextAsync(shopDomain, conversation, userMessage, settings, cancellationToken);

            // Get ordered providers
            var orderedProviders = GetOrderedProviders(settings);

            // Try each provider with fallback
            ChatCompletionResult? result = null;
            foreach (var provider in orderedProviders)
            {
                if (!provider.IsConfigured) continue;

                try
                {
                    _logger.LogInformation("Trying AI provider: {Provider}", provider.ProviderName);
                    result = await provider.GenerateResponseAsync(context, cancellationToken);
                    if (result.Success)
                    {
                        _logger.LogInformation("AI provider {Provider} succeeded", provider.ProviderName);
                        break;
                    }
                    _logger.LogWarning("AI provider {Provider} failed: {Error}", provider.ProviderName, result.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI provider {Provider} threw exception, trying fallback", provider.ProviderName);
                }
            }

            if (result == null || !result.Success)
            {
                return new ChatResponse
                {
                    Success = false,
                    Error = "All AI providers are unavailable. Please try again later.",
                    ConversationId = conversationId
                };
            }

            // Save user message
            var userMsg = new Message
            {
                ConversationId = conversationId,
                Role = MessageRole.User,
                Content = userMessage,
                CreatedAt = DateTime.UtcNow
            };
            _db.Messages.Add(userMsg);

            // Save assistant message
            var assistantMsg = new Message
            {
                ConversationId = conversationId,
                Role = MessageRole.Assistant,
                Content = result.Response,
                DetectedIntent = result.DetectedIntent,
                IntentConfidence = result.Confidence,
                AiProvider = result.ProviderUsed,
                AiModel = result.ModelUsed,
                TokensUsed = result.TokensUsed,
                AiCost = result.EstimatedCost,
                SuggestedActionsJson = result.SuggestedActions != null
                    ? System.Text.Json.JsonSerializer.Serialize(result.SuggestedActions)
                    : null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Messages.Add(assistantMsg);

            // Update conversation
            conversation.LastMessageAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(result.DetectedIntent) && string.IsNullOrEmpty(conversation.PrimaryIntent))
            {
                conversation.PrimaryIntent = result.DetectedIntent;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new ChatResponse
            {
                Success = true,
                ConversationId = conversationId,
                Response = result.Response,
                Intent = result.DetectedIntent,
                Confidence = result.Confidence,
                SuggestedActions = result.SuggestedActions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for conversation {ConversationId}", conversationId);
            return new ChatResponse
            {
                Success = false,
                Error = "An error occurred processing your message.",
                ConversationId = conversationId
            };
        }
    }

    public async Task<List<ProductRecommendationDto>> GetRecommendationsAsync(
        string shopDomain,
        int conversationId,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        // This would integrate with IShopifyProductService to get recommendations
        // For now, return empty list
        await Task.CompletedTask;
        return new List<ProductRecommendationDto>();
    }

    private async Task<ChatContext> BuildContextAsync(
        string shopDomain,
        Conversation conversation,
        string userMessage,
        ChatbotSettings? settings,
        CancellationToken cancellationToken)
    {
        // Build system prompt
        var systemPrompt = BuildSystemPrompt(settings);

        // Build history
        var history = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage
            {
                Role = m.Role == MessageRole.User ? "user" : "assistant",
                Content = m.Content
            })
            .ToList();

        // Load policies for context
        var policies = await _db.Policies
            .Where(p => p.ShopDomain == shopDomain && p.IsActive)
            .ToListAsync(cancellationToken);

        PolicyContext? policyContext = null;
        if (policies.Any())
        {
            policyContext = new PolicyContext
            {
                ReturnPolicy = policies.FirstOrDefault(p => p.PolicyType == "returns")?.Summary,
                ShippingPolicy = policies.FirstOrDefault(p => p.PolicyType == "shipping")?.Summary,
                ReturnWindowDays = policies.FirstOrDefault(p => p.PolicyType == "returns")?.ReturnWindowDays,
                FreeShippingThreshold = policies.FirstOrDefault(p => p.PolicyType == "shipping")?.FreeShippingThreshold
            };
        }

        return new ChatContext
        {
            ShopDomain = shopDomain,
            SystemPrompt = systemPrompt,
            History = history,
            CurrentMessage = userMessage,
            Customer = conversation.CustomerEmail != null ? new CustomerContext
            {
                CustomerId = conversation.ShopifyCustomerId,
                Email = conversation.CustomerEmail,
                Name = conversation.CustomerName
            } : null,
            Policies = policyContext,
            Temperature = settings?.Temperature ?? 0.7,
            MaxTokens = settings?.MaxTokens ?? 500
        };
    }

    private static string BuildSystemPrompt(ChatbotSettings? settings)
    {
        var botName = settings?.BotName ?? "Support Assistant";
        var tone = settings?.Tone ?? "professional";

        var prompt = $@"You are {botName}, a helpful customer support assistant for an online store.
Your communication style should be {tone}.

You help customers with:
- Tracking orders and checking order status
- Product information and recommendations
- Return and refund requests
- Shipping information and policies
- General questions about the store

Guidelines:
- Be concise and helpful
- If you don't know something, say so honestly
- For order-related questions, ask for order number or email if not provided
- Suggest relevant actions the customer can take

{(settings?.CustomInstructions ?? "")}

Always respond with valid JSON in this format:
{{
  ""response"": ""Your helpful response here"",
  ""intent"": ""order_status|product_inquiry|return_request|shipping_info|general"",
  ""confidence"": 0.0-1.0,
  ""suggestedActions"": [
    {{""label"": ""Track Order"", ""type"": ""action"", ""value"": ""track_order""}},
    {{""label"": ""View Returns"", ""type"": ""link"", ""value"": ""/returns""}}
  ]
}}";

        return prompt;
    }

    private IEnumerable<IChatbotAiProvider> GetOrderedProviders(ChatbotSettings? settings)
    {
        var preferred = settings?.PreferredAiProvider ?? "openai";
        var fallback = settings?.FallbackAiProvider;

        return _providers
            .Where(p => p.IsConfigured)
            .OrderBy(p => p.ProviderName == preferred ? 0 :
                         p.ProviderName == fallback ? 1 : 2)
            .ThenBy(p => p.Priority);
    }
}
