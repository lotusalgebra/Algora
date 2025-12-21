using Algora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algora.Infrastructure.Data
{
    /// <summary>
    /// EF Core database context for application persistence.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ----- Core entities -----
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<WebhookLog> WebhookLogs { get; set; } = null!;
        public DbSet<License> Licenses { get; set; } = null!;
        public DbSet<ShopSettings> ShopSettings { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<AppConfiguration> AppConfigurations { get; set; } = null!;

        // ----- Plan entities -----
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<PlanChangeRequest> PlanChangeRequests { get; set; } = null!;

        // ----- E-commerce entities -----
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Collection> Collections { get; set; } = null!;
        public DbSet<ProductCollection> ProductCollections { get; set; } = null!;
        public DbSet<DiscountCode> DiscountCodes { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderLine> OrderLines { get; set; } = null!;
        public DbSet<ShippingLine> ShippingLines { get; set; } = null!;
        public DbSet<TaxLine> TaxLines { get; set; } = null!;
        public DbSet<Fulfillment> Fulfillments { get; set; } = null!;
        public DbSet<FulfillmentLine> FulfillmentLines { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<Refund> Refunds { get; set; } = null!;
        public DbSet<RefundLine> RefundLines { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;

        // ----- Communication entities -----
        public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<CommunicationSettings> CommunicationSettings { get; set; } = null!;

        // ----- Email Marketing entities -----
        public DbSet<EmailSubscriber> EmailSubscribers { get; set; } = null!;
        public DbSet<EmailList> EmailLists { get; set; } = null!;
        public DbSet<EmailListSubscriber> EmailListSubscribers { get; set; } = null!;
        public DbSet<CustomerSegment> CustomerSegments { get; set; } = null!;
        public DbSet<CustomerSegmentMember> CustomerSegmentMembers { get; set; } = null!;
        public DbSet<EmailCampaign> EmailCampaigns { get; set; } = null!;
        public DbSet<EmailCampaignRecipient> EmailCampaignRecipients { get; set; } = null!;
        public DbSet<EmailAutomation> EmailAutomations { get; set; } = null!;
        public DbSet<EmailAutomationStep> EmailAutomationSteps { get; set; } = null!;
        public DbSet<EmailAutomationEnrollment> EmailAutomationEnrollments { get; set; } = null!;

        // ----- SMS entities -----
        public DbSet<SmsTemplate> SmsTemplates { get; set; } = null!;
        public DbSet<SmsMessage> SmsMessages { get; set; } = null!;

        // ----- Inventory Prediction entities -----
        public DbSet<InventoryPrediction> InventoryPredictions { get; set; } = null!;
        public DbSet<InventoryAlert> InventoryAlerts { get; set; } = null!;
        public DbSet<InventoryAlertSettings> InventoryAlertSettings { get; set; } = null!;

        // ----- Upsell entities -----
        public DbSet<ProductAffinity> ProductAffinities { get; set; } = null!;
        public DbSet<UpsellOffer> UpsellOffers { get; set; } = null!;
        public DbSet<UpsellExperiment> UpsellExperiments { get; set; } = null!;
        public DbSet<UpsellConversion> UpsellConversions { get; set; } = null!;
        public DbSet<UpsellSettings> UpsellSettings { get; set; } = null!;

        // ----- Return entities -----
        public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;
        public DbSet<ReturnItem> ReturnItems { get; set; } = null!;
        public DbSet<ReturnReason> ReturnReasons { get; set; } = null!;
        public DbSet<ReturnSettings> ReturnSettings { get; set; } = null!;
        public DbSet<ReturnLabel> ReturnLabels { get; set; } = null!;

        // ----- Bundle entities -----
        public DbSet<Bundle> Bundles { get; set; } = null!;
        public DbSet<BundleItem> BundleItems { get; set; } = null!;
        public DbSet<BundleRule> BundleRules { get; set; } = null!;
        public DbSet<BundleRuleTier> BundleRuleTiers { get; set; } = null!;
        public DbSet<BundleSettings> BundleSettings { get; set; } = null!;

        // ----- Review entities -----
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<ReviewMedia> ReviewMedia { get; set; } = null!;
        public DbSet<ReviewImportJob> ReviewImportJobs { get; set; } = null!;
        public DbSet<ReviewEmailAutomation> ReviewEmailAutomations { get; set; } = null!;
        public DbSet<ReviewEmailLog> ReviewEmailLogs { get; set; } = null!;
        public DbSet<ReviewSettings> ReviewSettings { get; set; } = null!;

        // ----- Tagging entities -----
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<EntityTag> EntityTags { get; set; } = null!;
        // ----- Analytics entities -----
        public DbSet<AdsSpend> AdsSpends { get; set; } = null!;
        public DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; } = null!;
        public DbSet<CustomerLifetimeValue> CustomerLifetimeValues { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== CORE ENTITIES ====================

            modelBuilder.Entity<Shop>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Domain).IsRequired().HasMaxLength(200);
                b.HasIndex(x => x.Domain).IsUnique();
            });

            modelBuilder.Entity<License>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PlanName).HasMaxLength(100);
                b.Property(x => x.ChargeId).HasMaxLength(200);
                b.Property(x => x.Status).HasMaxLength(50);
                b.HasIndex(x => x.ShopDomain);
            });

            modelBuilder.Entity<Plan>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(50);
                b.Property(x => x.Description).HasMaxLength(500);
                b.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
                b.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<PlanChangeRequest>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.CurrentPlanName).IsRequired().HasMaxLength(50);
                b.Property(x => x.RequestedPlanName).IsRequired().HasMaxLength(50);
                b.Property(x => x.RequestType).IsRequired().HasMaxLength(20);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.AdminNotes).HasMaxLength(1000);
                b.Property(x => x.ProcessedBy).HasMaxLength(255);
                b.HasIndex(x => new { x.ShopDomain, x.Status });
            });

            modelBuilder.Entity<WebhookLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Shop).IsRequired().HasMaxLength(200);
                b.Property(x => x.Topic).IsRequired().HasMaxLength(100);
                b.HasIndex(x => new { x.Shop, x.ReceivedAt });
            });

            modelBuilder.Entity<ShopSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            modelBuilder.Entity<AuditLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
                b.Property(x => x.EntityId).IsRequired().HasMaxLength(100);
                b.Property(x => x.Action).IsRequired().HasMaxLength(50);
                b.HasIndex(x => new { x.ShopDomain, x.Timestamp });
            });

            modelBuilder.Entity<AppUser>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Email).IsRequired().HasMaxLength(255);
                b.HasIndex(x => new { x.ShopDomain, x.Email }).IsUnique();
            });

            modelBuilder.Entity<ApiKey>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Key).IsRequired().HasMaxLength(64);
                b.HasIndex(x => x.Key).IsUnique();
            });

            // ==================== E-COMMERCE ENTITIES ====================

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Email).HasMaxLength(255);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformCustomerId }).IsUnique();
                b.HasIndex(x => new { x.ShopDomain, x.Email });
            });

            modelBuilder.Entity<Address>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Title).IsRequired().HasMaxLength(500);
                b.Property(x => x.Price).HasPrecision(18, 4);
                b.Property(x => x.CompareAtPrice).HasPrecision(18, 4);
                b.Property(x => x.CostOfGoodsSold).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductId }).IsUnique();
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Price).HasPrecision(18, 4);
                b.Property(x => x.CompareAtPrice).HasPrecision(18, 4);
                b.Property(x => x.Weight).HasPrecision(18, 4);
                b.Property(x => x.CostOfGoodsSold).HasPrecision(18, 4);
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductImage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Src).IsRequired().HasMaxLength(1000);
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Collection>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Title).IsRequired().HasMaxLength(500);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformCollectionId }).IsUnique();
            });

            modelBuilder.Entity<ProductCollection>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Collection).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.ProductId, x.CollectionId }).IsUnique();
            });

            modelBuilder.Entity<DiscountCode>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Code).IsRequired().HasMaxLength(100);
                b.Property(x => x.Value).HasPrecision(18, 4);
                b.Property(x => x.MinimumOrderAmount).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.Code }).IsUnique();
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
                b.Property(x => x.Subtotal).HasPrecision(18, 4);
                b.Property(x => x.TaxTotal).HasPrecision(18, 4);
                b.Property(x => x.ShippingTotal).HasPrecision(18, 4);
                b.Property(x => x.DiscountTotal).HasPrecision(18, 4);
                b.Property(x => x.GrandTotal).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformOrderId }).IsUnique();
                b.HasOne(x => x.Customer).WithMany(c => c.Orders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<OrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.UnitPrice).HasPrecision(18, 4);
                b.Property(x => x.LineTotal).HasPrecision(18, 4);
                b.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                b.Property(x => x.TaxAmount).HasPrecision(18, 4);
                b.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShippingLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Price).HasPrecision(18, 4);
                b.Property(x => x.DiscountedPrice).HasPrecision(18, 4);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaxLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Rate).HasPrecision(18, 6);
                b.Property(x => x.Amount).HasPrecision(18, 4);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Fulfillment>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FulfillmentLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Fulfillment).WithMany().HasForeignKey(x => x.FulfillmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Transaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Amount).HasPrecision(18, 4);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Refund>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Amount).HasPrecision(18, 4);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefundLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Amount).HasPrecision(18, 4);
                b.HasOne(x => x.Refund).WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Invoice>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
                b.Property(x => x.Subtotal).HasPrecision(18, 4);
                b.Property(x => x.Tax).HasPrecision(18, 4);
                b.Property(x => x.Total).HasPrecision(18, 4);
                b.Property(x => x.Discount).HasPrecision(18, 4);
                b.Property(x => x.Shipping).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.InvoiceNumber }).IsUnique();
                b.HasOne(x => x.Order).WithMany(o => o.Invoices).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InvoiceLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.UnitPrice).HasPrecision(18, 4);
                b.Property(x => x.LineTotal).HasPrecision(18, 4);
                b.Property(x => x.Discount).HasPrecision(18, 4);
                b.Property(x => x.Tax).HasPrecision(18, 4);
                b.HasOne(x => x.Invoice).WithMany(i => i.Lines).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== COMMUNICATION ENTITIES ====================

            modelBuilder.Entity<EmailTemplate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.Property(x => x.TemplateType).IsRequired().HasMaxLength(50);
                b.HasIndex(x => new { x.ShopDomain, x.Name }).IsUnique();
            });

            modelBuilder.Entity<Notification>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CommunicationSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            // ==================== EMAIL MARKETING ENTITIES ====================

            modelBuilder.Entity<EmailSubscriber>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Email).IsRequired().HasMaxLength(255);
                b.Property(x => x.Status).HasMaxLength(20);
                b.Property(x => x.Source).HasMaxLength(50);
                b.HasIndex(x => new { x.ShopDomain, x.Email }).IsUnique();
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EmailList>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.HasIndex(x => new { x.ShopDomain, x.Name }).IsUnique();
            });

            modelBuilder.Entity<EmailListSubscriber>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.EmailList).WithMany(l => l.Subscribers).HasForeignKey(x => x.EmailListId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.EmailSubscriber).WithMany(s => s.ListSubscriptions).HasForeignKey(x => x.EmailSubscriberId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.EmailListId, x.EmailSubscriberId }).IsUnique();
            });

            modelBuilder.Entity<CustomerSegment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.Property(x => x.SegmentType).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.Name }).IsUnique();
            });

            modelBuilder.Entity<CustomerSegmentMember>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Segment).WithMany(s => s.Members).HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Subscriber).WithMany().HasForeignKey(x => x.SubscriberId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EmailCampaign>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Subject).IsRequired().HasMaxLength(500);
                b.Property(x => x.Status).HasMaxLength(20);
                b.Property(x => x.CampaignType).HasMaxLength(20);
                b.HasOne(x => x.EmailTemplate).WithMany().HasForeignKey(x => x.EmailTemplateId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Segment).WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(x => new { x.ShopDomain, x.Status });
            });

            modelBuilder.Entity<EmailCampaignRecipient>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired().HasMaxLength(255);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasOne(x => x.EmailCampaign).WithMany(c => c.Recipients).HasForeignKey(x => x.EmailCampaignId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Subscriber).WithMany().HasForeignKey(x => x.SubscriberId).OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(x => new { x.EmailCampaignId, x.Email }).IsUnique();
            });

            modelBuilder.Entity<EmailAutomation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.TriggerType).IsRequired().HasMaxLength(50);
                b.Property(x => x.Revenue).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.TriggerType });
            });

            modelBuilder.Entity<EmailAutomationStep>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.StepType).IsRequired().HasMaxLength(20);
                b.HasOne(x => x.Automation).WithMany(a => a.Steps).HasForeignKey(x => x.AutomationId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.EmailTemplate).WithMany().HasForeignKey(x => x.EmailTemplateId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EmailAutomationEnrollment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired().HasMaxLength(255);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasOne(x => x.Automation).WithMany(a => a.Enrollments).HasForeignKey(x => x.AutomationId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Subscriber).WithMany().HasForeignKey(x => x.SubscriberId).OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== SMS ENTITIES ====================

            modelBuilder.Entity<SmsTemplate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.Property(x => x.TemplateType).IsRequired().HasMaxLength(50);
                b.HasIndex(x => new { x.ShopDomain, x.Name }).IsUnique();
            });

            modelBuilder.Entity<SmsMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                b.Property(x => x.Status).HasMaxLength(20);
                b.Property(x => x.Cost).HasPrecision(18, 6);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
            });

            // ==================== INVENTORY PREDICTION ENTITIES ====================

            modelBuilder.Entity<InventoryPrediction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.ProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.VariantTitle).HasMaxLength(500);
                b.Property(x => x.Sku).HasMaxLength(100);
                b.Property(x => x.AverageDailySales).HasPrecision(18, 4);
                b.Property(x => x.SevenDayAverageSales).HasPrecision(18, 4);
                b.Property(x => x.ThirtyDayAverageSales).HasPrecision(18, 4);
                b.Property(x => x.NinetyDayAverageSales).HasPrecision(18, 4);
                b.Property(x => x.ConfidenceLevel).HasMaxLength(20);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductId, x.PlatformVariantId }).IsUnique();
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InventoryAlert>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.ProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.VariantTitle).HasMaxLength(500);
                b.Property(x => x.Sku).HasMaxLength(100);
                b.Property(x => x.AlertType).IsRequired().HasMaxLength(50);
                b.Property(x => x.Severity).IsRequired().HasMaxLength(20);
                b.Property(x => x.Message).HasMaxLength(1000);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.DismissReason).HasMaxLength(500);
                b.HasIndex(x => new { x.ShopDomain, x.Status, x.CreatedAt });
                b.HasIndex(x => new { x.ShopDomain, x.Severity });
                b.HasOne(x => x.InventoryPrediction).WithMany().HasForeignKey(x => x.InventoryPredictionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InventoryAlertSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.NotificationEmail).HasMaxLength(255);
                b.Property(x => x.NotificationPhone).HasMaxLength(20);
                b.Property(x => x.WhatsAppPhone).HasMaxLength(20);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            // ==================== TAGGING ENTITIES ====================

            // ==================== UPSELL ENTITIES ====================

            modelBuilder.Entity<ProductAffinity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.ProductTitleA).IsRequired().HasMaxLength(500);
                b.Property(x => x.ProductTitleB).IsRequired().HasMaxLength(500);
                b.Property(x => x.SupportScore).HasPrecision(18, 4);
                b.Property(x => x.ConfidenceScore).HasPrecision(18, 4);
                b.Property(x => x.LiftScore).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductIdA });
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductIdB });
            });

            modelBuilder.Entity<UpsellOffer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.RecommendedProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.RecommendedProductImageUrl).HasMaxLength(1000);
                b.Property(x => x.RecommendedProductPrice).HasPrecision(18, 4);
                b.Property(x => x.DiscountType).HasMaxLength(20);
                b.Property(x => x.DiscountValue).HasPrecision(18, 4);
                b.Property(x => x.DiscountCode).HasMaxLength(100);
                b.Property(x => x.Headline).HasMaxLength(200);
                b.Property(x => x.BodyText).HasMaxLength(1000);
                b.Property(x => x.ButtonText).HasMaxLength(50);
                b.Property(x => x.RecommendationSource).HasMaxLength(20);
                b.Property(x => x.ExperimentVariant).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.IsActive });
                b.HasOne(x => x.ProductAffinity).WithMany().HasForeignKey(x => x.ProductAffinityId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Experiment).WithMany(e => e.Offers).HasForeignKey(x => x.ExperimentId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UpsellExperiment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.PrimaryMetric).HasMaxLength(50);
                b.Property(x => x.MinimumDetectableEffect).HasPrecision(18, 4);
                b.Property(x => x.SignificanceLevel).HasPrecision(18, 4);
                b.Property(x => x.StatisticalPower).HasPrecision(18, 4);
                b.Property(x => x.ControlRevenue).HasPrecision(18, 4);
                b.Property(x => x.VariantARevenue).HasPrecision(18, 4);
                b.Property(x => x.VariantBRevenue).HasPrecision(18, 4);
                b.Property(x => x.ControlConversionRate).HasPrecision(18, 6);
                b.Property(x => x.VariantAConversionRate).HasPrecision(18, 6);
                b.Property(x => x.VariantBConversionRate).HasPrecision(18, 6);
                b.Property(x => x.PValueVsControl).HasPrecision(18, 6);
                b.Property(x => x.WinningLift).HasPrecision(18, 4);
                b.Property(x => x.WinningVariant).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.Status });
            });

            modelBuilder.Entity<UpsellConversion>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.SessionId).IsRequired().HasMaxLength(64);
                b.Property(x => x.AssignedVariant).HasMaxLength(20);
                b.Property(x => x.ConversionRevenue).HasPrecision(18, 4);
                b.Property(x => x.GeneratedCartUrl).HasMaxLength(2000);
                b.HasIndex(x => new { x.ShopDomain, x.ImpressionAt });
                b.HasIndex(x => new { x.ExperimentId, x.AssignedVariant });
                b.HasOne(x => x.SourceOrder).WithMany().HasForeignKey(x => x.SourceOrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.UpsellOffer).WithMany().HasForeignKey(x => x.UpsellOfferId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Experiment).WithMany(e => e.Conversions).HasForeignKey(x => x.ExperimentId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UpsellSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.DisplayLayout).HasMaxLength(20);
                b.Property(x => x.MinimumConfidenceScore).HasPrecision(18, 4);
                b.Property(x => x.PageTitle).HasMaxLength(200);
                b.Property(x => x.ThankYouMessage).HasMaxLength(500);
                b.Property(x => x.UpsellSectionTitle).HasMaxLength(200);
                b.Property(x => x.LogoUrl).HasMaxLength(1000);
                b.Property(x => x.PrimaryColor).HasMaxLength(20);
                b.Property(x => x.SecondaryColor).HasMaxLength(20);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            // ==================== RETURN ENTITIES ====================

            modelBuilder.Entity<ReturnRequest>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.RequestNumber).IsRequired().HasMaxLength(50);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.CustomerEmail).HasMaxLength(255);
                b.Property(x => x.CustomerName).HasMaxLength(255);
                b.Property(x => x.ReasonCode).HasMaxLength(50);
                b.Property(x => x.CustomerNote).HasMaxLength(2000);
                b.Property(x => x.ApprovalNote).HasMaxLength(500);
                b.Property(x => x.RejectionReason).HasMaxLength(500);
                b.Property(x => x.TrackingNumber).HasMaxLength(100);
                b.Property(x => x.TrackingUrl).HasMaxLength(1000);
                b.Property(x => x.TrackingCarrier).HasMaxLength(50);
                b.Property(x => x.TotalRefundAmount).HasPrecision(18, 4);
                b.Property(x => x.ShippingCost).HasPrecision(18, 4);
                b.Property(x => x.Currency).HasMaxLength(10);
                b.HasIndex(x => new { x.ShopDomain, x.RequestNumber }).IsUnique();
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.ReturnReason).WithMany().HasForeignKey(x => x.ReturnReasonId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.ReturnLabel).WithMany().HasForeignKey(x => x.ReturnLabelId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Refund).WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReturnItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.VariantTitle).HasMaxLength(500);
                b.Property(x => x.Sku).HasMaxLength(100);
                b.Property(x => x.ImageUrl).HasMaxLength(1000);
                b.Property(x => x.UnitPrice).HasPrecision(18, 4);
                b.Property(x => x.RefundAmount).HasPrecision(18, 4);
                b.Property(x => x.CustomerNote).HasMaxLength(1000);
                b.Property(x => x.Condition).HasMaxLength(50);
                b.Property(x => x.ConditionNote).HasMaxLength(500);
                b.HasOne(x => x.ReturnRequest).WithMany(r => r.Items).HasForeignKey(x => x.ReturnRequestId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.ReturnReason).WithMany().HasForeignKey(x => x.ReturnReasonId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReturnReason>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Code).IsRequired().HasMaxLength(50);
                b.Property(x => x.DisplayText).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(500);
                b.HasIndex(x => new { x.ShopDomain, x.Code }).IsUnique();
            });

            modelBuilder.Entity<ReturnSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.AutoApprovalMaxAmount).HasPrecision(18, 4);
                b.Property(x => x.ShippoApiKey).HasMaxLength(200);
                b.Property(x => x.DefaultCarrier).HasMaxLength(50);
                b.Property(x => x.DefaultServiceLevel).HasMaxLength(50);
                b.Property(x => x.ReturnAddressName).HasMaxLength(100);
                b.Property(x => x.ReturnAddressCompany).HasMaxLength(200);
                b.Property(x => x.ReturnAddressStreet1).HasMaxLength(200);
                b.Property(x => x.ReturnAddressStreet2).HasMaxLength(200);
                b.Property(x => x.ReturnAddressCity).HasMaxLength(100);
                b.Property(x => x.ReturnAddressState).HasMaxLength(100);
                b.Property(x => x.ReturnAddressZip).HasMaxLength(20);
                b.Property(x => x.ReturnAddressCountry).HasMaxLength(10);
                b.Property(x => x.ReturnAddressPhone).HasMaxLength(20);
                b.Property(x => x.ReturnAddressEmail).HasMaxLength(255);
                b.Property(x => x.NotificationEmail).HasMaxLength(255);
                b.Property(x => x.PageTitle).HasMaxLength(200);
                b.Property(x => x.PolicyText).HasMaxLength(2000);
                b.Property(x => x.LogoUrl).HasMaxLength(1000);
                b.Property(x => x.PrimaryColor).HasMaxLength(20);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            modelBuilder.Entity<ReturnLabel>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.ShippoTransactionId).IsRequired().HasMaxLength(100);
                b.Property(x => x.ShippoRateId).HasMaxLength(100);
                b.Property(x => x.ShippoShipmentId).HasMaxLength(100);
                b.Property(x => x.TrackingNumber).IsRequired().HasMaxLength(100);
                b.Property(x => x.TrackingUrl).HasMaxLength(1000);
                b.Property(x => x.Carrier).HasMaxLength(50);
                b.Property(x => x.ServiceLevel).HasMaxLength(50);
                b.Property(x => x.LabelUrl).IsRequired().HasMaxLength(1000);
                b.Property(x => x.LabelFormat).HasMaxLength(10);
                b.Property(x => x.Cost).HasPrecision(18, 4);
                b.Property(x => x.Currency).HasMaxLength(10);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.TrackingNumber });
            });

            // ----- Bundle entities -----
            modelBuilder.Entity<Bundle>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Slug).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(4000);
                b.Property(x => x.BundleType).IsRequired().HasMaxLength(20);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.DiscountType).IsRequired().HasMaxLength(20);
                b.Property(x => x.DiscountValue).HasPrecision(18, 4);
                b.Property(x => x.DiscountCode).HasMaxLength(100);
                b.Property(x => x.ImageUrl).HasMaxLength(1000);
                b.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
                b.Property(x => x.ShopifySyncStatus).HasMaxLength(20);
                b.Property(x => x.ShopifySyncError).HasMaxLength(500);
                b.Property(x => x.OriginalPrice).HasPrecision(18, 4);
                b.Property(x => x.BundlePrice).HasPrecision(18, 4);
                b.Property(x => x.Currency).HasMaxLength(10);
                b.HasIndex(x => new { x.ShopDomain, x.Slug }).IsUnique();
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasIndex(x => new { x.ShopDomain, x.IsActive });
            });

            modelBuilder.Entity<BundleItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ProductTitle).IsRequired().HasMaxLength(500);
                b.Property(x => x.VariantTitle).HasMaxLength(500);
                b.Property(x => x.Sku).HasMaxLength(100);
                b.Property(x => x.ImageUrl).HasMaxLength(1000);
                b.Property(x => x.UnitPrice).HasPrecision(18, 4);
                b.HasOne(x => x.Bundle).WithMany(b => b.Items).HasForeignKey(x => x.BundleId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.BundleId);
            });

            modelBuilder.Entity<BundleRule>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200);
                b.Property(x => x.DisplayLabel).HasMaxLength(200);
                b.HasOne(x => x.Bundle).WithMany(b => b.Rules).HasForeignKey(x => x.BundleId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.BundleId);
            });

            modelBuilder.Entity<BundleRuleTier>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.DiscountType).IsRequired().HasMaxLength(20);
                b.Property(x => x.DiscountValue).HasPrecision(18, 4);
                b.Property(x => x.DisplayLabel).HasMaxLength(200);
                b.HasOne(x => x.BundleRule).WithMany(r => r.Tiers).HasForeignKey(x => x.BundleRuleId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.BundleRuleId);
            });

            modelBuilder.Entity<BundleSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.DefaultDiscountType).HasMaxLength(20);
                b.Property(x => x.DefaultDiscountValue).HasPrecision(18, 4);
                b.Property(x => x.BundlePageTitle).HasMaxLength(200);
                b.Property(x => x.BundlePageDescription).HasMaxLength(2000);
                b.Property(x => x.DisplayLayout).HasMaxLength(20);
                b.Property(x => x.PrimaryColor).HasMaxLength(20);
                b.Property(x => x.SecondaryColor).HasMaxLength(20);
                b.Property(x => x.ShopifyProductType).HasMaxLength(100);
                b.Property(x => x.ShopifyProductTags).HasMaxLength(500);
                b.HasIndex(x => x.ShopDomain).IsUnique();
            });

            // ==================== REVIEW ENTITIES ====================

            modelBuilder.Entity<Review>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.ProductTitle).HasMaxLength(500);
                b.Property(x => x.ProductSku).HasMaxLength(100);
                b.Property(x => x.ReviewerName).IsRequired().HasMaxLength(200);
                b.Property(x => x.ReviewerEmail).HasMaxLength(255);
                b.Property(x => x.Title).HasMaxLength(500);
                b.Property(x => x.Body).HasMaxLength(10000);
                b.Property(x => x.Source).IsRequired().HasMaxLength(20);
                b.Property(x => x.SourceUrl).HasMaxLength(2000);
                b.Property(x => x.ExternalReviewId).HasMaxLength(100);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.ModerationNote).HasMaxLength(1000);
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductId });
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasIndex(x => new { x.ShopDomain, x.Source });
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.ImportJob).WithMany(j => j.Reviews).HasForeignKey(x => x.ImportJobId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReviewMedia>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.MediaType).IsRequired().HasMaxLength(20);
                b.Property(x => x.Url).IsRequired().HasMaxLength(2000);
                b.Property(x => x.ThumbnailUrl).HasMaxLength(2000);
                b.Property(x => x.AltText).HasMaxLength(500);
                b.HasOne(x => x.Review).WithMany(r => r.Media).HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => x.ReviewId);
            });

            modelBuilder.Entity<ReviewImportJob>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.SourceType).IsRequired().HasMaxLength(20);
                b.Property(x => x.SourceUrl).IsRequired().HasMaxLength(2000);
                b.Property(x => x.SourceProductId).HasMaxLength(100);
                b.Property(x => x.SourceProductTitle).HasMaxLength(500);
                b.Property(x => x.TargetProductTitle).HasMaxLength(500);
                b.Property(x => x.MappingMethod).IsRequired().HasMaxLength(20);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.ErrorMessage).HasMaxLength(2000);
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
                b.HasOne(x => x.TargetProduct).WithMany().HasForeignKey(x => x.TargetProductId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReviewEmailAutomation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.TriggerType).IsRequired().HasMaxLength(20);
                b.Property(x => x.Subject).IsRequired().HasMaxLength(500);
                b.Property(x => x.MinOrderValue).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.IsActive });
                b.HasOne(x => x.EmailTemplate).WithMany().HasForeignKey(x => x.EmailTemplateId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReviewEmailLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.CustomerEmail).IsRequired().HasMaxLength(255);
                b.Property(x => x.Status).IsRequired().HasMaxLength(20);
                b.Property(x => x.TrackingToken).HasMaxLength(64);
                b.Property(x => x.ErrorMessage).HasMaxLength(2000);
                b.HasIndex(x => new { x.ShopDomain, x.Status });
                b.HasIndex(x => new { x.ShopDomain, x.ScheduledAt });
                b.HasIndex(x => x.TrackingToken).IsUnique();
                b.HasOne(x => x.Automation).WithMany(a => a.EmailLogs).HasForeignKey(x => x.AutomationId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Review).WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReviewSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.WidgetTheme).HasMaxLength(20);
                b.Property(x => x.PrimaryColor).HasMaxLength(20);
                b.Property(x => x.AccentColor).HasMaxLength(20);
                b.Property(x => x.StarColor).HasMaxLength(20);
                b.Property(x => x.WidgetLayout).HasMaxLength(20);
                b.Property(x => x.TranslateToLanguage).HasMaxLength(10);
                b.Property(x => x.WidgetApiKey).IsRequired().HasMaxLength(64);
                b.Property(x => x.DefaultEmailFromName).HasMaxLength(100);
                b.Property(x => x.DefaultEmailFromAddress).HasMaxLength(255);
                b.HasIndex(x => x.ShopDomain).IsUnique();
                b.HasIndex(x => x.WidgetApiKey).IsUnique();
            });

            modelBuilder.Entity<Tag>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
                b.HasIndex(x => new { x.ShopDomain, x.Name, x.EntityType }).IsUnique();
            });

            modelBuilder.Entity<EntityTag>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
                b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.EntityType, x.EntityId, x.TagId }).IsUnique();
            });

            modelBuilder.Entity<AppConfiguration>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Key).IsRequired().HasMaxLength(100);
                b.Property(x => x.Value).HasMaxLength(1000);
                b.Property(x => x.Description).HasMaxLength(500);
                b.HasIndex(x => x.Key).IsUnique();
            });
            // ==================== ANALYTICS ENTITIES ====================

            modelBuilder.Entity<AdsSpend>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Platform).IsRequired().HasMaxLength(50);
                b.Property(x => x.CampaignName).HasMaxLength(500);
                b.Property(x => x.CampaignId).HasMaxLength(100);
                b.Property(x => x.Amount).HasPrecision(18, 4);
                b.Property(x => x.Currency).HasMaxLength(10);
                b.Property(x => x.Revenue).HasPrecision(18, 4);
                b.Property(x => x.Notes).HasMaxLength(1000);
                b.HasIndex(x => new { x.ShopDomain, x.SpendDate });
                b.HasIndex(x => new { x.ShopDomain, x.Platform });
            });

            modelBuilder.Entity<AnalyticsSnapshot>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PeriodType).IsRequired().HasMaxLength(20);
                b.Property(x => x.TotalRevenue).HasPrecision(18, 4);
                b.Property(x => x.TotalCOGS).HasPrecision(18, 4);
                b.Property(x => x.TotalAdsSpend).HasPrecision(18, 4);
                b.Property(x => x.GrossProfit).HasPrecision(18, 4);
                b.Property(x => x.NetProfit).HasPrecision(18, 4);
                b.Property(x => x.TotalRefunds).HasPrecision(18, 4);
                b.Property(x => x.AverageOrderValue).HasPrecision(18, 4);
                b.Property(x => x.ConversionRate).HasPrecision(18, 6);
                b.HasIndex(x => new { x.ShopDomain, x.SnapshotDate, x.PeriodType }).IsUnique();
            });

            modelBuilder.Entity<CustomerLifetimeValue>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.TotalSpent).HasPrecision(18, 4);
                b.Property(x => x.AverageOrderValue).HasPrecision(18, 4);
                b.Property(x => x.AverageDaysBetweenOrders).HasPrecision(18, 4);
                b.Property(x => x.PredictedLifetimeValue).HasPrecision(18, 4);
                b.Property(x => x.Segment).IsRequired().HasMaxLength(20);
                b.Property(x => x.AcquisitionSource).HasMaxLength(50);
                b.Property(x => x.TotalProfit).HasPrecision(18, 4);
                b.HasIndex(x => new { x.ShopDomain, x.CustomerId }).IsUnique();
                b.HasIndex(x => new { x.ShopDomain, x.Segment });
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}