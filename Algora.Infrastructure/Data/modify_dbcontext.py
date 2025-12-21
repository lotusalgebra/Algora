import re

# Read the file
with open('AppDbContext.cs', 'r', encoding='utf-8-sig') as f:
    content = f.read()

# Add analytics DbSets after tagging entities
dbsets_addition = '''
        // ----- Analytics entities -----
        public DbSet<AdsSpend> AdsSpends { get; set; } = null!;
        public DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; } = null!;
        public DbSet<CustomerLifetimeValue> CustomerLifetimeValues { get; set; } = null!;
'''

# Insert after EntityTags DbSet
pattern = r'(public DbSet<EntityTag> EntityTags \{ get; set; \} = null!;)'
replacement = r'\1\n' + dbsets_addition
content = re.sub(pattern, replacement, content)

# Add CostOfGoodsSold precision for Product
pattern = r'(modelBuilder\.Entity<Product>\(b =>\s*\{[^}]*b\.Property\(x => x\.CompareAtPrice\)\.HasPrecision\(18, 4\);)'
replacement = r'\1\n                b.Property(x => x.CostOfGoodsSold).HasPrecision(18, 4);'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Add CostOfGoodsSold precision for ProductVariant
pattern = r'(modelBuilder\.Entity<ProductVariant>\(b =>\s*\{[^}]*b\.Property\(x => x\.Weight\)\.HasPrecision\(18, 4\);)'
replacement = r'\1\n                b.Property(x => x.CostOfGoodsSold).HasPrecision(18, 4);'
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Add analytics entity configurations before the closing of OnModelCreating method
analytics_config = '''
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
'''

# Insert before the closing of OnModelCreating method
pattern = r'(modelBuilder\.Entity<AppConfiguration>\(b =>\s*\{[^}]*\}\);)'
replacement = r'\1\n' + analytics_config
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Write the modified content
with open('AppDbContext.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("File modified successfully!")
