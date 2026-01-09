-- =============================================
-- WhatsApp Module Tables Migration Script
-- Facebook WhatsApp Business API Integration
-- =============================================

-- WhatsApp Templates
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppTemplates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WhatsAppTemplates] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(200) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [ExternalTemplateId] NVARCHAR(100) NULL,
        [Language] NVARCHAR(10) NOT NULL DEFAULT 'en',
        [Category] NVARCHAR(20) NOT NULL DEFAULT 'MARKETING',
        [HeaderType] NVARCHAR(20) NULL,
        [HeaderContent] NVARCHAR(1000) NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [Footer] NVARCHAR(60) NULL,
        [Buttons] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'pending',
        [RejectionReason] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [ApprovedAt] DATETIME2 NULL,
        CONSTRAINT [PK_WhatsAppTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [IX_WhatsAppTemplates_ShopDomain_Name]
        ON [dbo].[WhatsAppTemplates] ([ShopDomain], [Name]);
    CREATE INDEX [IX_WhatsAppTemplates_ShopDomain_Status]
        ON [dbo].[WhatsAppTemplates] ([ShopDomain], [Status]);

    PRINT 'Created table: WhatsAppTemplates';
END
GO

-- WhatsApp Conversations
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppConversations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WhatsAppConversations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(200) NOT NULL,
        [ExternalConversationId] NVARCHAR(100) NULL,
        [CustomerId] INT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [CustomerName] NVARCHAR(200) NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'open',
        [AssignedTo] NVARCHAR(100) NULL,
        [LastMessageAt] DATETIME2 NULL,
        [LastMessagePreview] NVARCHAR(200) NULL,
        [UnreadCount] INT NOT NULL DEFAULT 0,
        [IsBusinessInitiated] BIT NOT NULL DEFAULT 0,
        [WindowExpiresAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_WhatsAppConversations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [IX_WhatsAppConversations_ShopDomain_PhoneNumber]
        ON [dbo].[WhatsAppConversations] ([ShopDomain], [PhoneNumber]);
    CREATE INDEX [IX_WhatsAppConversations_ShopDomain_Status]
        ON [dbo].[WhatsAppConversations] ([ShopDomain], [Status]);
    CREATE INDEX [IX_WhatsAppConversations_ShopDomain_LastMessageAt]
        ON [dbo].[WhatsAppConversations] ([ShopDomain], [LastMessageAt]);

    PRINT 'Created table: WhatsAppConversations';
END
GO

-- WhatsApp Messages
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WhatsAppMessages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(200) NOT NULL,
        [ExternalMessageId] NVARCHAR(100) NULL,
        [CustomerId] INT NULL,
        [OrderId] INT NULL,
        [ConversationId] INT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [Direction] NVARCHAR(10) NOT NULL DEFAULT 'outbound',
        [MessageType] NVARCHAR(20) NOT NULL DEFAULT 'text',
        [TemplateId] INT NULL,
        [Content] NVARCHAR(MAX) NULL,
        [MediaUrl] NVARCHAR(2000) NULL,
        [MediaMimeType] NVARCHAR(100) NULL,
        [MediaCaption] NVARCHAR(1024) NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'pending',
        [ErrorCode] NVARCHAR(50) NULL,
        [ErrorMessage] NVARCHAR(1000) NULL,
        [SentAt] DATETIME2 NULL,
        [DeliveredAt] DATETIME2 NULL,
        [ReadAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_WhatsAppMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WhatsAppMessages_Templates] FOREIGN KEY ([TemplateId])
            REFERENCES [dbo].[WhatsAppTemplates]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WhatsAppMessages_Conversations] FOREIGN KEY ([ConversationId])
            REFERENCES [dbo].[WhatsAppConversations]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_WhatsAppMessages_ExternalMessageId]
        ON [dbo].[WhatsAppMessages] ([ExternalMessageId]);
    CREATE INDEX [IX_WhatsAppMessages_ShopDomain_PhoneNumber]
        ON [dbo].[WhatsAppMessages] ([ShopDomain], [PhoneNumber]);
    CREATE INDEX [IX_WhatsAppMessages_ShopDomain_Status]
        ON [dbo].[WhatsAppMessages] ([ShopDomain], [Status]);
    CREATE INDEX [IX_WhatsAppMessages_ShopDomain_CreatedAt]
        ON [dbo].[WhatsAppMessages] ([ShopDomain], [CreatedAt]);
    CREATE INDEX [IX_WhatsAppMessages_ConversationId]
        ON [dbo].[WhatsAppMessages] ([ConversationId]);
    CREATE INDEX [IX_WhatsAppMessages_TemplateId]
        ON [dbo].[WhatsAppMessages] ([TemplateId]);

    PRINT 'Created table: WhatsAppMessages';
END
GO

-- WhatsApp Campaigns
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppCampaigns]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WhatsAppCampaigns] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(200) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [TemplateId] INT NOT NULL,
        [SegmentId] INT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'draft',
        [ScheduledAt] DATETIME2 NULL,
        [SentAt] DATETIME2 NULL,
        [TotalRecipients] INT NOT NULL DEFAULT 0,
        [TotalSent] INT NOT NULL DEFAULT 0,
        [TotalDelivered] INT NOT NULL DEFAULT 0,
        [TotalRead] INT NOT NULL DEFAULT 0,
        [TotalFailed] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_WhatsAppCampaigns] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WhatsAppCampaigns_Templates] FOREIGN KEY ([TemplateId])
            REFERENCES [dbo].[WhatsAppTemplates]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_WhatsAppCampaigns_ShopDomain_Status]
        ON [dbo].[WhatsAppCampaigns] ([ShopDomain], [Status]);
    CREATE INDEX [IX_WhatsAppCampaigns_ShopDomain_CreatedAt]
        ON [dbo].[WhatsAppCampaigns] ([ShopDomain], [CreatedAt]);
    CREATE INDEX [IX_WhatsAppCampaigns_TemplateId]
        ON [dbo].[WhatsAppCampaigns] ([TemplateId]);

    PRINT 'Created table: WhatsAppCampaigns';
END
GO

PRINT 'WhatsApp module tables migration completed successfully.';
GO
