-- Migration: Create PortalFieldConfigurations table
-- Purpose: Store Customer Portal field configuration for forms (Registration, Profile, Checkout)

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Create PortalFieldConfigurations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalFieldConfigurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalFieldConfigurations](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ShopDomain] [nvarchar](200) NOT NULL,

        -- Field identification
        [PageType] [nvarchar](50) NOT NULL,           -- Registration, Profile, Checkout
        [FieldName] [nvarchar](100) NOT NULL,         -- Internal name (lowercase, underscores)
        [FieldType] [nvarchar](50) NOT NULL,          -- text, email, phone, number, date, select, checkbox, textarea, password

        -- Display
        [Label] [nvarchar](200) NOT NULL,
        [Placeholder] [nvarchar](500) NULL,
        [HelpText] [nvarchar](500) NULL,

        -- Behavior
        [IsRequired] [bit] NOT NULL DEFAULT 0,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [IsSystemField] [bit] NOT NULL DEFAULT 0,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,

        -- Validation
        [ValidationRegex] [nvarchar](500) NULL,
        [ValidationMessage] [nvarchar](500) NULL,
        [MinLength] [int] NULL,
        [MaxLength] [int] NULL,
        [MinValue] [decimal](18,2) NULL,
        [MaxValue] [decimal](18,2) NULL,

        -- Options & Styling
        [SelectOptions] [nvarchar](max) NULL,         -- JSON array for select fields
        [DefaultValue] [nvarchar](500) NULL,
        [CssClass] [nvarchar](200) NULL,
        [ColumnWidth] [int] NOT NULL DEFAULT 12,      -- 12-column grid (6 = half, 12 = full)

        -- Timestamps
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NULL,

        CONSTRAINT [PK_PortalFieldConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC)
    )

    PRINT 'Created table: PortalFieldConfigurations'
END
GO

-- Create unique index on ShopDomain + PageType + FieldName
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PortalFieldConfigurations_ShopDomain_PageType_FieldName' AND object_id = OBJECT_ID('dbo.PortalFieldConfigurations'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PortalFieldConfigurations_ShopDomain_PageType_FieldName]
    ON [dbo].[PortalFieldConfigurations]([ShopDomain] ASC, [PageType] ASC, [FieldName] ASC)

    PRINT 'Created index: IX_PortalFieldConfigurations_ShopDomain_PageType_FieldName'
END
GO

-- Create index for ordering queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PortalFieldConfigurations_ShopDomain_PageType_DisplayOrder' AND object_id = OBJECT_ID('dbo.PortalFieldConfigurations'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PortalFieldConfigurations_ShopDomain_PageType_DisplayOrder]
    ON [dbo].[PortalFieldConfigurations]([ShopDomain] ASC, [PageType] ASC, [DisplayOrder] ASC)

    PRINT 'Created index: IX_PortalFieldConfigurations_ShopDomain_PageType_DisplayOrder'
END
GO

PRINT 'Portal Field Configurations migration completed successfully.'
GO
