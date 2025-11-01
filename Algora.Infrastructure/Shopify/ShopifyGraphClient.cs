using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using ShopifySharp.GraphQL;
using ShopifySharp.Services.Graph;
using System.Reflection;
using System.Text.Json;

namespace Algora.Infrastructure.Shopify
{
    public class ShopifyGraphClient : IShopifyGraphClient
    {
        private readonly GraphService _graph;
        private readonly ILogger<ShopifyGraphClient> _logger;

        public ShopifyGraphClient(IShopContext context, ILogger<ShopifyGraphClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var domain = context.ShopDomain;
            var token = context.AccessToken;

            if (string.IsNullOrWhiteSpace(domain))
                throw new InvalidOperationException("Shop domain missing in context.");
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Shop access token missing in context.");

            _graph = new GraphService(domain, token);
        }

        public async Task<T?> QueryAsync<T>(string gql, object? variables = null)
        {
            if (string.IsNullOrWhiteSpace(gql))
                throw new ArgumentException("GraphQL query cannot be null or empty.", nameof(gql));

            // Build variables dictionary for Shopify GraphQL
            var variableDict = new Dictionary<string, object?>();
            if (variables != null)
            {
                foreach (var prop in variables.GetType().GetProperties())
                    variableDict[prop.Name] = prop.GetValue(variables);
            }

            var request = new GraphRequest
            {
                Query = gql,
                Variables = variableDict.Count == 0 ? null : variableDict
            };

            try
            {
                var response = await _graph.PostAsync(request);

                if (response == null)
                {
                    _logger.LogWarning("Shopify GraphQL returned null response.");
                    return default;
                }

                // Normalize response JSON into a string safely.
                object respObj = response!;
                string? json = null;

                if (respObj is string sResp)
                {
                    json = sResp;
                }
                else
                {
                    var jsonProp = respObj.GetType().GetProperty("Json", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (jsonProp != null)
                    {
                        var jsonVal = jsonProp.GetValue(respObj);
                        if (jsonVal is string s) json = s;
                        else if (jsonVal is JsonElement je) json = je.GetRawText();
                        else if (jsonVal != null) json = JsonSerializer.Serialize(jsonVal);
                    }
                }

                // Final fallback - serialize the whole response object
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = JsonSerializer.Serialize(respObj);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Shopify GraphQL returned empty JSON payload.");
                    return default;
                }

                // Detect GraphQL-level errors
                if (json.Contains("\"errors\"", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Shopify GraphQL returned errors: {Json}", json);
                }

                // Deserialize the "data" section from raw JSON
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogWarning("No 'data' field found in GraphQL JSON response: {Json}", json);
                    return default;
                }

                var dataJson = dataElement.GetRawText();
                var result = JsonSerializer.Deserialize<T>(dataJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (ShopifyException ex)
            {
                _logger.LogError(ex, "Shopify API call failed: {Message}", ex.Message);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Shopify GraphQL.");
                return default;
            }
        }
    }
}

