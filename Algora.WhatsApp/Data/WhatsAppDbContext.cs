using Algora.WhatsApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algora.WhatsApp.Data;

/// <summary>
/// DbContext for WhatsApp module entities.
/// </summary>
public class WhatsAppDbContext : DbContext
{
    public WhatsAppDbContext(DbContextOptions<WhatsAppDbContext> options) : base(options)
    {
    }

    public DbSet<WhatsAppTemplate> WhatsAppTemplates { get; set; } = null!;
    public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; } = null!;
    public DbSet<WhatsAppConversation> WhatsAppConversations { get; set; } = null!;
    public DbSet<WhatsAppCampaign> WhatsAppCampaigns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WhatsAppTemplate>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.ExternalTemplateId).HasMaxLength(100);
            b.Property(x => x.Language).IsRequired().HasMaxLength(10);
            b.Property(x => x.Category).IsRequired().HasMaxLength(20);
            b.Property(x => x.HeaderType).HasMaxLength(20);
            b.Property(x => x.HeaderContent).HasMaxLength(1000);
            b.Property(x => x.Body).IsRequired().HasMaxLength(4096);
            b.Property(x => x.Footer).HasMaxLength(60);
            b.Property(x => x.Status).IsRequired().HasMaxLength(20);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);
            b.HasIndex(x => new { x.ShopDomain, x.Name }).IsUnique();
            b.HasIndex(x => new { x.ShopDomain, x.Status });
        });

        modelBuilder.Entity<WhatsAppMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
            b.Property(x => x.ExternalMessageId).HasMaxLength(100);
            b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
            b.Property(x => x.Direction).IsRequired().HasMaxLength(10);
            b.Property(x => x.MessageType).IsRequired().HasMaxLength(20);
            b.Property(x => x.Content).HasMaxLength(4096);
            b.Property(x => x.MediaUrl).HasMaxLength(2000);
            b.Property(x => x.MediaMimeType).HasMaxLength(100);
            b.Property(x => x.MediaCaption).HasMaxLength(1024);
            b.Property(x => x.Status).IsRequired().HasMaxLength(20);
            b.Property(x => x.ErrorCode).HasMaxLength(50);
            b.Property(x => x.ErrorMessage).HasMaxLength(1000);
            b.HasIndex(x => x.ExternalMessageId);
            b.HasIndex(x => new { x.ShopDomain, x.PhoneNumber });
            b.HasIndex(x => new { x.ShopDomain, x.Status });
            b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
            b.HasOne(x => x.Template).WithMany(t => t.Messages).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WhatsAppConversation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
            b.Property(x => x.ExternalConversationId).HasMaxLength(100);
            b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
            b.Property(x => x.CustomerName).HasMaxLength(200);
            b.Property(x => x.Status).IsRequired().HasMaxLength(20);
            b.Property(x => x.AssignedTo).HasMaxLength(100);
            b.Property(x => x.LastMessagePreview).HasMaxLength(200);
            b.HasIndex(x => new { x.ShopDomain, x.PhoneNumber }).IsUnique();
            b.HasIndex(x => new { x.ShopDomain, x.Status });
            b.HasIndex(x => new { x.ShopDomain, x.LastMessageAt });
        });

        modelBuilder.Entity<WhatsAppCampaign>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopDomain).IsRequired().HasMaxLength(200);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Status).IsRequired().HasMaxLength(20);
            b.HasIndex(x => new { x.ShopDomain, x.Status });
            b.HasIndex(x => new { x.ShopDomain, x.CreatedAt });
            b.HasOne(x => x.Template).WithMany(t => t.Campaigns).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
