namespace Algora.Chatbot.Infrastructure.Shopify;

public class ShopifyOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string AppUrl { get; set; } = string.Empty;
    public string Scopes { get; set; } = "read_orders,read_customers,read_products,write_script_tags";
    public string? WebhookSecret { get; set; }
}
