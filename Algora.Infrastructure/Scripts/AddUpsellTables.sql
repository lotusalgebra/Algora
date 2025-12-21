-- Add Upsell Tables
-- Run this script to add the Post-Purchase Upsell feature tables to the database

-- =====================================================
-- ProductAffinities table (co-purchase relationships)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductAffinities')
BEGIN
    CREATE TABLE [ProductAffinities] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [PlatformProductIdA] bigint NOT NULL,
        [ProductTitleA] nvarchar(500) NOT NULL,
        [PlatformProductIdB] bigint NOT NULL,
        [ProductTitleB] nvarchar(500) NOT NULL,
        [CoOccurrenceCount] int NOT NULL,
        [ProductAOrderCount] int NOT NULL,
        [ProductBOrderCount] int NOT NULL,
        [SupportScore] decimal(18,4) NOT NULL,
        [ConfidenceScore] decimal(18,4) NOT NULL,
        [LiftScore] decimal(18,4) NOT NULL,
        [TotalOrdersAnalyzed] int NOT NULL,
        [AnalysisStartDate] datetime2 NOT NULL,
        [AnalysisEndDate] datetime2 NOT NULL,
        [CalculatedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ProductAffinities] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_ProductAffinities_ShopDomain] ON [ProductAffinities]([ShopDomain]);
    CREATE INDEX [IX_ProductAffinities_ShopDomain_ProductA] ON [ProductAffinities]([ShopDomain], [PlatformProductIdA]);
    CREATE INDEX [IX_ProductAffinities_ShopDomain_ProductB] ON [ProductAffinities]([ShopDomain], [PlatformProductIdB]);
    CREATE INDEX [IX_ProductAffinities_ConfidenceScore] ON [ProductAffinities]([ConfidenceScore] DESC);

    PRINT 'Created ProductAffinities table';
END
ELSE
BEGIN
    PRINT 'ProductAffinities table already exists';
END
GO

-- =====================================================
-- UpsellSettings table (per-shop configuration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UpsellSettings')
BEGIN
    CREATE TABLE [UpsellSettings] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT 1,
        [ShowOnConfirmationPage] bit NOT NULL DEFAULT 1,
        [SendUpsellEmail] bit NOT NULL DEFAULT 0,
        [MaxOffersToShow] int NOT NULL DEFAULT 3,
        [DisplayLayout] nvarchar(20) NOT NULL DEFAULT 'carousel',
        [AffinityLookbackDays] int NOT NULL DEFAULT 90,
        [MinimumConfidenceScore] decimal(18,4) NOT NULL DEFAULT 0.1,
        [MinimumCoOccurrences] int NOT NULL DEFAULT 3,
        [PageTitle] nvarchar(200) NULL,
        [ThankYouMessage] nvarchar(500) NULL,
        [UpsellSectionTitle] nvarchar(200) NULL,
        [CustomCss] nvarchar(max) NULL,
        [LogoUrl] nvarchar(1000) NULL,
        [PrimaryColor] nvarchar(20) NULL,
        [SecondaryColor] nvarchar(20) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UpsellSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_UpsellSettings_ShopDomain] ON [UpsellSettings]([ShopDomain]);

    PRINT 'Created UpsellSettings table';
END
ELSE
BEGIN
    PRINT 'UpsellSettings table already exists';
END
GO

-- =====================================================
-- UpsellExperiments table (A/B testing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UpsellExperiments')
BEGIN
    CREATE TABLE [UpsellExperiments] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'draft',
        [PrimaryMetric] nvarchar(50) NOT NULL DEFAULT 'conversion_rate',
        [ControlTrafficPercent] int NOT NULL DEFAULT 50,
        [VariantATrafficPercent] int NOT NULL DEFAULT 50,
        [VariantBTrafficPercent] int NULL,
        [MinimumDetectableEffect] decimal(18,4) NOT NULL DEFAULT 0.05,
        [SignificanceLevel] decimal(18,4) NOT NULL DEFAULT 0.05,
        [StatisticalPower] decimal(18,4) NOT NULL DEFAULT 0.80,
        [CalculatedSampleSize] int NULL,
        [ControlImpressions] int NOT NULL DEFAULT 0,
        [ControlClicks] int NOT NULL DEFAULT 0,
        [ControlConversions] int NOT NULL DEFAULT 0,
        [ControlRevenue] decimal(18,4) NOT NULL DEFAULT 0,
        [VariantAImpressions] int NOT NULL DEFAULT 0,
        [VariantAClicks] int NOT NULL DEFAULT 0,
        [VariantAConversions] int NOT NULL DEFAULT 0,
        [VariantARevenue] decimal(18,4) NOT NULL DEFAULT 0,
        [VariantBImpressions] int NULL,
        [VariantBClicks] int NULL,
        [VariantBConversions] int NULL,
        [VariantBRevenue] decimal(18,4) NULL,
        [ControlConversionRate] decimal(18,6) NULL,
        [VariantAConversionRate] decimal(18,6) NULL,
        [VariantBConversionRate] decimal(18,6) NULL,
        [ControlConfidenceInterval] nvarchar(max) NULL,
        [VariantAConfidenceInterval] nvarchar(max) NULL,
        [VariantBConfidenceInterval] nvarchar(max) NULL,
        [PValueVsControl] decimal(18,6) NULL,
        [IsStatisticallySignificant] bit NOT NULL DEFAULT 0,
        [WinningVariant] nvarchar(20) NULL,
        [WinningLift] decimal(18,4) NULL,
        [AutoSelectWinner] bit NOT NULL DEFAULT 1,
        [WinnerSelectedAt] datetime2 NULL,
        [StartedAt] datetime2 NULL,
        [EndedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UpsellExperiments] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_UpsellExperiments_ShopDomain] ON [UpsellExperiments]([ShopDomain]);
    CREATE INDEX [IX_UpsellExperiments_ShopDomain_Status] ON [UpsellExperiments]([ShopDomain], [Status]);

    PRINT 'Created UpsellExperiments table';
END
ELSE
BEGIN
    PRINT 'UpsellExperiments table already exists';
END
GO

-- =====================================================
-- UpsellOffers table (configured upsell offers)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UpsellOffers')
BEGIN
    CREATE TABLE [UpsellOffers] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [TriggerProductIds] nvarchar(max) NULL,
        [RecommendedProductId] bigint NOT NULL,
        [RecommendedVariantId] bigint NULL,
        [RecommendedProductTitle] nvarchar(500) NOT NULL,
        [RecommendedProductImageUrl] nvarchar(1000) NULL,
        [RecommendedProductPrice] decimal(18,4) NOT NULL DEFAULT 0,
        [DiscountType] nvarchar(20) NULL,
        [DiscountValue] decimal(18,4) NULL,
        [DiscountCode] nvarchar(100) NULL,
        [Headline] nvarchar(200) NULL,
        [BodyText] nvarchar(1000) NULL,
        [ButtonText] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [RecommendationSource] nvarchar(20) NOT NULL DEFAULT 'manual',
        [ProductAffinityId] int NULL,
        [ExperimentId] int NULL,
        [ExperimentVariant] nvarchar(20) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UpsellOffers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UpsellOffers_ProductAffinities] FOREIGN KEY ([ProductAffinityId]) REFERENCES [ProductAffinities]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UpsellOffers_UpsellExperiments] FOREIGN KEY ([ExperimentId]) REFERENCES [UpsellExperiments]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_UpsellOffers_ShopDomain] ON [UpsellOffers]([ShopDomain]);
    CREATE INDEX [IX_UpsellOffers_ShopDomain_IsActive] ON [UpsellOffers]([ShopDomain], [IsActive]);
    CREATE INDEX [IX_UpsellOffers_ProductAffinityId] ON [UpsellOffers]([ProductAffinityId]);
    CREATE INDEX [IX_UpsellOffers_ExperimentId] ON [UpsellOffers]([ExperimentId]);

    PRINT 'Created UpsellOffers table';
END
ELSE
BEGIN
    PRINT 'UpsellOffers table already exists';
END
GO

-- =====================================================
-- UpsellConversions table (event tracking)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UpsellConversions')
BEGIN
    CREATE TABLE [UpsellConversions] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [SourceOrderId] int NULL,
        [PlatformSourceOrderId] bigint NOT NULL,
        [SessionId] nvarchar(64) NOT NULL,
        [CustomerId] int NULL,
        [UpsellOfferId] int NOT NULL,
        [ExperimentId] int NULL,
        [AssignedVariant] nvarchar(20) NULL,
        [ImpressionAt] datetime2 NOT NULL,
        [ClickedAt] datetime2 NULL,
        [ConvertedAt] datetime2 NULL,
        [ConversionOrderId] bigint NULL,
        [ConversionRevenue] decimal(18,4) NULL,
        [ConversionQuantity] int NULL,
        [GeneratedCartUrl] nvarchar(2000) NULL,
        [CartUrlUsed] bit NOT NULL DEFAULT 0,
        [AttributionWindowMinutes] int NOT NULL DEFAULT 30,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UpsellConversions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UpsellConversions_Orders] FOREIGN KEY ([SourceOrderId]) REFERENCES [Orders]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UpsellConversions_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UpsellConversions_UpsellOffers] FOREIGN KEY ([UpsellOfferId]) REFERENCES [UpsellOffers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UpsellConversions_UpsellExperiments] FOREIGN KEY ([ExperimentId]) REFERENCES [UpsellExperiments]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_UpsellConversions_ShopDomain] ON [UpsellConversions]([ShopDomain]);
    CREATE INDEX [IX_UpsellConversions_ShopDomain_ImpressionAt] ON [UpsellConversions]([ShopDomain], [ImpressionAt]);
    CREATE INDEX [IX_UpsellConversions_SessionId] ON [UpsellConversions]([SessionId]);
    CREATE INDEX [IX_UpsellConversions_UpsellOfferId] ON [UpsellConversions]([UpsellOfferId]);
    CREATE INDEX [IX_UpsellConversions_ExperimentId] ON [UpsellConversions]([ExperimentId]);
    CREATE INDEX [IX_UpsellConversions_SourceOrderId] ON [UpsellConversions]([SourceOrderId]);
    CREATE INDEX [IX_UpsellConversions_CustomerId] ON [UpsellConversions]([CustomerId]);

    PRINT 'Created UpsellConversions table';
END
ELSE
BEGIN
    PRINT 'UpsellConversions table already exists';
END
GO

PRINT 'Upsell tables migration complete';
