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

        // ----- WhatsApp entities -----
        public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; } = null!;
        public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; } = null!;
        public DbSet<WhatsAppConversation> WhatsAppConversations { get; set; } = null!;
        public DbSet<WhatsAppCampaign> WhatsAppCampaigns { get; set; } = null!;

        // ----- SMS entities -----
        public DbSet<SmsTemplate> SmsTemplates { get; set; } = null!;
        public DbSet<SmsMessage> SmsMessages { get; set; } = null!;

        // ----- Tagging entities -----
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<EntityTag> EntityTags { get; set; } = null!;

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
                b.HasIndex(x => new { x.ShopDomain, x.PlatformProductId }).IsUnique();
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Price).HasPrecision(18, 4);
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
                b.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShippingLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Price).HasPrecision(18, 4);
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
                b.HasIndex(x => new { x.ShopDomain, x.InvoiceNumber }).IsUnique();
                b.HasOne(x => x.Order).WithMany(o => o.Invoices).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InvoiceLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.UnitPrice).HasPrecision(18, 4);
                b.Property(x => x.LineTotal).HasPrecision(18, 4);
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

            // ==================== WHATSAPP ENTITIES ====================

            modelBuilder.Entity<WhatsAppTemplate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.Property(x => x.Language).HasMaxLength(10);
                b.Property(x => x.Category).HasMaxLength(20);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasIndex(x => new { x.ShopDomain, x.Name, x.Language }).IsUnique();
            });

            modelBuilder.Entity<WhatsAppMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                b.Property(x => x.Direction).HasMaxLength(10);
                b.Property(x => x.MessageType).HasMaxLength(20);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
            });

            modelBuilder.Entity<WhatsAppConversation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(x => new { x.ShopDomain, x.PhoneNumber }).IsUnique();
            });

            modelBuilder.Entity<WhatsAppCampaign>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Status).HasMaxLength(20);
                b.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Segment).WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.SetNull);
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

            // ==================== TAGGING ENTITIES ====================

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
        }
    }
}