-- Add missing columns to EmailCampaigns table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaigns]') AND name = 'FromEmail')
BEGIN
    ALTER TABLE [dbo].[EmailCampaigns] ADD [FromEmail] NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaigns]') AND name = 'FromName')
BEGIN
    ALTER TABLE [dbo].[EmailCampaigns] ADD [FromName] NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaigns]') AND name = 'ReplyToEmail')
BEGIN
    ALTER TABLE [dbo].[EmailCampaigns] ADD [ReplyToEmail] NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailCampaigns]') AND name = 'TotalComplaints')
BEGIN
    ALTER TABLE [dbo].[EmailCampaigns] ADD [TotalComplaints] INT NOT NULL DEFAULT 0;
END
GO

-- Add missing column to CustomerSegments table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CustomerSegments]') AND name = 'LastCalculatedAt')
BEGIN
    ALTER TABLE [dbo].[CustomerSegments] ADD [LastCalculatedAt] DATETIME2 NULL;
END
GO

PRINT 'Missing email columns added successfully.';
