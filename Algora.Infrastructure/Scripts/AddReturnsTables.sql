-- Add Returns Tables
-- Run this script to add the Return Portal Manager feature tables to the database

-- =====================================================
-- ReturnSettings table (per-shop configuration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnSettings')
BEGIN
    CREATE TABLE [ReturnSettings] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT 1,
        [AllowSelfService] bit NOT NULL DEFAULT 1,
        [ReturnWindowDays] int NOT NULL DEFAULT 30,
        [RequireDeliveryConfirmation] bit NOT NULL DEFAULT 0,
        [LabelExpirationDays] int NOT NULL DEFAULT 14,
        [AutoApprovalEnabled] bit NOT NULL DEFAULT 1,
        [AutoApprovalMaxAmount] decimal(18,4) NOT NULL DEFAULT 500.00,
        [AutoApprovalRequireReason] bit NOT NULL DEFAULT 1,
        [ShippoApiKey] nvarchar(200) NULL,
        [StorePayShipping] bit NOT NULL DEFAULT 1,
        [DefaultCarrier] nvarchar(50) NULL DEFAULT 'usps',
        [DefaultServiceLevel] nvarchar(50) NULL DEFAULT 'usps_priority',
        [ReturnAddressName] nvarchar(100) NULL,
        [ReturnAddressCompany] nvarchar(200) NULL,
        [ReturnAddressStreet1] nvarchar(200) NULL,
        [ReturnAddressStreet2] nvarchar(200) NULL,
        [ReturnAddressCity] nvarchar(100) NULL,
        [ReturnAddressState] nvarchar(100) NULL,
        [ReturnAddressZip] nvarchar(20) NULL,
        [ReturnAddressCountry] nvarchar(10) NULL DEFAULT 'US',
        [ReturnAddressPhone] nvarchar(20) NULL,
        [ReturnAddressEmail] nvarchar(255) NULL,
        [EmailNotificationsEnabled] bit NOT NULL DEFAULT 1,
        [SmsNotificationsEnabled] bit NOT NULL DEFAULT 0,
        [NotificationEmail] nvarchar(255) NULL,
        [PageTitle] nvarchar(200) NULL,
        [PolicyText] nvarchar(2000) NULL,
        [LogoUrl] nvarchar(1000) NULL,
        [PrimaryColor] nvarchar(20) NULL DEFAULT '#4f46e5',
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReturnSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_ReturnSettings_ShopDomain] ON [ReturnSettings]([ShopDomain]);

    PRINT 'Created ReturnSettings table';
END
ELSE
BEGIN
    PRINT 'ReturnSettings table already exists';
END
GO

-- =====================================================
-- ReturnReasons table (configurable return reasons)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnReasons')
BEGIN
    CREATE TABLE [ReturnReasons] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [DisplayText] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [RequiresNote] bit NOT NULL DEFAULT 0,
        [IsDefect] bit NOT NULL DEFAULT 0,
        [EligibleForAutoApproval] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReturnReasons] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_ReturnReasons_ShopDomain_Code] ON [ReturnReasons]([ShopDomain], [Code]);
    CREATE INDEX [IX_ReturnReasons_ShopDomain_IsActive] ON [ReturnReasons]([ShopDomain], [IsActive]);

    PRINT 'Created ReturnReasons table';
END
ELSE
BEGIN
    PRINT 'ReturnReasons table already exists';
END
GO

-- =====================================================
-- ReturnLabels table (Shippo shipping labels)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnLabels')
BEGIN
    CREATE TABLE [ReturnLabels] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [ShippoTransactionId] nvarchar(100) NOT NULL,
        [ShippoRateId] nvarchar(100) NULL,
        [ShippoShipmentId] nvarchar(100) NULL,
        [TrackingNumber] nvarchar(100) NOT NULL,
        [TrackingUrl] nvarchar(1000) NULL,
        [Carrier] nvarchar(50) NULL,
        [ServiceLevel] nvarchar(50) NULL,
        [LabelUrl] nvarchar(1000) NOT NULL,
        [LabelFormat] nvarchar(10) NULL DEFAULT 'PDF',
        [Cost] decimal(18,4) NOT NULL DEFAULT 0,
        [Currency] nvarchar(10) NULL DEFAULT 'USD',
        [FromAddressJson] nvarchar(max) NULL,
        [ToAddressJson] nvarchar(max) NULL,
        [Status] nvarchar(20) NULL DEFAULT 'created',
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReturnLabels] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_ReturnLabels_ShopDomain_TrackingNumber] ON [ReturnLabels]([ShopDomain], [TrackingNumber]);
    CREATE INDEX [IX_ReturnLabels_ShippoTransactionId] ON [ReturnLabels]([ShippoTransactionId]);

    PRINT 'Created ReturnLabels table';
END
ELSE
BEGIN
    PRINT 'ReturnLabels table already exists';
END
GO

-- =====================================================
-- ReturnRequests table (main return request)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnRequests')
BEGIN
    CREATE TABLE [ReturnRequests] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [RequestNumber] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'pending',
        [OrderId] int NOT NULL,
        [PlatformOrderId] bigint NOT NULL,
        [OrderNumber] nvarchar(50) NULL,
        [CustomerId] int NULL,
        [CustomerEmail] nvarchar(255) NULL,
        [CustomerName] nvarchar(255) NULL,
        [ReturnReasonId] int NULL,
        [ReasonCode] nvarchar(50) NULL,
        [CustomerNote] nvarchar(2000) NULL,
        [IsAutoApproved] bit NOT NULL DEFAULT 0,
        [ApprovalNote] nvarchar(500) NULL,
        [RejectionReason] nvarchar(500) NULL,
        [ReturnLabelId] int NULL,
        [TrackingNumber] nvarchar(100) NULL,
        [TrackingUrl] nvarchar(1000) NULL,
        [TrackingCarrier] nvarchar(50) NULL,
        [RefundId] int NULL,
        [TotalRefundAmount] decimal(18,4) NOT NULL DEFAULT 0,
        [ShippingCost] decimal(18,4) NOT NULL DEFAULT 0,
        [Currency] nvarchar(10) NULL DEFAULT 'USD',
        [RequestedAt] datetime2 NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedAt] datetime2 NULL,
        [ShippedAt] datetime2 NULL,
        [ReceivedAt] datetime2 NULL,
        [RefundedAt] datetime2 NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnRequests_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnRequests_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReturnRequests_ReturnReasons] FOREIGN KEY ([ReturnReasonId]) REFERENCES [ReturnReasons]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReturnRequests_ReturnLabels] FOREIGN KEY ([ReturnLabelId]) REFERENCES [ReturnLabels]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReturnRequests_Refunds] FOREIGN KEY ([RefundId]) REFERENCES [Refunds]([Id]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX [IX_ReturnRequests_ShopDomain_RequestNumber] ON [ReturnRequests]([ShopDomain], [RequestNumber]);
    CREATE INDEX [IX_ReturnRequests_ShopDomain_Status] ON [ReturnRequests]([ShopDomain], [Status]);
    CREATE INDEX [IX_ReturnRequests_OrderId] ON [ReturnRequests]([OrderId]);
    CREATE INDEX [IX_ReturnRequests_CustomerId] ON [ReturnRequests]([CustomerId]);
    CREATE INDEX [IX_ReturnRequests_ReturnReasonId] ON [ReturnRequests]([ReturnReasonId]);
    CREATE INDEX [IX_ReturnRequests_ReturnLabelId] ON [ReturnRequests]([ReturnLabelId]);
    CREATE INDEX [IX_ReturnRequests_RequestedAt] ON [ReturnRequests]([RequestedAt]);

    PRINT 'Created ReturnRequests table';
END
ELSE
BEGIN
    PRINT 'ReturnRequests table already exists';
END
GO

-- =====================================================
-- ReturnItems table (items in return request)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnItems')
BEGIN
    CREATE TABLE [ReturnItems] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ReturnRequestId] int NOT NULL,
        [OrderLineId] int NULL,
        [PlatformProductId] bigint NULL,
        [PlatformVariantId] bigint NULL,
        [ProductTitle] nvarchar(500) NOT NULL,
        [VariantTitle] nvarchar(500) NULL,
        [Sku] nvarchar(100) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        [QuantityOrdered] int NOT NULL DEFAULT 0,
        [QuantityReturned] int NOT NULL DEFAULT 0,
        [UnitPrice] decimal(18,4) NOT NULL DEFAULT 0,
        [RefundAmount] decimal(18,4) NOT NULL DEFAULT 0,
        [ReasonCode] nvarchar(50) NULL,
        [CustomerNote] nvarchar(1000) NULL,
        [Restock] bit NOT NULL DEFAULT 1,
        [Restocked] bit NOT NULL DEFAULT 0,
        [Condition] nvarchar(50) NULL,
        [ConditionNote] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnItems_ReturnRequests] FOREIGN KEY ([ReturnRequestId]) REFERENCES [ReturnRequests]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReturnItems_OrderLines] FOREIGN KEY ([OrderLineId]) REFERENCES [OrderLines]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_ReturnItems_ReturnRequestId] ON [ReturnItems]([ReturnRequestId]);
    CREATE INDEX [IX_ReturnItems_OrderLineId] ON [ReturnItems]([OrderLineId]);
    CREATE INDEX [IX_ReturnItems_PlatformProductId] ON [ReturnItems]([PlatformProductId]);

    PRINT 'Created ReturnItems table';
END
ELSE
BEGIN
    PRINT 'ReturnItems table already exists';
END
GO

PRINT 'Returns tables migration complete';
