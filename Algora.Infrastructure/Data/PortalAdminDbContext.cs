using Microsoft.EntityFrameworkCore;

namespace Algora.Infrastructure.Data;

/// <summary>
/// DbContext for accessing the Customer Portal database from the admin application.
/// This provides read/write access to portal return requests for admin management.
/// </summary>
public class PortalAdminDbContext : DbContext
{
    public PortalAdminDbContext(DbContextOptions<PortalAdminDbContext> options) : base(options)
    {
    }

    public DbSet<PortalReturnRequest> ReturnRequests => Set<PortalReturnRequest>();
    public DbSet<PortalReturnRequestItem> ReturnRequestItems => Set<PortalReturnRequestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ReturnRequest
        modelBuilder.Entity<PortalReturnRequest>(entity =>
        {
            entity.ToTable("ReturnRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShopDomain).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CustomerEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OrderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RequestType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AdditionalComments).HasMaxLength(2000);
            entity.Property(e => e.PreferredResolution).HasMaxLength(50);
            entity.Property(e => e.AdminNotes).HasMaxLength(2000);
            entity.Property(e => e.ReturnLabelUrl).HasMaxLength(500);
            entity.Property(e => e.ReturnTrackingNumber).HasMaxLength(100);
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.ShopDomain, e.CustomerEmail });
            entity.HasIndex(e => new { e.ShopDomain, e.OrderId });
            entity.HasIndex(e => new { e.ShopDomain, e.Status });
        });

        // ReturnRequestItem
        modelBuilder.Entity<PortalReturnRequestItem>(entity =>
        {
            entity.ToTable("ReturnRequestItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LineItemId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.VariantId).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.VariantTitle).HasMaxLength(255);
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ItemReason).HasMaxLength(500);
            entity.Property(e => e.Condition).HasMaxLength(50);

            entity.HasOne(e => e.ReturnRequest)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>
/// Portal return request entity (mirrors Shopify.CustomerPortal.Domain)
/// </summary>
public class PortalReturnRequest
{
    public int Id { get; set; }
    public string ShopDomain { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = "Return";
    public string Status { get; set; } = "Pending";
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalComments { get; set; }
    public string PreferredResolution { get; set; } = "Refund";
    public string? AdminNotes { get; set; }
    public string? ReturnLabelUrl { get; set; }
    public string? ReturnTrackingNumber { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<PortalReturnRequestItem> Items { get; set; } = new();
}

/// <summary>
/// Portal return request item entity (mirrors Shopify.CustomerPortal.Domain)
/// </summary>
public class PortalReturnRequestItem
{
    public int Id { get; set; }
    public int ReturnRequestId { get; set; }
    public string LineItemId { get; set; } = string.Empty;
    public string? VariantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? VariantTitle { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ItemReason { get; set; }
    public string Condition { get; set; } = "Unopened";
    public PortalReturnRequest? ReturnRequest { get; set; }
}
