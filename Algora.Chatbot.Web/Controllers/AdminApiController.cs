using Algora.Chatbot.Application.Interfaces.Services;
using Algora.Chatbot.Domain.Entities;
using Algora.Chatbot.Domain.Enums;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Algora.Chatbot.Web.Controllers;

[ApiController]
[Route("api/admin/v1")]
public class AdminApiController : ControllerBase
{
    private readonly ChatbotDbContext _db;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AdminApiController> _logger;

    public AdminApiController(
        ChatbotDbContext db,
        IAnalyticsService analyticsService,
        ILogger<AdminApiController> logger)
    {
        _db = db;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        try
        {
            var stats = await _analyticsService.GetDashboardStatsAsync(shop);
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for {Shop}", shop);
            return Ok(new { success = false, error = "Failed to get dashboard data" });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string shop,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var query = _db.Conversations
            .Include(c => c.Messages)
            .Where(c => c.ShopDomain == shop);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        var total = await query.CountAsync();
        var conversations = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                id = c.Id,
                sessionId = c.SessionId,
                customerEmail = c.CustomerEmail,
                status = c.Status.ToString().ToLower(),
                primaryIntent = c.PrimaryIntent,
                isEscalated = c.IsEscalated,
                messageCount = c.Messages.Count,
                rating = c.Rating,
                wasHelpful = c.WasHelpful,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = conversations,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(int id, [FromQuery] string shop)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.ShopDomain == shop);

        if (conversation == null)
        {
            return NotFound(new { success = false, error = "Conversation not found" });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                id = conversation.Id,
                sessionId = conversation.SessionId,
                visitorId = conversation.VisitorId,
                shopifyCustomerId = conversation.ShopifyCustomerId,
                customerEmail = conversation.CustomerEmail,
                customerName = conversation.CustomerName,
                status = conversation.Status.ToString().ToLower(),
                primaryIntent = conversation.PrimaryIntent,
                isEscalated = conversation.IsEscalated,
                escalatedAt = conversation.EscalatedAt,
                rating = conversation.Rating,
                wasHelpful = conversation.WasHelpful,
                feedback = conversation.FeedbackComment,
                currentPageUrl = conversation.CurrentPageUrl,
                messages = conversation.Messages.Select(m => new
                {
                    id = m.Id,
                    role = m.Role.ToString().ToLower(),
                    content = m.Content,
                    detectedIntent = m.DetectedIntent,
                    intentConfidence = m.IntentConfidence,
                    aiProvider = m.AiProvider,
                    tokensUsed = m.TokensUsed,
                    suggestedActions = m.SuggestedActionsJson,
                    createdAt = m.CreatedAt
                }),
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt
            }
        });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var settings = await _db.ChatbotSettings
            .FirstOrDefaultAsync(s => s.ShopDomain == shop);

        var widgetConfig = await _db.WidgetConfigurations
            .FirstOrDefaultAsync(w => w.ShopDomain == shop);

        return Ok(new
        {
            success = true,
            settings = settings == null ? null : new
            {
                botName = settings.BotName,
                welcomeMessage = settings.WelcomeMessage,
                tone = settings.Tone,
                preferredAiProvider = settings.PreferredAiProvider,
                fallbackAiProvider = settings.FallbackAiProvider,
                enableOrderTracking = settings.EnableOrderTracking,
                enableProductRecommendations = settings.EnableProductRecommendations,
                enableReturns = settings.EnableReturns,
                enableHumanEscalation = settings.EnableHumanEscalation,
                confidenceThreshold = settings.ConfidenceThreshold,
                maxResponseTokens = settings.MaxTokens,
                escalationEmail = settings.EscalationEmail
            },
            widgetConfig = widgetConfig == null ? null : new
            {
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

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromQuery] string shop, [FromBody] UpdateSettingsRequest request)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var settings = await _db.ChatbotSettings.FirstOrDefaultAsync(s => s.ShopDomain == shop);

        if (settings == null)
        {
            settings = new ChatbotSettings { ShopDomain = shop };
            _db.ChatbotSettings.Add(settings);
        }

        if (request.BotName != null) settings.BotName = request.BotName;
        if (request.WelcomeMessage != null) settings.WelcomeMessage = request.WelcomeMessage;
        if (request.Tone != null) settings.Tone = request.Tone;
        if (request.PreferredAiProvider != null) settings.PreferredAiProvider = request.PreferredAiProvider;
        if (request.FallbackAiProvider != null) settings.FallbackAiProvider = request.FallbackAiProvider;
        if (request.EnableOrderTracking.HasValue) settings.EnableOrderTracking = request.EnableOrderTracking.Value;
        if (request.EnableProductRecommendations.HasValue) settings.EnableProductRecommendations = request.EnableProductRecommendations.Value;
        if (request.EnableReturns.HasValue) settings.EnableReturns = request.EnableReturns.Value;
        if (request.EnableHumanEscalation.HasValue) settings.EnableHumanEscalation = request.EnableHumanEscalation.Value;
        if (request.ConfidenceThreshold.HasValue) settings.ConfidenceThreshold = request.ConfidenceThreshold.Value;
        if (request.MaxTokens.HasValue) settings.MaxTokens = request.MaxTokens.Value;
        if (request.EscalationEmail != null) settings.EscalationEmail = request.EscalationEmail;

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("widget-config")]
    public async Task<IActionResult> UpdateWidgetConfig([FromQuery] string shop, [FromBody] UpdateWidgetConfigRequest request)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var config = await _db.WidgetConfigurations.FirstOrDefaultAsync(w => w.ShopDomain == shop);

        if (config == null)
        {
            config = new WidgetConfiguration { ShopDomain = shop };
            _db.WidgetConfigurations.Add(config);
        }

        if (request.Position != null) config.Position = request.Position;
        if (request.OffsetX.HasValue) config.OffsetX = request.OffsetX.Value;
        if (request.OffsetY.HasValue) config.OffsetY = request.OffsetY.Value;
        if (request.TriggerStyle != null) config.TriggerStyle = request.TriggerStyle;
        if (request.PrimaryColor != null) config.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor != null) config.SecondaryColor = request.SecondaryColor;
        if (request.TextColor != null) config.TextColor = request.TextColor;
        if (request.HeaderBackgroundColor != null) config.HeaderBackgroundColor = request.HeaderBackgroundColor;
        if (request.HeaderTextColor != null) config.HeaderTextColor = request.HeaderTextColor;
        if (request.LogoUrl != null) config.LogoUrl = request.LogoUrl;
        if (request.AvatarUrl != null) config.AvatarUrl = request.AvatarUrl;
        if (request.HeaderTitle != null) config.HeaderTitle = request.HeaderTitle;
        if (request.TriggerText != null) config.TriggerText = request.TriggerText;
        if (request.AutoOpenOnFirstVisit.HasValue) config.AutoOpenOnFirstVisit = request.AutoOpenOnFirstVisit.Value;
        if (request.AutoOpenDelaySeconds.HasValue) config.AutoOpenDelaySeconds = request.AutoOpenDelaySeconds.Value;
        if (request.ShowTypingIndicator.HasValue) config.ShowTypingIndicator = request.ShowTypingIndicator.Value;
        if (request.EnableSoundNotifications.HasValue) config.EnableSoundNotifications = request.EnableSoundNotifications.Value;
        if (request.PlaceholderText != null) config.PlaceholderText = request.PlaceholderText;
        if (request.ShowPoweredBy.HasValue) config.ShowPoweredBy = request.ShowPoweredBy.Value;
        if (request.CustomCss != null) config.CustomCss = request.CustomCss;

        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] string shop,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        try
        {
            var analytics = await _analyticsService.GetAnalyticsAsync(shop, start, end);
            return Ok(new { success = true, data = analytics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for {Shop}", shop);
            return Ok(new { success = false, error = "Failed to get analytics data" });
        }
    }

    [HttpGet("knowledge")]
    public async Task<IActionResult> GetKnowledgeArticles([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var articles = await _db.KnowledgeArticles
            .Where(a => a.ShopDomain == shop)
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Title)
            .Select(a => new
            {
                id = a.Id,
                title = a.Title,
                category = a.Category,
                content = a.Content,
                keywords = a.Tags,
                isActive = a.IsActive,
                createdAt = a.CreatedAt,
                updatedAt = a.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = articles });
    }

    [HttpPost("knowledge")]
    public async Task<IActionResult> CreateKnowledgeArticle([FromQuery] string shop, [FromBody] CreateKnowledgeArticleRequest request)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest(new { success = false, error = "Shop parameter is required" });
        }

        var article = new KnowledgeArticle
        {
            ShopDomain = shop,
            Title = request.Title,
            Category = request.Category,
            Content = request.Content,
            Tags = request.Tags,
            IsActive = true
        };

        _db.KnowledgeArticles.Add(article);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, id = article.Id });
    }

    [HttpPut("knowledge/{id}")]
    public async Task<IActionResult> UpdateKnowledgeArticle(int id, [FromQuery] string shop, [FromBody] UpdateKnowledgeArticleRequest request)
    {
        var article = await _db.KnowledgeArticles
            .FirstOrDefaultAsync(a => a.Id == id && a.ShopDomain == shop);

        if (article == null)
        {
            return NotFound(new { success = false, error = "Article not found" });
        }

        if (request.Title != null) article.Title = request.Title;
        if (request.Category != null) article.Category = request.Category;
        if (request.Content != null) article.Content = request.Content;
        if (request.Tags != null) article.Tags = request.Tags;
        if (request.IsActive.HasValue) article.IsActive = request.IsActive.Value;

        article.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("knowledge/{id}")]
    public async Task<IActionResult> DeleteKnowledgeArticle(int id, [FromQuery] string shop)
    {
        var article = await _db.KnowledgeArticles
            .FirstOrDefaultAsync(a => a.Id == id && a.ShopDomain == shop);

        if (article == null)
        {
            return NotFound(new { success = false, error = "Article not found" });
        }

        _db.KnowledgeArticles.Remove(article);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

public class UpdateSettingsRequest
{
    public string? BotName { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? Tone { get; set; }
    public string? PreferredAiProvider { get; set; }
    public string? FallbackAiProvider { get; set; }
    public bool? EnableOrderTracking { get; set; }
    public bool? EnableProductRecommendations { get; set; }
    public bool? EnableReturns { get; set; }
    public bool? EnableHumanEscalation { get; set; }
    public decimal? ConfidenceThreshold { get; set; }
    public int? MaxTokens { get; set; }
    public string? EscalationEmail { get; set; }
}

public class UpdateWidgetConfigRequest
{
    public string? Position { get; set; }
    public int? OffsetX { get; set; }
    public int? OffsetY { get; set; }
    public string? TriggerStyle { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? TextColor { get; set; }
    public string? HeaderBackgroundColor { get; set; }
    public string? HeaderTextColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public string? HeaderTitle { get; set; }
    public string? TriggerText { get; set; }
    public bool? AutoOpenOnFirstVisit { get; set; }
    public int? AutoOpenDelaySeconds { get; set; }
    public bool? ShowTypingIndicator { get; set; }
    public bool? EnableSoundNotifications { get; set; }
    public string? PlaceholderText { get; set; }
    public bool? ShowPoweredBy { get; set; }
    public string? CustomCss { get; set; }
}

public class CreateKnowledgeArticleRequest
{
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Tags { get; set; }
}

public class UpdateKnowledgeArticleRequest
{
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? Content { get; set; }
    public string? Tags { get; set; }
    public bool? IsActive { get; set; }
}
