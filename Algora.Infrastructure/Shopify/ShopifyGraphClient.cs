using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using ShopifySharp.GraphQL;
using ShopifySharp.Services.Graph;
using System.Reflection;
using System.Text.Json;
using System;
using System.Collections.Generic;

namespace Algora.Infrastructure.Shopify
{
    /// <summary>
    /// Lightweight wrapper around ShopifySharp's <see cref="GraphService"/> that
    /// provides a safe and consistent way to execute GraphQL queries against a shop.
    /// </summary>
    /// <remarks>
    /// Responsibilities:
    /// - Build a <see cref="GraphRequest"/> from a GraphQL string and optional variables.
    /// - Call <see cref="GraphService.PostAsync(GraphRequest)"/> and normalize the variety of
    ///   response shapes returned by different ShopifySharp versions (string, JsonElement, GraphResult.Json, etc.).
    /// - Detect and log GraphQL errors if present in the raw JSON payload.
    /// - Extract the top-level "data" object and deserialize it into the requested type <typeparamref name="T"/>.
    /// - Catch and log Shopify-specific and general exceptions, returning <c>default</c> on error.
    /// </remarks>
    public class ShopifyGraphClient : IShopifyGraphClient
    {
        private readonly GraphService _graph;
        private readonly ILogger<ShopifyGraphClient> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="ShopifyGraphClient"/>.
        /// </summary>
        /// <param name="context">Shop context providing the shop domain and access token. Must not be null.</param>
        /// <param name="logger">Logger instance used to emit warnings and errors.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="logger"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If shop domain or access token is missing from the provided <paramref name="context"/>.</exception>
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

        /// <summary>
        /// Executes a GraphQL query against Shopify and returns the deserialized "data" payload as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type representing the "data" portion of the GraphQL response.</typeparam>
        /// <param name="gql">GraphQL query or mutation string. Cannot be null or empty.</param>
        /// <param name="variables">
        /// Optional anonymous object containing variables to pass to the query.
        /// Public properties on the object will be copied into a variables dictionary sent to Shopify.
        /// </param>
        /// <returns>
        /// An instance of <typeparamref name="T"/> deserialized from the response "data" object, or <c>null</c>/<c>default</c>
        /// if the call failed, the response contained no "data" field, or deserialization failed.
        /// </returns>
        /// <remarks>
        /// Behavior details:
        /// - The method accepts either raw JSON responses (string), boxed <see cref="JsonElement"/>, or types that contain
        ///   a public "Json" property (for example, <c>GraphResult</c>/GraphResponse wrapper types).
        /// - It will normalize those shapes into a single JSON string and then parse it to find the "data" property.
        /// - If the returned JSON contains a top-level "errors" member, that payload is logged as an error but the method
        ///   will still attempt to deserialize "data" if present.
        /// - Shopify-specific exceptions are caught and logged; the method returns <c>default</c> in error cases.
        /// </remarks>
        /// <exception cref="ArgumentException">If <paramref name="gql"/> is null or empty.</exception>
        public async Task<T?> QueryAsync<T>(string gql, object? variables = null)
        {
            if (string.IsNullOrWhiteSpace(gql))
                throw new ArgumentException("GraphQL query cannot be null or empty.", nameof(gql));

            // Build variables dictionary from anonymous object
            var variableDict = new Dictionary<string, object?>();
            if (variables != null)
            {
                foreach (var prop in variables.GetType().GetProperties())
                    variableDict[prop.Name] = prop.GetValue(variables);
            }

            // Convert variableDict into a Dictionary<string, object> suitable for GraphRequest.Variables
            Dictionary<string, object>? variablesToSend = null;
            if (variableDict.Count > 0)
            {
                variablesToSend = new Dictionary<string, object>(variableDict.Count);
                foreach (var kv in variableDict)
                {
                    object? outVal = null;

                    if (kv.Value is JsonElement je)
                    {
                        // Convert JsonElement to primitive CLR values where possible
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.String:
                                outVal = je.GetString();
                                break;
                            case JsonValueKind.Number:
                                if (je.TryGetInt64(out var li)) outVal = li;
                                else if (je.TryGetDouble(out var d)) outVal = d;
                                else outVal = je.GetRawText();
                                break;
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                outVal = je.GetBoolean();
                                break;
                            case JsonValueKind.Null:
                                outVal = null;
                                break;
                            default:
                                // For objects/arrays deserialize into plain CLR object
                                outVal = JsonSerializer.Deserialize<object>(je.GetRawText());
                                break;
                        }
                    }
                    else if (kv.Value != null && kv.Value.GetType().Assembly == typeof(GraphService).Assembly)
                    {
                        // Avoid passing SDK wrapper types directly; serialize then deserialize to plain object
                        var ser = JsonSerializer.Serialize(kv.Value);
                        outVal = JsonSerializer.Deserialize<object>(ser);
                    }
                    else
                    {
                        outVal = kv.Value;
                    }

                    // Even if outVal is null we can store it in the dictionary (boxed as object)
                    variablesToSend[kv.Key] = outVal!;
                }

                try
                {
                    _logger.LogDebug("Graph variables: {VariablesJson}", JsonSerializer.Serialize(variablesToSend));
                }
                catch
                {
                    // ignore logging serialization errors
                }
            }

            // Build the GraphRequest (pass null when there are no variables)
            var request = new GraphRequest
            {
                Query = gql,
                Variables = variablesToSend == null || variablesToSend.Count == 0 ? null : variablesToSend
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

