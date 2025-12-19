-- Add Inventory Prediction Tables
-- Run this script to add the Smart Inventory Predictor tables to the database

-- InventoryPredictions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryPredictions')
BEGIN
    CREATE TABLE [InventoryPredictions] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [PlatformProductId] bigint NOT NULL,
        [PlatformVariantId] bigint NULL,
        [ProductTitle] nvarchar(500) NOT NULL,
        [VariantTitle] nvarchar(500) NULL,
        [Sku] nvarchar(100) NULL,
        [CurrentQuantity] int NOT NULL,
        [AverageDailySales] decimal(18,4) NOT NULL,
        [SevenDayAverageSales] decimal(18,4) NULL,
        [ThirtyDayAverageSales] decimal(18,4) NULL,
        [DaysUntilStockout] int NOT NULL,
        [ProjectedStockoutDate] datetime2 NULL,
        [SuggestedReorderQuantity] int NOT NULL,
        [SuggestedReorderDate] datetime2 NULL,
        [ConfidenceLevel] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [SalesDataPointsCount] int NOT NULL DEFAULT 0,
        [OldestSaleDate] datetime2 NULL,
        [NewestSaleDate] datetime2 NULL,
        [CalculatedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InventoryPredictions] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_InventoryPredictions_ShopDomain] ON [InventoryPredictions]([ShopDomain]);
    CREATE INDEX [IX_InventoryPredictions_ShopDomain_Status] ON [InventoryPredictions]([ShopDomain], [Status]);
    CREATE UNIQUE INDEX [IX_InventoryPredictions_ShopDomain_ProductId_VariantId] ON [InventoryPredictions]([ShopDomain], [PlatformProductId], [PlatformVariantId]);

    PRINT 'Created InventoryPredictions table';
END
ELSE
BEGIN
    PRINT 'InventoryPredictions table already exists';
END
GO

-- InventoryAlerts table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryAlerts')
BEGIN
    CREATE TABLE [InventoryAlerts] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [InventoryPredictionId] int NOT NULL,
        [PlatformProductId] bigint NOT NULL,
        [PlatformVariantId] bigint NULL,
        [ProductTitle] nvarchar(500) NOT NULL,
        [VariantTitle] nvarchar(500) NULL,
        [Sku] nvarchar(100) NULL,
        [AlertType] nvarchar(50) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [CurrentQuantity] int NOT NULL,
        [DaysUntilStockout] int NULL,
        [EmailSent] bit NOT NULL DEFAULT 0,
        [EmailSentAt] datetime2 NULL,
        [SmsSent] bit NOT NULL DEFAULT 0,
        [SmsSentAt] datetime2 NULL,
        [WhatsAppSent] bit NOT NULL DEFAULT 0,
        [WhatsAppSentAt] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL,
        [AcknowledgedAt] datetime2 NULL,
        [AcknowledgedBy] nvarchar(255) NULL,
        [DismissedAt] datetime2 NULL,
        [DismissedBy] nvarchar(255) NULL,
        [DismissReason] nvarchar(500) NULL,
        [ResolvedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InventoryAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryAlerts_InventoryPredictions] FOREIGN KEY ([InventoryPredictionId]) REFERENCES [InventoryPredictions]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_InventoryAlerts_ShopDomain] ON [InventoryAlerts]([ShopDomain]);
    CREATE INDEX [IX_InventoryAlerts_ShopDomain_Status] ON [InventoryAlerts]([ShopDomain], [Status]);
    CREATE INDEX [IX_InventoryAlerts_ShopDomain_Severity] ON [InventoryAlerts]([ShopDomain], [Severity]);
    CREATE INDEX [IX_InventoryAlerts_InventoryPredictionId] ON [InventoryAlerts]([InventoryPredictionId]);

    PRINT 'Created InventoryAlerts table';
END
ELSE
BEGIN
    PRINT 'InventoryAlerts table already exists';
END
GO

-- InventoryAlertSettings table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryAlertSettings')
BEGIN
    CREATE TABLE [InventoryAlertSettings] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [AlertsEnabled] bit NOT NULL DEFAULT 1,
        [LowStockDaysThreshold] int NOT NULL DEFAULT 14,
        [CriticalStockDaysThreshold] int NOT NULL DEFAULT 7,
        [DefaultLeadTimeDays] int NOT NULL DEFAULT 7,
        [DefaultSafetyStockDays] int NOT NULL DEFAULT 3,
        [EmailNotificationsEnabled] bit NOT NULL DEFAULT 1,
        [NotificationEmail] nvarchar(255) NULL,
        [SmsNotificationsEnabled] bit NOT NULL DEFAULT 0,
        [NotificationPhone] nvarchar(50) NULL,
        [WhatsAppNotificationsEnabled] bit NOT NULL DEFAULT 0,
        [WhatsAppPhone] nvarchar(50) NULL,
        [MinHoursBetweenAlerts] int NOT NULL DEFAULT 24,
        [DailyDigestEnabled] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InventoryAlertSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_InventoryAlertSettings_ShopDomain] ON [InventoryAlertSettings]([ShopDomain]);

    PRINT 'Created InventoryAlertSettings table';
END
ELSE
BEGIN
    PRINT 'InventoryAlertSettings table already exists';
END
GO

PRINT 'Inventory tables migration complete';
