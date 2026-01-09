using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.AI;

[Authorize]
[RequireFeature(FeatureCodes.AiChatbot)]
public class ChatbotModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IShopContext _shopContext;

    public ChatbotModel(AppDbContext db, IShopContext shopContext)
    {
        _db = db;
        _shopContext = shopContext;
    }

    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int ResolvedConversations { get; set; }
    public int EscalatedConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AvgMessagesPerConversation { get; set; }
    public double ResolutionRate { get; set; }
    public double HelpfulRate { get; set; }
    public List<ConversationSummary> RecentConversations { get; set; } = new();
    public Dictionary<string, int> TopIntents { get; set; } = new();

    public async Task OnGetAsync()
    {
        var shopDomain = _shopContext.ShopDomain;

        var conversations = await _db.Set<ChatbotConversation>()
            .Where(c => c.ShopDomain == shopDomain)
            .ToListAsync();

        TotalConversations = conversations.Count;
        ActiveConversations = conversations.Count(c => c.Status == "active");
        ResolvedConversations = conversations.Count(c => c.Status == "resolved");
        EscalatedConversations = conversations.Count(c => c.Status == "escalated");

        var helpfulCount = conversations.Count(c => c.WasHelpful == true);
        var resolvedWithFeedback = conversations.Count(c => c.WasHelpful.HasValue);
        HelpfulRate = resolvedWithFeedback > 0 ? (double)helpfulCount / resolvedWithFeedback * 100 : 0;

        ResolutionRate = TotalConversations > 0
            ? (double)ResolvedConversations / TotalConversations * 100
            : 0;

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var messages = await _db.Set<ChatbotMessage>()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .ToListAsync();

        TotalMessages = messages.Count;
        AvgMessagesPerConversation = TotalConversations > 0
            ? (double)TotalMessages / TotalConversations
            : 0;

        // Get top intents
        TopIntents = messages
            .Where(m => !string.IsNullOrEmpty(m.Intent))
            .GroupBy(m => m.Intent!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count());

        // Get recent conversations
        RecentConversations = conversations
            .OrderByDescending(c => c.CreatedAt)
            .Take(10)
            .Select(c => new ConversationSummary
            {
                Id = c.Id,
                SessionId = c.SessionId,
                CustomerEmail = c.CustomerEmail,
                Topic = c.Topic,
                Status = c.Status,
                MessageCount = messages.Count(m => m.ConversationId == c.Id),
                CreatedAt = c.CreatedAt,
                EndedAt = c.EndedAt,
                WasHelpful = c.WasHelpful
            })
            .ToList();
    }

    public async Task<IActionResult> OnGetConversationAsync(int id)
    {
        var conversation = await _db.Set<ChatbotConversation>()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return new JsonResult(new { success = false, error = "Conversation not found" });

        var messages = await _db.Set<ChatbotMessage>()
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return new JsonResult(new
        {
            success = true,
            data = new
            {
                conversation.Id,
                conversation.SessionId,
                conversation.CustomerEmail,
                conversation.Topic,
                conversation.Status,
                conversation.CreatedAt,
                conversation.EndedAt,
                conversation.WasHelpful,
                messages = messages.Select(m => new
                {
                    m.Id,
                    m.Role,
                    m.Content,
                    m.Intent,
                    m.CreatedAt
                })
            }
        });
    }

    public class ConversationSummary
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = "";
        public string? CustomerEmail { get; set; }
        public string? Topic { get; set; }
        public string Status { get; set; } = "";
        public int MessageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool? WasHelpful { get; set; }
    }
}
