using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure
{

    public static class ShopifyHmac
    {
        public static bool IsAuthenticWebhook(string hmacHeaderBase64, string body, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var calc = Convert.ToBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(calc), Encoding.UTF8.GetBytes(hmacHeaderBase64 ?? string.Empty));
        }

        public static bool IsAuthenticQuery(IDictionary<string, string> query, string secret)
        {
            var filtered = new List<string>();
            foreach (var kvp in query)
            {
                if (kvp.Key == "hmac" || kvp.Key == "signature") continue;
                filtered.Add(kvp.Key + "=" + kvp.Value);
            }
            filtered.Sort(StringComparer.Ordinal);
            var message = string.Join("&", filtered);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            var provided = query.TryGetValue("hmac", out var h) ? h : "";
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(provided));
        }
    }
}
