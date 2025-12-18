using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for registering Shopify webhooks via the Admin API.
/// </summary>
public class WebhookRegistrationService : IWebhookRegistrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ShopifyOptions _options;
    private readonly ILogger<WebhookRegistrationService> _logger;

    // Webhook topics to register on app install
    private static readonly string[] RequiredWebhookTopics =
    [
        // App lifecycle
        "app/uninstalled",

        // Orders
        "orders/create",
        "orders/updated",
        "orders/cancelled",
        "orders/fulfilled",

        // Customers
        "customers/create",
        "customers/update",
        "customers/delete",

        // Products
        "products/create",
        "products/update",
        "products/delete"
    ];

    public WebhookRegistrationService(
        IHttpClientFactory httpClientFactory,
        IOptions<ShopifyOptions> options,
        ILogger<WebhookRegistrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> RegisterAllWebhooksAsync(string shopDomain, string accessToken)
    {
        var callbackUrl = $"{_options.AppUrl}/webhooks/shopify";
        var allSuccess = true;

        _logger.LogInformation("Registering webhooks for shop {Shop} with callback {Callback}", shopDomain, callbackUrl);

        // First, get existing webhooks to avoid duplicates
        var existingWebhooks = await GetRegisteredWebhooksAsync(shopDomain, accessToken);
        var existingTopics = existingWebhooks.Select(w => w.Topic).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var topic in RequiredWebhookTopics)
        {
            if (existingTopics.Contains(topic))
            {
                _logger.LogDebug("Webhook {Topic} already registered for shop {Shop}", topic, shopDomain);
                continue;
            }

            var success = await RegisterWebhookAsync(shopDomain, accessToken, topic, callbackUrl);
            if (!success)
            {
                _logger.LogWarning("Failed to register webhook {Topic} for shop {Shop}", topic, shopDomain);
                allSuccess = false;
            }
            else
            {
                _logger.LogInformation("Registered webhook {Topic} for shop {Shop}", topic, shopDomain);
            }

            // Small delay to avoid rate limiting
            await Task.Delay(100);
        }

        return allSuccess;
    }

    public async Task<bool> RegisterWebhookAsync(string shopDomain, string accessToken, string topic, string callbackUrl)
    {
        try
        {
            var client = CreateClient(accessToken);
            var url = $"https://{shopDomain}/admin/api/2024-01/webhooks.json";

            var payload = new
            {
                webhook = new
                {
                    topic = topic,
                    address = callbackUrl,
                    format = "json"
                }
            };

            var response = await client.PostAsJsonAsync(url, payload);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to register webhook {Topic}: {StatusCode} - {Error}",
                topic, response.StatusCode, errorContent);

            // If webhook already exists (422), consider it a success
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity &&
                errorContent.Contains("already been taken", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Webhook {Topic} already exists for shop {Shop}", topic, shopDomain);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook {Topic} for shop {Shop}", topic, shopDomain);
            return false;
        }
    }

    public async Task<IEnumerable<WebhookInfo>> GetRegisteredWebhooksAsync(string shopDomain, string accessToken)
    {
        try
        {
            var client = CreateClient(accessToken);
            var url = $"https://{shopDomain}/admin/api/2024-01/webhooks.json";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get webhooks for shop {Shop}: {StatusCode}",
                    shopDomain, response.StatusCode);
                return Enumerable.Empty<WebhookInfo>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WebhooksResponse>(content, JsonOptions);

            return result?.Webhooks?.Select(w => new WebhookInfo(w.Id, w.Topic, w.Address, w.Format))
                ?? Enumerable.Empty<WebhookInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhooks for shop {Shop}", shopDomain);
            return Enumerable.Empty<WebhookInfo>();
        }
    }

    public async Task<bool> DeleteWebhookAsync(string shopDomain, string accessToken, long webhookId)
    {
        try
        {
            var client = CreateClient(accessToken);
            var url = $"https://{shopDomain}/admin/api/2024-01/webhooks/{webhookId}.json";

            var response = await client.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {WebhookId} for shop {Shop}", webhookId, shopDomain);
            return false;
        }
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private class WebhooksResponse
    {
        public List<WebhookDto>? Webhooks { get; set; }
    }

    private class WebhookDto
    {
        public long Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
    }
}
