-- Add Review Tables
-- Run this script to add the Review Importer & Syncer feature tables to the database

-- =====================================================
-- Reviews table (main review content)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reviews')
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [ProductId] int NULL,
        [PlatformProductId] bigint NULL,
        [ProductTitle] nvarchar(500) NULL,
        [ProductSku] nvarchar(100) NULL,
        [ReviewerName] nvarchar(200) NOT NULL,
        [ReviewerEmail] nvarchar(255) NULL,
        [Rating] int NOT NULL,
        [Title] nvarchar(500) NULL,
        [Body] nvarchar(max) NULL,
        [IsVerifiedPurchase] bit NOT NULL DEFAULT 0,
        [Source] nvarchar(20) NOT NULL DEFAULT 'manual',
        [SourceUrl] nvarchar(2000) NULL,
        [ExternalReviewId] nvarchar(100) NULL,
        [ImportJobId] int NULL,
        [OrderId] int NULL,
        [CustomerId] int NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'pending',
        [IsFeatured] bit NOT NULL DEFAULT 0,
        [ModerationNote] nvarchar(1000) NULL,
        [ApprovedAt] datetime2 NULL,
        [HelpfulVotes] int NOT NULL DEFAULT 0,
        [UnhelpfulVotes] int NOT NULL DEFAULT 0,
        [ReviewDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_Reviews_ShopDomain] ON [Reviews]([ShopDomain]);
    CREATE INDEX [IX_Reviews_ShopDomain_Status] ON [Reviews]([ShopDomain], [Status]);
    CREATE INDEX [IX_Reviews_ShopDomain_Source] ON [Reviews]([ShopDomain], [Source]);
    CREATE INDEX [IX_Reviews_ShopDomain_PlatformProductId] ON [Reviews]([ShopDomain], [PlatformProductId]);
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews]([ProductId]);
    CREATE INDEX [IX_Reviews_ImportJobId] ON [Reviews]([ImportJobId]);
    CREATE INDEX [IX_Reviews_OrderId] ON [Reviews]([OrderId]);
    CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews]([CustomerId]);

    PRINT 'Created Reviews table';
END
ELSE
BEGIN
    PRINT 'Reviews table already exists';
END
GO

-- =====================================================
-- ReviewMedia table (photos/videos attached to reviews)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReviewMedia')
BEGIN
    CREATE TABLE [ReviewMedia] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ReviewId] int NOT NULL,
        [MediaType] nvarchar(20) NOT NULL DEFAULT 'image',
        [Url] nvarchar(2000) NOT NULL,
        [ThumbnailUrl] nvarchar(2000) NULL,
        [AltText] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReviewMedia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReviewMedia_Reviews] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ReviewMedia_ReviewId] ON [ReviewMedia]([ReviewId]);

    PRINT 'Created ReviewMedia table';
END
ELSE
BEGIN
    PRINT 'ReviewMedia table already exists';
END
GO

-- =====================================================
-- ReviewImportJobs table (scraping job tracking)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReviewImportJobs')
BEGIN
    CREATE TABLE [ReviewImportJobs] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [SourceType] nvarchar(20) NOT NULL,
        [SourceUrl] nvarchar(2000) NOT NULL,
        [SourceProductId] nvarchar(100) NULL,
        [SourceProductTitle] nvarchar(500) NULL,
        [TargetProductId] int NULL,
        [TargetPlatformProductId] bigint NULL,
        [TargetProductTitle] nvarchar(500) NULL,
        [MappingMethod] nvarchar(20) NOT NULL DEFAULT 'manual',
        [Status] nvarchar(20) NOT NULL DEFAULT 'pending',
        [TotalReviews] int NOT NULL DEFAULT 0,
        [ImportedReviews] int NOT NULL DEFAULT 0,
        [SkippedReviews] int NOT NULL DEFAULT 0,
        [FailedReviews] int NOT NULL DEFAULT 0,
        [ErrorMessage] nvarchar(2000) NULL,
        [ProgressLog] nvarchar(max) NULL,
        [MinRating] int NULL,
        [IncludePhotosOnly] bit NOT NULL DEFAULT 0,
        [ReviewsAfterDate] datetime2 NULL,
        [MaxReviews] int NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReviewImportJobs] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_ReviewImportJobs_ShopDomain] ON [ReviewImportJobs]([ShopDomain]);
    CREATE INDEX [IX_ReviewImportJobs_ShopDomain_Status] ON [ReviewImportJobs]([ShopDomain], [Status]);
    CREATE INDEX [IX_ReviewImportJobs_ShopDomain_CreatedAt] ON [ReviewImportJobs]([ShopDomain], [CreatedAt]);
    CREATE INDEX [IX_ReviewImportJobs_TargetProductId] ON [ReviewImportJobs]([TargetProductId]);

    PRINT 'Created ReviewImportJobs table';
END
ELSE
BEGIN
    PRINT 'ReviewImportJobs table already exists';
END
GO

-- =====================================================
-- ReviewEmailAutomations table (email automation rules)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReviewEmailAutomations')
BEGIN
    CREATE TABLE [ReviewEmailAutomations] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [TriggerType] nvarchar(20) NOT NULL DEFAULT 'after_delivery',
        [DelayDays] int NOT NULL DEFAULT 7,
        [DelayHours] int NOT NULL DEFAULT 0,
        [MinOrderValue] decimal(18,2) NULL,
        [ProductIds] nvarchar(max) NULL,
        [ExcludedProductIds] nvarchar(max) NULL,
        [CustomerTags] nvarchar(max) NULL,
        [ExcludedCustomerTags] nvarchar(max) NULL,
        [ExcludeRepeatedCustomers] bit NOT NULL DEFAULT 1,
        [RepeatedCustomerExclusionDays] int NULL DEFAULT 30,
        [Subject] nvarchar(500) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [EmailTemplateId] int NULL,
        [TotalSent] int NOT NULL DEFAULT 0,
        [TotalOpened] int NOT NULL DEFAULT 0,
        [TotalClicked] int NOT NULL DEFAULT 0,
        [TotalReviewsCollected] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReviewEmailAutomations] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_ReviewEmailAutomations_ShopDomain] ON [ReviewEmailAutomations]([ShopDomain]);
    CREATE INDEX [IX_ReviewEmailAutomations_ShopDomain_IsActive] ON [ReviewEmailAutomations]([ShopDomain], [IsActive]);
    CREATE INDEX [IX_ReviewEmailAutomations_EmailTemplateId] ON [ReviewEmailAutomations]([EmailTemplateId]);

    PRINT 'Created ReviewEmailAutomations table';
END
ELSE
BEGIN
    PRINT 'ReviewEmailAutomations table already exists';
END
GO

-- =====================================================
-- ReviewEmailLogs table (track sent emails)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReviewEmailLogs')
BEGIN
    CREATE TABLE [ReviewEmailLogs] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [AutomationId] int NOT NULL,
        [OrderId] int NOT NULL,
        [CustomerId] int NULL,
        [CustomerEmail] nvarchar(255) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'pending',
        [ScheduledAt] datetime2 NULL,
        [SentAt] datetime2 NULL,
        [OpenedAt] datetime2 NULL,
        [ClickedAt] datetime2 NULL,
        [ReviewSubmittedAt] datetime2 NULL,
        [ReviewId] int NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [TrackingToken] nvarchar(64) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReviewEmailLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReviewEmailLogs_ReviewEmailAutomations] FOREIGN KEY ([AutomationId]) REFERENCES [ReviewEmailAutomations]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReviewEmailLogs_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ReviewEmailLogs_ShopDomain] ON [ReviewEmailLogs]([ShopDomain]);
    CREATE INDEX [IX_ReviewEmailLogs_ShopDomain_Status] ON [ReviewEmailLogs]([ShopDomain], [Status]);
    CREATE INDEX [IX_ReviewEmailLogs_ShopDomain_ScheduledAt] ON [ReviewEmailLogs]([ShopDomain], [ScheduledAt]);
    CREATE INDEX [IX_ReviewEmailLogs_AutomationId] ON [ReviewEmailLogs]([AutomationId]);
    CREATE INDEX [IX_ReviewEmailLogs_OrderId] ON [ReviewEmailLogs]([OrderId]);
    CREATE INDEX [IX_ReviewEmailLogs_CustomerId] ON [ReviewEmailLogs]([CustomerId]);
    CREATE UNIQUE INDEX [IX_ReviewEmailLogs_TrackingToken] ON [ReviewEmailLogs]([TrackingToken]) WHERE [TrackingToken] IS NOT NULL;

    PRINT 'Created ReviewEmailLogs table';
END
ELSE
BEGIN
    PRINT 'ReviewEmailLogs table already exists';
END
GO

-- =====================================================
-- ReviewSettings table (per-shop configuration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReviewSettings')
BEGIN
    CREATE TABLE [ReviewSettings] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [WidgetTheme] nvarchar(20) NOT NULL DEFAULT 'light',
        [PrimaryColor] nvarchar(20) NOT NULL DEFAULT '#000000',
        [AccentColor] nvarchar(20) NOT NULL DEFAULT '#f5a623',
        [StarColor] nvarchar(20) NOT NULL DEFAULT '#ffc107',
        [WidgetLayout] nvarchar(20) NOT NULL DEFAULT 'list',
        [ReviewsPerPage] int NOT NULL DEFAULT 10,
        [ShowReviewerName] bit NOT NULL DEFAULT 1,
        [ShowReviewDate] bit NOT NULL DEFAULT 1,
        [ShowVerifiedBadge] bit NOT NULL DEFAULT 1,
        [ShowPhotoGallery] bit NOT NULL DEFAULT 1,
        [AllowCustomerReviews] bit NOT NULL DEFAULT 1,
        [RequireApproval] bit NOT NULL DEFAULT 1,
        [AutoApproveReviews] bit NOT NULL DEFAULT 0,
        [AutoApproveMinRating] int NULL DEFAULT 4,
        [AutoApproveVerifiedOnly] bit NOT NULL DEFAULT 1,
        [TranslateImportedReviews] bit NOT NULL DEFAULT 0,
        [TranslateToLanguage] nvarchar(10) NULL,
        [RemoveSourceBranding] bit NOT NULL DEFAULT 1,
        [ImportPhotos] bit NOT NULL DEFAULT 1,
        [WidgetApiKey] nvarchar(64) NOT NULL,
        [DefaultEmailFromName] nvarchar(100) NULL,
        [DefaultEmailFromAddress] nvarchar(255) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReviewSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_ReviewSettings_ShopDomain] ON [ReviewSettings]([ShopDomain]);
    CREATE UNIQUE INDEX [IX_ReviewSettings_WidgetApiKey] ON [ReviewSettings]([WidgetApiKey]);

    PRINT 'Created ReviewSettings table';
END
ELSE
BEGIN
    PRINT 'ReviewSettings table already exists';
END
GO

-- =====================================================
-- Add foreign keys for Reviews table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Products')
BEGIN
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_Reviews_Products';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Orders')
BEGIN
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_Reviews_Orders';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Customers')
BEGIN
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_Reviews_Customers';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_ReviewImportJobs')
BEGIN
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_ReviewImportJobs] FOREIGN KEY ([ImportJobId]) REFERENCES [ReviewImportJobs]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_Reviews_ReviewImportJobs';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReviewImportJobs_Products')
BEGIN
    ALTER TABLE [ReviewImportJobs] ADD CONSTRAINT [FK_ReviewImportJobs_Products] FOREIGN KEY ([TargetProductId]) REFERENCES [Products]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_ReviewImportJobs_Products';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReviewEmailAutomations_EmailTemplates')
BEGIN
    ALTER TABLE [ReviewEmailAutomations] ADD CONSTRAINT [FK_ReviewEmailAutomations_EmailTemplates] FOREIGN KEY ([EmailTemplateId]) REFERENCES [EmailTemplates]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_ReviewEmailAutomations_EmailTemplates';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReviewEmailLogs_Customers')
BEGIN
    ALTER TABLE [ReviewEmailLogs] ADD CONSTRAINT [FK_ReviewEmailLogs_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_ReviewEmailLogs_Customers';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReviewEmailLogs_Reviews')
BEGIN
    ALTER TABLE [ReviewEmailLogs] ADD CONSTRAINT [FK_ReviewEmailLogs_Reviews] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews]([Id]) ON DELETE SET NULL;
    PRINT 'Added FK_ReviewEmailLogs_Reviews';
END
GO

PRINT 'Review tables migration completed successfully!';
GO
