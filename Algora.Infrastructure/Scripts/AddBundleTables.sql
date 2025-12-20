-- Add Bundle Tables
-- Run this script to add the Bundle Builder feature tables to the database

-- =====================================================
-- Bundles table (main bundle configuration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bundles')
BEGIN
    CREATE TABLE [Bundles] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [BundleType] nvarchar(20) NOT NULL DEFAULT 'fixed',
        [Status] nvarchar(20) NOT NULL DEFAULT 'draft',
        [DiscountType] nvarchar(20) NOT NULL DEFAULT 'percentage',
        [DiscountValue] decimal(18,4) NOT NULL DEFAULT 0,
        [DiscountCode] nvarchar(100) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [ThumbnailUrl] nvarchar(1000) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [ShopifyProductId] bigint NULL,
        [ShopifyVariantId] bigint NULL,
        [ShopifySyncStatus] nvarchar(20) NOT NULL DEFAULT 'pending',
        [ShopifySyncError] nvarchar(max) NULL,
        [ShopifySyncedAt] datetime2 NULL,
        [MinItems] int NULL,
        [MaxItems] int NULL,
        [OriginalPrice] decimal(18,4) NOT NULL DEFAULT 0,
        [BundlePrice] decimal(18,4) NOT NULL DEFAULT 0,
        [Currency] nvarchar(10) NOT NULL DEFAULT 'USD',
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Bundles] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_Bundles_ShopDomain] ON [Bundles]([ShopDomain]);
    CREATE INDEX [IX_Bundles_ShopDomain_Status] ON [Bundles]([ShopDomain], [Status]);
    CREATE INDEX [IX_Bundles_ShopDomain_IsActive] ON [Bundles]([ShopDomain], [IsActive]);
    CREATE UNIQUE INDEX [IX_Bundles_ShopDomain_Slug] ON [Bundles]([ShopDomain], [Slug]);
    CREATE INDEX [IX_Bundles_ShopifyProductId] ON [Bundles]([ShopifyProductId]);

    PRINT 'Created Bundles table';
END
ELSE
BEGIN
    PRINT 'Bundles table already exists';
END
GO

-- =====================================================
-- BundleItems table (products in fixed bundles)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BundleItems')
BEGIN
    CREATE TABLE [BundleItems] (
        [Id] int NOT NULL IDENTITY(1,1),
        [BundleId] int NOT NULL,
        [PlatformProductId] bigint NOT NULL,
        [PlatformVariantId] bigint NULL,
        [ProductTitle] nvarchar(500) NOT NULL,
        [VariantTitle] nvarchar(500) NULL,
        [Sku] nvarchar(100) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [Quantity] int NOT NULL DEFAULT 1,
        [UnitPrice] decimal(18,4) NOT NULL DEFAULT 0,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [CurrentInventory] int NOT NULL DEFAULT 0,
        [InventoryCheckedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BundleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BundleItems_Bundles] FOREIGN KEY ([BundleId]) REFERENCES [Bundles]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BundleItems_BundleId] ON [BundleItems]([BundleId]);
    CREATE INDEX [IX_BundleItems_PlatformProductId] ON [BundleItems]([PlatformProductId]);
    CREATE INDEX [IX_BundleItems_PlatformVariantId] ON [BundleItems]([PlatformVariantId]);

    PRINT 'Created BundleItems table';
END
ELSE
BEGIN
    PRINT 'BundleItems table already exists';
END
GO

-- =====================================================
-- BundleRules table (mix-and-match rules)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BundleRules')
BEGIN
    CREATE TABLE [BundleRules] (
        [Id] int NOT NULL IDENTITY(1,1),
        [BundleId] int NOT NULL,
        [Name] nvarchar(200) NULL,
        [EligibleProductIds] nvarchar(max) NULL,
        [EligibleCollectionIds] nvarchar(max) NULL,
        [EligibleTags] nvarchar(max) NULL,
        [MinQuantity] int NOT NULL DEFAULT 1,
        [MaxQuantity] int NULL,
        [AllowDuplicates] bit NOT NULL DEFAULT 1,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [DisplayLabel] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BundleRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BundleRules_Bundles] FOREIGN KEY ([BundleId]) REFERENCES [Bundles]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BundleRules_BundleId] ON [BundleRules]([BundleId]);

    PRINT 'Created BundleRules table';
END
ELSE
BEGIN
    PRINT 'BundleRules table already exists';
END
GO

-- =====================================================
-- BundleRuleTiers table (tiered discounts for mix-and-match)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BundleRuleTiers')
BEGIN
    CREATE TABLE [BundleRuleTiers] (
        [Id] int NOT NULL IDENTITY(1,1),
        [BundleRuleId] int NOT NULL,
        [MinQuantity] int NOT NULL,
        [MaxQuantity] int NULL,
        [DiscountType] nvarchar(20) NOT NULL DEFAULT 'percentage',
        [DiscountValue] decimal(18,4) NOT NULL DEFAULT 0,
        [DisplayLabel] nvarchar(200) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BundleRuleTiers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BundleRuleTiers_BundleRules] FOREIGN KEY ([BundleRuleId]) REFERENCES [BundleRules]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BundleRuleTiers_BundleRuleId] ON [BundleRuleTiers]([BundleRuleId]);

    PRINT 'Created BundleRuleTiers table';
END
ELSE
BEGIN
    PRINT 'BundleRuleTiers table already exists';
END
GO

-- =====================================================
-- BundleSettings table (per-shop configuration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BundleSettings')
BEGIN
    CREATE TABLE [BundleSettings] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT 1,
        [DefaultDiscountType] nvarchar(20) NOT NULL DEFAULT 'percentage',
        [DefaultDiscountValue] decimal(18,4) NOT NULL DEFAULT 10,
        [ShowInventoryWarnings] bit NOT NULL DEFAULT 1,
        [LowInventoryThreshold] int NOT NULL DEFAULT 5,
        [HideOutOfStock] bit NOT NULL DEFAULT 0,
        [BundlePageTitle] nvarchar(200) NULL,
        [BundlePageDescription] nvarchar(1000) NULL,
        [DisplayLayout] nvarchar(20) NOT NULL DEFAULT 'grid',
        [BundlesPerPage] int NOT NULL DEFAULT 12,
        [ShowOnStorefront] bit NOT NULL DEFAULT 1,
        [PrimaryColor] nvarchar(20) NULL,
        [SecondaryColor] nvarchar(20) NULL,
        [CustomCss] nvarchar(max) NULL,
        [AutoSyncToShopify] bit NOT NULL DEFAULT 0,
        [ShopifyProductType] nvarchar(100) NULL DEFAULT 'Bundle',
        [ShopifyProductTags] nvarchar(500) NULL DEFAULT 'bundle',
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BundleSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_BundleSettings_ShopDomain] ON [BundleSettings]([ShopDomain]);

    PRINT 'Created BundleSettings table';
END
ELSE
BEGIN
    PRINT 'BundleSettings table already exists';
END
GO

PRINT 'Bundle tables migration complete';
