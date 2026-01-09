namespace Algora.Web.Authorization;

/// <summary>
/// Constants for feature codes used in authorization.
/// These must match the feature codes in the PlanFeatures database table.
/// </summary>
public static class FeatureCodes
{
    // Commerce
    public const string Orders = "orders_management";
    public const string Products = "products_management";
    public const string Bundles = "product_bundles";
    public const string BulkImportExport = "bulk_import_export";
    public const string Customers = "customers";

    // Inventory
    public const string Inventory = "inventory_tracking";
    public const string InventoryAlerts = "inventory_alerts";
    public const string StockThresholds = "stock_thresholds";
    public const string MultiLocation = "multi_location";
    public const string InventoryPredictions = "inventory_predictions";

    // Communication
    public const string WhatsApp = "whatsapp";
    public const string EmailCampaigns = "email_campaigns";
    public const string Sms = "sms";

    // AI Tools
    public const string AiDescriptions = "ai_descriptions";
    public const string AiSeo = "ai_seo";
    public const string AiPricing = "ai_pricing";
    public const string AiChatbot = "ai_chatbot";
    public const string AiAltText = "ai_alt_text";

    // Analytics
    public const string AdvancedReports = "advanced_reports";

    // Operations
    public const string PurchaseOrders = "purchase_orders";
    public const string SupplierManagement = "supplier_management";
    public const string LabelDesigner = "label_designer";
    public const string BarcodeGenerator = "barcode_generator";
    public const string PackingSlips = "packing_slips";

    // Customer Hub
    public const string UnifiedInbox = "unified_inbox";
    public const string LoyaltyProgram = "loyalty_program";
    public const string Exchanges = "exchanges";
    public const string LiveChat = "live_chat";

    // Integrations
    public const string ApiAccess = "api_access";
    public const string Webhooks = "webhooks";

    // Marketing
    public const string UpsellOffers = "upsell_offers";
    public const string AbTesting = "ab_testing";
    public const string AbandonedCart = "abandoned_cart";

    // Returns
    public const string Returns = "returns";

    // Reviews
    public const string Reviews = "reviews";
}
