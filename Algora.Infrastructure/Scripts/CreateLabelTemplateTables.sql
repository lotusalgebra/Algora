-- Create LabelTemplates table for storing label designer templates
-- Run this script against the database to add the LabelTemplates table

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LabelTemplates')
BEGIN
    CREATE TABLE [dbo].[LabelTemplates] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(255) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [LabelType] NVARCHAR(50) NOT NULL DEFAULT 'Avery5163',
        [CustomWidthInches] FLOAT NULL,
        [CustomHeightInches] FLOAT NULL,
        [FieldsJson] NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_LabelTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Index for shop domain queries
    CREATE NONCLUSTERED INDEX [IX_LabelTemplates_ShopDomain]
    ON [dbo].[LabelTemplates] ([ShopDomain]);

    -- Index for finding default template
    CREATE NONCLUSTERED INDEX [IX_LabelTemplates_ShopDomain_IsDefault]
    ON [dbo].[LabelTemplates] ([ShopDomain], [IsDefault])
    WHERE [IsDefault] = 1;

    PRINT 'Created LabelTemplates table with indexes';
END
ELSE
BEGIN
    PRINT 'LabelTemplates table already exists';
END
GO
