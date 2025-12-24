using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for AI-powered response suggestions using existing AI providers.
/// </summary>
public class AiResponseService : IAiResponseService
{
    private readonly AppDbContext _db;
    private readonly IAiTextProvider _aiProvider;
    private readonly ILogger<AiResponseService> _logger;

    public AiResponseService(
        AppDbContext db,
        IAiTextProvider aiProvider,
        ILogger<AiResponseService> logger)
    {
        _db = db;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<AiSuggestionDto>> GenerateSuggestionsAsync(int conversationId, int suggestionCount = 3)
    {
        var conversation = await _db.ConversationThreads
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found");

        var messages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Take(10)
            .ToListAsync();

        messages.Reverse();

        var quickReplies = await _db.QuickReplies
            .Where(r => r.ShopDomain == conversation.ShopDomain && r.IsActive)
            .Take(5)
            .ToListAsync();

        var prompt = BuildSuggestionPrompt(conversation, messages, quickReplies, suggestionCount);

        var suggestions = new List<AiSuggestion>();
        var (providerName, modelName) = _aiProvider.GetProviderInfo();

        try
        {
            var response = await _aiProvider.GenerateTextAsync(prompt);
            var parsedSuggestions = ParseSuggestions(response, suggestionCount);

            foreach (var (text, confidence) in parsedSuggestions)
            {
                var suggestion = new AiSuggestion
                {
                    ConversationThreadId = conversationId,
                    ConversationMessageId = messages.LastOrDefault()?.Id,
                    SuggestionText = text,
                    Confidence = confidence,
                    Provider = providerName,
                    Model = modelName,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AiSuggestions.Add(suggestion);
                suggestions.Add(suggestion);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI suggestions for conversation {ConversationId}", conversationId);
            throw;
        }

        return suggestions.Select(MapToDto);
    }

    public async Task<AiSuggestionDto> AcceptSuggestionAsync(int suggestionId, bool wasModified = false)
    {
        var suggestion = await _db.AiSuggestions.FindAsync(suggestionId)
            ?? throw new InvalidOperationException($"Suggestion {suggestionId} not found");

        suggestion.WasAccepted = true;
        suggestion.WasModified = wasModified;
        suggestion.AcceptedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(suggestion);
    }

    public async Task<string> GenerateContextualReplyAsync(int conversationId, string context)
    {
        var conversation = await _db.ConversationThreads.FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found");

        var messages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Take(5)
            .ToListAsync();

        messages.Reverse();

        var prompt = $@"You are a helpful customer service agent. Based on the conversation below and the given context, write a professional and helpful response.

Context: {context}

Conversation:
{string.Join("\n", messages.Select(m => $"{(m.Direction == "inbound" ? "Customer" : "Agent")}: {m.Content}"))}

Write a single, clear response:";

        return await _aiProvider.GenerateTextAsync(prompt);
    }

    public async Task<SentimentAnalysisDto> AnalyzeSentimentAsync(string text)
    {
        var prompt = $@"Analyze the sentiment of the following text and respond in JSON format:
{{
  ""sentiment"": ""positive"" or ""negative"" or ""neutral"",
  ""confidenceScore"": 0.0 to 1.0,
  ""summary"": ""brief summary"",
  ""keyPhrases"": [""phrase1"", ""phrase2""],
  ""requiresUrgentAttention"": true or false
}}

Text: {text}";

        var response = await _aiProvider.GenerateTextAsync(prompt);

        try
        {
            // Parse JSON response - simplified parsing
            var sentiment = "neutral";
            var confidence = 0.7m;
            var requiresUrgent = false;

            if (response.Contains("\"positive\"", StringComparison.OrdinalIgnoreCase))
                sentiment = "positive";
            else if (response.Contains("\"negative\"", StringComparison.OrdinalIgnoreCase))
            {
                sentiment = "negative";
                requiresUrgent = response.Contains("\"requiresUrgentAttention\": true", StringComparison.OrdinalIgnoreCase);
            }

            return new SentimentAnalysisDto(
                sentiment,
                confidence,
                null,
                null,
                requiresUrgent
            );
        }
        catch
        {
            return new SentimentAnalysisDto("neutral", 0.5m, null, null, false);
        }
    }

    public async Task<string> SummarizeConversationAsync(int conversationId)
    {
        var messages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        if (messages.Count == 0)
            return "No messages in this conversation.";

        var prompt = $@"Summarize the following customer service conversation in 2-3 sentences:

{string.Join("\n", messages.Select(m => $"{(m.Direction == "inbound" ? "Customer" : "Agent")}: {m.Content}"))}

Summary:";

        return await _aiProvider.GenerateTextAsync(prompt);
    }

    public async Task<IEnumerable<string>> SuggestTagsAsync(int conversationId)
    {
        var conversation = await _db.ConversationThreads.FindAsync(conversationId);
        if (conversation == null) return Enumerable.Empty<string>();

        var messages = await _db.ConversationMessages
            .Where(m => m.ConversationThreadId == conversationId)
            .OrderBy(m => m.SentAt)
            .Take(10)
            .ToListAsync();

        var prompt = $@"Suggest 3-5 relevant tags for categorizing this customer service conversation. Return only the tags as a comma-separated list.

Conversation:
{string.Join("\n", messages.Select(m => m.Content))}

Tags:";

        var response = await _aiProvider.GenerateTextAsync(prompt);
        return response.Split(',').Select(t => t.Trim().ToLower()).Where(t => !string.IsNullOrEmpty(t)).Take(5);
    }

    private static string BuildSuggestionPrompt(
        ConversationThread conversation,
        List<ConversationMessage> messages,
        List<QuickReply> quickReplies,
        int suggestionCount)
    {
        var customerName = conversation.CustomerName ?? "Customer";

        return $@"You are a professional customer service agent. Generate {suggestionCount} different response suggestions for the following conversation.

Customer: {customerName}
Channel: {conversation.Channel}
Subject: {conversation.Subject ?? "General inquiry"}

Conversation:
{string.Join("\n", messages.Select(m => $"{(m.Direction == "inbound" ? "Customer" : "Agent")}: {m.Content}"))}

{(quickReplies.Count > 0 ? $"Available quick replies for reference:\n{string.Join("\n", quickReplies.Select(r => $"- {r.Title}: {r.Content}"))}\n" : "")}

Generate {suggestionCount} different response options. For each, indicate a confidence score (0-100) based on how appropriate it is.
Format: SUGGESTION|confidence_score

Responses:";
    }

    private static List<(string Text, decimal Confidence)> ParseSuggestions(string response, int maxCount)
    {
        var results = new List<(string Text, decimal Confidence)>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (results.Count >= maxCount) break;

            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Try to parse "SUGGESTION|confidence" format
            var parts = trimmed.Split('|');
            if (parts.Length == 2 && decimal.TryParse(parts[1].Trim(), out var confidence))
            {
                results.Add((parts[0].Trim(), Math.Clamp(confidence, 0, 100)));
            }
            else if (!trimmed.StartsWith("SUGGESTION") && trimmed.Length > 10)
            {
                // Just use the text with default confidence
                results.Add((trimmed.TrimStart('-', '*', '1', '2', '3', '.', ' '), 75m));
            }
        }

        return results;
    }

    private static AiSuggestionDto MapToDto(AiSuggestion s) => new(
        s.Id,
        s.ConversationThreadId,
        s.ConversationMessageId,
        s.SuggestionText,
        s.Confidence,
        s.Provider,
        s.Model,
        s.TokensUsed,
        s.EstimatedCost,
        s.WasAccepted,
        s.WasModified,
        s.CreatedAt,
        s.AcceptedAt
    );
}
