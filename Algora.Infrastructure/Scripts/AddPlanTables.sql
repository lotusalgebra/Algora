-- Create Plans table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Plans' AND xtype='U')
BEGIN
    CREATE TABLE [Plans] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [MonthlyPrice] decimal(18,2) NOT NULL,
        [OrderLimit] int NOT NULL,
        [ProductLimit] int NOT NULL,
        [CustomerLimit] int NOT NULL,
        [HasWhatsApp] bit NOT NULL,
        [HasEmailCampaigns] bit NOT NULL,
        [HasSms] bit NOT NULL,
        [HasAdvancedReports] bit NOT NULL,
        [HasApiAccess] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [TrialDays] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_Plans_Name] ON [Plans] ([Name]);
END

-- Create PlanChangeRequests table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PlanChangeRequests' AND xtype='U')
BEGIN
    CREATE TABLE [PlanChangeRequests] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ShopDomain] nvarchar(200) NOT NULL,
        [CurrentPlanName] nvarchar(50) NOT NULL,
        [RequestedPlanName] nvarchar(50) NOT NULL,
        [RequestType] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'pending',
        [AdminNotes] nvarchar(max) NULL,
        [RequestedAt] datetime2 NOT NULL,
        [ProcessedAt] datetime2 NULL,
        [ProcessedBy] nvarchar(200) NULL,
        CONSTRAINT [PK_PlanChangeRequests] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_PlanChangeRequests_ShopDomain] ON [PlanChangeRequests] ([ShopDomain]);
    CREATE INDEX [IX_PlanChangeRequests_Status] ON [PlanChangeRequests] ([Status]);
END

PRINT 'Plan tables created successfully';
