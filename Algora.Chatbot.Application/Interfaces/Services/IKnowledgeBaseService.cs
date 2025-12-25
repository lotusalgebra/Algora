using Algora.Chatbot.Domain.Entities;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IKnowledgeBaseService
{
    Task<List<KnowledgeArticle>> SearchArticlesAsync(string shopDomain, string query, int limit = 5, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle?> GetArticleAsync(int id, CancellationToken cancellationToken = default);
    Task<List<KnowledgeArticle>> GetAllArticlesAsync(string shopDomain, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle> CreateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle> UpdateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task DeleteArticleAsync(int id, CancellationToken cancellationToken = default);
}
