using Algora.Chatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algora.Chatbot.Infrastructure.Data;

public class ChatbotDbContext : DbContext
{
    public ChatbotDbContext(DbContextOptions<ChatbotDbContext> options) : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ChatbotSettings> ChatbotSettings => Set<ChatbotSettings>();
    public DbSet<WidgetConfiguration> WidgetConfigurations => Set<WidgetConfiguration>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<ConversationAnalytics> ConversationAnalytics => Set<ConversationAnalytics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Shop
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.Property(e => e.OfflineAccessToken).HasMaxLength(500);
            entity.Property(e => e.ShopName).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.Timezone).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(10);
            entity.Property(e => e.PlanName).HasMaxLength(50);
        });

        // ChatbotSettings
        modelBuilder.Entity<ChatbotSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.ShopDomain).IsUnique();
            entity.Property(e => e.PreferredAiProvider).HasMaxLength(50);
            entity.Property(e => e.FallbackAiProvider).HasMaxLength(50);
            entity.Property(e => e.BotName).HasMaxLength(100);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(1000);
            entity.Property(e => e.Tone).HasMaxLength(50);
            entity.Property(e => e.EscalationEmail).HasMaxLength(255);
            entity.Property(e => e.EscalationWebhookUrl).HasMaxLength(500);
            entity.Property(e => e.OutOfHoursMessage).HasMaxLength(500);
            entity.Property(e => e.ConfidenceThreshold).HasPrecision(3, 2);

            entity.HasOne(e => e.WidgetConfiguration)
                .WithOne()
                .HasForeignKey<ChatbotSettings>(e => e.WidgetConfigurationId);
        });

        // WidgetConfiguration
        modelBuilder.Entity<WidgetConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.ShopDomain).IsUnique();
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.TriggerStyle).HasMaxLength(50);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.SecondaryColor).HasMaxLength(20);
            entity.Property(e => e.TextColor).HasMaxLength(20);
            entity.Property(e => e.HeaderBackgroundColor).HasMaxLength(20);
            entity.Property(e => e.HeaderTextColor).HasMaxLength(20);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.HeaderTitle).HasMaxLength(100);
            entity.Property(e => e.TriggerText).HasMaxLength(100);
            entity.Property(e => e.PlaceholderText).HasMaxLength(200);
        });

        // Conversation
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VisitorId).HasMaxLength(100);
            entity.Property(e => e.CustomerEmail).HasMaxLength(255);
            entity.Property(e => e.CustomerName).HasMaxLength(255);
            entity.Property(e => e.CurrentPageUrl).HasMaxLength(2000);
            entity.Property(e => e.ReferrerUrl).HasMaxLength(2000);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.PrimaryIntent).HasMaxLength(100);
            entity.Property(e => e.OverallSentiment).HasPrecision(3, 2);
            entity.Property(e => e.EscalationReason).HasMaxLength(500);
            entity.Property(e => e.AssignedAgentEmail).HasMaxLength(255);
            entity.Property(e => e.FeedbackComment).HasMaxLength(1000);

            entity.HasIndex(e => e.ShopDomain);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ShopDomain, e.SessionId });

            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Conversation)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.DetectedIntent).HasMaxLength(100);
            entity.Property(e => e.IntentConfidence).HasPrecision(3, 2);
            entity.Property(e => e.Sentiment).HasPrecision(3, 2);
            entity.Property(e => e.AiProvider).HasMaxLength(50);
            entity.Property(e => e.AiModel).HasMaxLength(100);
            entity.Property(e => e.AiCost).HasPrecision(10, 6);

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // KnowledgeArticle
        modelBuilder.Entity<KnowledgeArticle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.KeyPhrases).HasMaxLength(1000);

            entity.HasIndex(e => e.ShopDomain);
            entity.HasIndex(e => new { e.ShopDomain, e.Category });
        });

        // Policy
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PolicyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.ShippingTimeframe).HasMaxLength(100);
            entity.Property(e => e.FreeShippingThreshold).HasPrecision(10, 2);

            entity.HasIndex(e => e.ShopDomain);
            entity.HasIndex(e => new { e.ShopDomain, e.PolicyType });
        });

        // Plan
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // License
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShopifyChargeId).HasMaxLength(100);

            entity.HasIndex(e => e.ShopDomain).IsUnique();

            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId);
        });

        // ConversationAnalytics
        modelBuilder.Entity<ConversationAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PeriodType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TotalAiCost).HasPrecision(10, 4);

            entity.HasIndex(e => e.ShopDomain);
            entity.HasIndex(e => new { e.ShopDomain, e.SnapshotDate, e.PeriodType });
        });
    }
}
