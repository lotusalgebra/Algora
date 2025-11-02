using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Algora.Infrastructure.Services
{
    public class ShopifyOAuthService : IShopifyOAuthService
    {
        private readonly ShopifyOptions _opt;
        private readonly AppDbContext _db;
        private readonly HttpClient _http = new();

        public ShopifyOAuthService(IOptions<ShopifyOptions> opt, AppDbContext db)
        {
            _opt = opt.Value;
            _db = db;
        }

        public async Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code)
        {
            var url = $"https://{shopDomain}/admin/oauth/access_token";
            var payload = new { client_id = _opt.ApiKey, client_secret = _opt.ApiSecret, code };
            var resp = await _http.PostAsJsonAsync(url, payload);
            resp.EnsureSuccessStatusCode();
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var token = doc.RootElement.GetProperty("access_token").GetString()!;

            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
            if (shop is null)
            {
                shop = new Shop { Domain = shopDomain, OfflineAccessToken = token, InstalledAt = DateTime.UtcNow };
                _db.Shops.Add(shop);
            }
            else shop.OfflineAccessToken = token;

            await _db.SaveChangesAsync();
            return token;
        }

        public async Task<string?> GetAccessTokenAsync(string shopDomain)
        {
            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
            return shop?.OfflineAccessToken;
        }
    }



}
