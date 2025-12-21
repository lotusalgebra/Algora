using Algora.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Services
{
    public class ShopifyGraphService : IShopifyGraphService
    {
        private readonly HttpClient _http = new();
        public async Task<string> PostAsync(string shopDomain, string accessToken, string query, object? variables = null)
        {
            var url = $"https://{shopDomain}/admin/api/2025-01/graphql.json";
            var body = new { query, variables };
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("X-Shopify-Access-Token", accessToken);
            req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }
    }
}
