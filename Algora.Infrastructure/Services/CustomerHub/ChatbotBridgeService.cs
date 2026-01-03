using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Algora.Infrastructure.Services.CustomerHub;

public class ChatbotBridgeService : IChatbotBridgeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatbotBridgeService> _logger;
    private readonly string _chatbotApiUrl;

    public ChatbotBridgeService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChatbotBridgeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _chatbotApiUrl = configuration["Chatbot:ApiUrl"] ?? "https://localhost:5001";
    }

    public async Task<ChatbotConversationListResult> GetEscalatedConversationsAsync(
        string shopDomain,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations/escalated?shop={Uri.EscapeDataString(shopDomain)}&page={page}&pageSize={pageSize}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<List<ChatbotConversationDto>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                return new ChatbotConversationListResult
                {
                    Conversations = result.Data,
                    TotalCount = result.Pagination?.Total ?? result.Data.Count,
                    Page = result.Pagination?.Page ?? page,
                    PageSize = result.Pagination?.PageSize ?? pageSize,
                    TotalPages = result.Pagination?.TotalPages ?? 1
                };
            }

            return new ChatbotConversationListResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalated conversations for {Shop}", shopDomain);
            return new ChatbotConversationListResult();
        }
    }

    public async Task<ChatbotConversationListResult> GetConversationsAsync(
        string shopDomain,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations?shop={Uri.EscapeDataString(shopDomain)}&page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={Uri.EscapeDataString(status)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<List<ChatbotConversationDto>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                return new ChatbotConversationListResult
                {
                    Conversations = result.Data,
                    TotalCount = result.Pagination?.Total ?? result.Data.Count,
                    Page = result.Pagination?.Page ?? page,
                    PageSize = result.Pagination?.PageSize ?? pageSize,
                    TotalPages = result.Pagination?.TotalPages ?? 1
                };
            }

            return new ChatbotConversationListResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for {Shop}", shopDomain);
            return new ChatbotConversationListResult();
        }
    }

    public async Task<ChatbotConversationDetailDto?> GetConversationAsync(
        int conversationId,
        string shopDomain,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations/{conversationId}?shop={Uri.EscapeDataString(shopDomain)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ConversationDetailResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                return new ChatbotConversationDetailDto
                {
                    Id = result.Data.Id,
                    SessionId = result.Data.SessionId ?? "",
                    VisitorId = result.Data.VisitorId,
                    ShopifyCustomerId = result.Data.ShopifyCustomerId,
                    CustomerEmail = result.Data.CustomerEmail,
                    CustomerName = result.Data.CustomerName,
                    Status = result.Data.Status ?? "",
                    PrimaryIntent = result.Data.PrimaryIntent,
                    IsEscalated = result.Data.IsEscalated,
                    EscalationReason = null, // Not returned by current API
                    EscalatedAt = result.Data.EscalatedAt,
                    AssignedAgentEmail = null, // Not returned by current API
                    Rating = result.Data.Rating,
                    WasHelpful = result.Data.WasHelpful,
                    Feedback = result.Data.Feedback,
                    CurrentPageUrl = result.Data.CurrentPageUrl,
                    Messages = result.Data.Messages?.Select(m => new ChatbotMessageDto
                    {
                        Id = m.Id,
                        Role = m.Role ?? "",
                        Content = m.Content ?? "",
                        DetectedIntent = m.DetectedIntent,
                        IntentConfidence = m.IntentConfidence,
                        AiProvider = m.AiProvider,
                        TokensUsed = m.TokensUsed,
                        SuggestedActionsJson = m.SuggestedActions,
                        CreatedAt = m.CreatedAt
                    }).ToList() ?? new(),
                    CreatedAt = result.Data.CreatedAt,
                    UpdatedAt = result.Data.UpdatedAt
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId} for {Shop}", conversationId, shopDomain);
            return null;
        }
    }

    public async Task<bool> SendAgentMessageAsync(
        int conversationId,
        string shopDomain,
        SendAgentMessageDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations/{conversationId}/message?shop={Uri.EscapeDataString(shopDomain)}";
            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                message = message.Message,
                agentEmail = message.AgentEmail,
                agentName = message.AgentName
            }, cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiSuccessResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent message to conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<bool> AssignAgentAsync(
        int conversationId,
        string shopDomain,
        string agentEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations/{conversationId}/assign?shop={Uri.EscapeDataString(shopDomain)}";
            var response = await _httpClient.PostAsJsonAsync(url, new { agentEmail }, cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiSuccessResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning agent to conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<bool> ResolveConversationAsync(
        int conversationId,
        string shopDomain,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_chatbotApiUrl}/api/admin/v1/conversations/{conversationId}/resolve?shop={Uri.EscapeDataString(shopDomain)}";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiSuccessResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<int> GetEscalatedCountAsync(
        string shopDomain,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetEscalatedConversationsAsync(shopDomain, 1, 1, cancellationToken);
            return result.TotalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalated count for {Shop}", shopDomain);
            return 0;
        }
    }

    // Response classes for deserialization
    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public PaginationInfo? Pagination { get; set; }
    }

    private class ApiSuccessResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    private class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    private class ConversationDetailResponse
    {
        public bool Success { get; set; }
        public ConversationData? Data { get; set; }
    }

    private class ConversationData
    {
        public int Id { get; set; }
        public string? SessionId { get; set; }
        public string? VisitorId { get; set; }
        public long? ShopifyCustomerId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public string? PrimaryIntent { get; set; }
        public bool IsEscalated { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public int? Rating { get; set; }
        public bool? WasHelpful { get; set; }
        public string? Feedback { get; set; }
        public string? CurrentPageUrl { get; set; }
        public List<MessageData>? Messages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class MessageData
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? DetectedIntent { get; set; }
        public decimal? IntentConfidence { get; set; }
        public string? AiProvider { get; set; }
        public int? TokensUsed { get; set; }
        public string? SuggestedActions { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
