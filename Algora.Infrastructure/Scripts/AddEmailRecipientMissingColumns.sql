-- =============================================
-- Add Missing EmailCampaignRecipients Columns
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'BounceReason')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [BounceReason] NVARCHAR(500) NULL;
    PRINT 'Added column: EmailCampaignRecipients.BounceReason';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'BounceType')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [BounceType] NVARCHAR(50) NULL;
    PRINT 'Added column: EmailCampaignRecipients.BounceType';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'OpenCount')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [OpenCount] INT NOT NULL DEFAULT 0;
    PRINT 'Added column: EmailCampaignRecipients.OpenCount';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'ClickCount')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [ClickCount] INT NOT NULL DEFAULT 0;
    PRINT 'Added column: EmailCampaignRecipients.ClickCount';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'ComplainedAt')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [ComplainedAt] DATETIME2 NULL;
    PRINT 'Added column: EmailCampaignRecipients.ComplainedAt';
END
GO

PRINT 'EmailCampaignRecipients columns migration completed.';
GO
