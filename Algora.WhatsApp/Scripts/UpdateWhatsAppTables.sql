-- =============================================
-- WhatsApp Tables Update Script
-- Adds missing columns to existing tables
-- =============================================

-- Add missing columns to WhatsAppTemplates
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppTemplates]') AND name = 'ApprovedAt')
BEGIN
    ALTER TABLE [dbo].[WhatsAppTemplates] ADD [ApprovedAt] DATETIME2 NULL;
    PRINT 'Added column: WhatsAppTemplates.ApprovedAt';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppTemplates]') AND name = 'RejectionReason')
BEGIN
    ALTER TABLE [dbo].[WhatsAppTemplates] ADD [RejectionReason] NVARCHAR(1000) NULL;
    PRINT 'Added column: WhatsAppTemplates.RejectionReason';
END
GO

-- Add missing columns to WhatsAppConversations
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppConversations]') AND name = 'ExternalConversationId')
BEGIN
    ALTER TABLE [dbo].[WhatsAppConversations] ADD [ExternalConversationId] NVARCHAR(100) NULL;
    PRINT 'Added column: WhatsAppConversations.ExternalConversationId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppConversations]') AND name = 'WindowExpiresAt')
BEGIN
    ALTER TABLE [dbo].[WhatsAppConversations] ADD [WindowExpiresAt] DATETIME2 NULL;
    PRINT 'Added column: WhatsAppConversations.WindowExpiresAt';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WhatsAppConversations]') AND name = 'IsBusinessInitiated')
BEGIN
    ALTER TABLE [dbo].[WhatsAppConversations] ADD [IsBusinessInitiated] BIT NOT NULL DEFAULT 0;
    PRINT 'Added column: WhatsAppConversations.IsBusinessInitiated';
END
GO

-- Add missing columns to InventoryAlerts
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InventoryAlerts]') AND name = 'ThresholdQuantity')
BEGIN
    ALTER TABLE [dbo].[InventoryAlerts] ADD [ThresholdQuantity] INT NULL;
    PRINT 'Added column: InventoryAlerts.ThresholdQuantity';
END
GO

PRINT 'WhatsApp tables update completed successfully.';
GO
