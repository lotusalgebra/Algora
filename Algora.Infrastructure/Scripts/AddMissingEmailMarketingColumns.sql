-- Add missing Conditions column to EmailAutomationSteps table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailAutomationSteps]') AND name = 'Conditions')
BEGIN
    ALTER TABLE [dbo].[EmailAutomationSteps] ADD [Conditions] NVARCHAR(MAX) NULL;
END
GO

PRINT 'Email marketing schema updated successfully.';
