using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Algora.Infrastructure
{
    public static class ShopifyWebhookVerifier
    {
        public static bool IsValidWebhook(string secret, HttpRequest request, string body)
        {
            if (string.IsNullOrWhiteSpace(secret)) return false;
            if (!request.Headers.TryGetValue("X-Shopify-Hmac-Sha256", out var provided)) return false;
            var computedHash = ComputeHash(secret, body);
            // constant-time comparison
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided!),
                Encoding.UTF8.GetBytes(computedHash));
        }

        private static string ComputeHash(string secret, string body)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var payload = Encoding.UTF8.GetBytes(body);
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(payload);
            return Convert.ToBase64String(hash);
        }
    }
}
