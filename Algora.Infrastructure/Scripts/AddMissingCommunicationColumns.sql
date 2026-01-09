-- =============================================
-- Add Missing Communication Columns
-- =============================================

-- Add CreatedAt to EmailCampaignRecipients
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaignRecipients]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[EmailCampaignRecipients] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT 'Added column: EmailCampaignRecipients.CreatedAt';
END
GO

PRINT 'Communication columns migration completed.';
GO
