using Algora.Chatbot.Application.DTOs;
using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Domain.Entities;
using Algora.Chatbot.Domain.Enums;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Algora.Chatbot.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ChatbotDbContext _db;

    public AnalyticsService(ChatbotDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string shopDomain, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var weekAgo = today.AddDays(-7);

        // Get conversation counts
        var todayConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain && c.CreatedAt >= today, cancellationToken);

        var monthConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain && c.CreatedAt >= monthStart, cancellationToken);

        var activeConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain && c.Status == ConversationStatus.Active, cancellationToken);

        var escalatedConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain &&
                c.Status == ConversationStatus.Escalated &&
                c.CreatedAt >= monthStart, cancellationToken);

        // Resolution rate
        var resolvedConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain &&
                c.Status == ConversationStatus.Resolved &&
                c.CreatedAt >= monthStart, cancellationToken);

        var totalClosedConversations = await _db.Conversations
            .CountAsync(c => c.ShopDomain == shopDomain &&
                (c.Status == ConversationStatus.Resolved || c.Status == ConversationStatus.Escalated) &&
                c.CreatedAt >= monthStart, cancellationToken);

        var resolutionRate = totalClosedConversations > 0
            ? (double)resolvedConversations / totalClosedConversations * 100
            : 0;

        // Average rating
        var ratings = await _db.Conversations
            .Where(c => c.ShopDomain == shopDomain && c.Rating.HasValue && c.CreatedAt >= monthStart)
            .Select(c => c.Rating!.Value)
            .ToListAsync(cancellationToken);

        var avgRating = ratings.Count > 0 ? ratings.Average() : 0;

        // AI cost (estimate based on tokens)
        var monthTokens = await _db.Messages
            .Where(m => m.Conversation.ShopDomain == shopDomain &&
                m.Role == MessageRole.Assistant &&
                m.TokensUsed.HasValue &&
                m.CreatedAt >= monthStart)
            .SumAsync(m => m.TokensUsed!.Value, cancellationToken);

        var estimatedCost = monthTokens * 0.00003m; // Rough GPT-4 estimate

        // Intent distribution
        var intents = await _db.Conversations
            .Where(c => c.ShopDomain == shopDomain &&
                !string.IsNullOrEmpty(c.PrimaryIntent) &&
                c.CreatedAt >= monthStart)
            .GroupBy(c => c.PrimaryIntent!)
            .Select(g => new { Intent = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var intentDistribution = intents.ToDictionary(i => i.Intent, i => i.Count);

        // Recent trend (last 7 days)
        var trend = await _db.Conversations
            .Where(c => c.ShopDomain == shopDomain && c.CreatedAt >= weekAgo)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Resolved = g.Count(c => c.Status == ConversationStatus.Resolved),
                Escalated = g.Count(c => c.Status == ConversationStatus.Escalated)
            })
            .OrderBy(g => g.Date)
            .ToListAsync(cancellationToken);

        var recentTrend = trend.Select(t => new ConversationTrendDto
        {
            Date = t.Date,
            Count = t.Count,
            Resolved = t.Resolved,
            Escalated = t.Escalated
        }).ToList();

        return new DashboardStatsDto
        {
            TotalConversationsToday = todayConversations,
            TotalConversationsThisMonth = monthConversations,
            ActiveConversations = activeConversations,
            EscalatedConversations = escalatedConversations,
            ResolutionRate = resolutionRate,
            AvgRating = avgRating,
            AvgResponseTimeSeconds = 2.5, // Placeholder
            AvgConversationDurationMinutes = 5.2, // Placeholder
            TotalAiCostThisMonth = estimatedCost,
            IntentDistribution = intentDistribution,
            RecentTrend = recentTrend
        };
    }

    public async Task<List<AnalyticsSnapshotDto>> GetAnalyticsHistoryAsync(
        string shopDomain,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _db.ConversationAnalytics
            .Where(a => a.ShopDomain == shopDomain &&
                a.SnapshotDate >= startDate &&
                a.SnapshotDate <= endDate)
            .OrderBy(a => a.SnapshotDate)
            .ToListAsync(cancellationToken);

        return snapshots.Select(s => new AnalyticsSnapshotDto
        {
            SnapshotDate = s.SnapshotDate,
            TotalConversations = s.TotalConversations,
            ResolvedConversations = s.ResolvedConversations,
            EscalatedConversations = s.EscalatedConversations,
            AvgResponseTimeSeconds = s.AvgResponseTimeSeconds,
            AvgRating = s.AvgRating,
            HelpfulPercentage = s.HelpfulPercentage,
            TotalAiCost = s.TotalAiCost,
            IntentDistribution = string.IsNullOrEmpty(s.IntentDistributionJson)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(s.IntentDistributionJson)
                    ?? new Dictionary<string, int>()
        }).ToList();
    }

    public async Task GenerateDailySnapshotAsync(string shopDomain, CancellationToken cancellationToken = default)
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        // Check if snapshot already exists
        var existingSnapshot = await _db.ConversationAnalytics
            .FirstOrDefaultAsync(a => a.ShopDomain == shopDomain && a.SnapshotDate == yesterday, cancellationToken);

        if (existingSnapshot != null)
        {
            return; // Already generated
        }

        // Get conversation stats for yesterday
        var conversations = await _db.Conversations
            .Where(c => c.ShopDomain == shopDomain &&
                c.CreatedAt >= yesterday &&
                c.CreatedAt < today)
            .ToListAsync(cancellationToken);

        var totalConversations = conversations.Count;
        var resolvedConversations = conversations.Count(c => c.Status == ConversationStatus.Resolved);
        var escalatedConversations = conversations.Count(c => c.Status == ConversationStatus.Escalated);

        var ratings = conversations.Where(c => c.Rating.HasValue).Select(c => c.Rating!.Value).ToList();
        var avgRating = ratings.Count > 0 ? ratings.Average() : 0;

        var helpfulCount = conversations.Count(c => c.WasHelpful == true);
        var totalFeedback = conversations.Count(c => c.WasHelpful.HasValue);
        var helpfulPercentage = totalFeedback > 0 ? (double)helpfulCount / totalFeedback * 100 : 0;

        // Intent distribution
        var intents = conversations
            .Where(c => !string.IsNullOrEmpty(c.PrimaryIntent))
            .GroupBy(c => c.PrimaryIntent!)
            .ToDictionary(g => g.Key, g => g.Count());

        // AI cost
        var conversationIds = conversations.Select(c => c.Id).ToList();
        var tokens = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationId) &&
                m.Role == MessageRole.Assistant &&
                m.TokensUsed.HasValue)
            .SumAsync(m => m.TokensUsed!.Value, cancellationToken);

        var estimatedCost = tokens * 0.00003m;

        var snapshot = new ConversationAnalytics
        {
            ShopDomain = shopDomain,
            SnapshotDate = yesterday,
            TotalConversations = totalConversations,
            ResolvedConversations = resolvedConversations,
            EscalatedConversations = escalatedConversations,
            AvgResponseTimeSeconds = 2.5, // Placeholder
            AvgRating = avgRating,
            HelpfulPercentage = helpfulPercentage,
            TotalAiCost = estimatedCost,
            IntentDistributionJson = JsonSerializer.Serialize(intents)
        };

        _db.ConversationAnalytics.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AnalyticsSnapshotDto>> GetAnalyticsAsync(string shopDomain, DateTime startDate, DateTime endDate)
    {
        return await GetAnalyticsHistoryAsync(shopDomain, startDate, endDate);
    }
}
