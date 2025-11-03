using Algora.Application.DTOs.Order;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Algora.Infrastructure.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _http;
        private readonly WhatsAppOptionsDto _opts;

        public WhatsAppService(IHttpClientFactory httpFactory, IOptions<WhatsAppOptionsDto> options)
        {
            _http = httpFactory.CreateClient();
            _opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            // Validate required configuration early so misconfiguration surfaces at startup/time of construction.
            if (string.IsNullOrWhiteSpace(_opts.AccessToken))
                throw new InvalidOperationException("WhatsApp access token is not configured.");
            if (string.IsNullOrWhiteSpace(_opts.PhoneNumberId))
                throw new InvalidOperationException("WhatsApp phone number id is not configured.");
        }

        public async Task SendOrderUpdateAsync(string toPhone, string message)
        {
            if (string.IsNullOrWhiteSpace(toPhone)) throw new ArgumentException("toPhone is required", nameof(toPhone));
            if (message is null) throw new ArgumentNullException(nameof(message));

            var url = $"https://graph.facebook.com/v20.0/{_opts.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone,
                type = "text",
                text = new { body = message }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _opts.AccessToken);

            // Use JsonContent (System.Net.Http.Json) to create JSON payload
            req.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var resp = await _http.SendAsync(req);
            var result = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"WhatsApp API Error: {resp.StatusCode} {result}");
        }
    }
}
