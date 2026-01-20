-- Migration: Create PortalThemeSettings table
-- Purpose: Store Customer Portal theme configuration per shop

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Create PortalThemeSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalThemeSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalThemeSettings](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ShopDomain] [nvarchar](255) NOT NULL,

        -- Branding
        [LogoUrl] [nvarchar](500) NULL,
        [FaviconUrl] [nvarchar](500) NULL,
        [StoreName] [nvarchar](255) NOT NULL DEFAULT 'My Store',

        -- Colors
        [PrimaryColor] [nvarchar](20) NOT NULL DEFAULT '#7c3aed',
        [PrimaryHoverColor] [nvarchar](20) NOT NULL DEFAULT '#6d28d9',
        [SecondaryColor] [nvarchar](20) NOT NULL DEFAULT '#ec4899',
        [AccentColor] [nvarchar](20) NOT NULL DEFAULT '#06b6d4',
        [BackgroundColor] [nvarchar](20) NOT NULL DEFAULT '#ffffff',
        [SurfaceColor] [nvarchar](20) NOT NULL DEFAULT '#f9fafb',
        [TextColor] [nvarchar](20) NOT NULL DEFAULT '#1f2937',
        [TextMutedColor] [nvarchar](20) NOT NULL DEFAULT '#6b7280',
        [BorderColor] [nvarchar](20) NOT NULL DEFAULT '#e5e7eb',
        [ErrorColor] [nvarchar](20) NOT NULL DEFAULT '#ef4444',
        [SuccessColor] [nvarchar](20) NOT NULL DEFAULT '#10b981',
        [WarningColor] [nvarchar](20) NOT NULL DEFAULT '#f59e0b',

        -- Dark mode colors
        [DarkBackgroundColor] [nvarchar](20) NOT NULL DEFAULT '#111827',
        [DarkSurfaceColor] [nvarchar](20) NOT NULL DEFAULT '#1f2937',
        [DarkTextColor] [nvarchar](20) NOT NULL DEFAULT '#f9fafb',
        [DarkTextMutedColor] [nvarchar](20) NOT NULL DEFAULT '#9ca3af',
        [DarkBorderColor] [nvarchar](20) NOT NULL DEFAULT '#374151',

        -- Typography
        [FontFamily] [nvarchar](100) NOT NULL DEFAULT 'Inter',
        [HeadingFontFamily] [nvarchar](100) NOT NULL DEFAULT 'Inter',
        [FontSizeBase] [nvarchar](20) NOT NULL DEFAULT '16px',

        -- Layout
        [ButtonStyle] [nvarchar](20) NOT NULL DEFAULT 'rounded',
        [ButtonSize] [nvarchar](20) NOT NULL DEFAULT 'medium',
        [CardStyle] [nvarchar](20) NOT NULL DEFAULT 'shadow',
        [CardRadius] [nvarchar](20) NOT NULL DEFAULT '0.75rem',
        [InputStyle] [nvarchar](20) NOT NULL DEFAULT 'bordered',

        -- Features
        [EnableDarkMode] [bit] NOT NULL DEFAULT 1,
        [EnableAnimations] [bit] NOT NULL DEFAULT 1,
        [ShowPoweredBy] [bit] NOT NULL DEFAULT 1,

        -- Custom code
        [CustomCss] [nvarchar](max) NULL,
        [CustomHeadHtml] [nvarchar](max) NULL,
        [CustomFooterHtml] [nvarchar](max) NULL,

        -- Timestamps
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NULL,

        CONSTRAINT [PK_PortalThemeSettings] PRIMARY KEY CLUSTERED ([Id] ASC)
    )

    PRINT 'Created table: PortalThemeSettings'
END
GO

-- Create unique index on ShopDomain (one theme config per shop)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PortalThemeSettings_ShopDomain' AND object_id = OBJECT_ID('dbo.PortalThemeSettings'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PortalThemeSettings_ShopDomain]
    ON [dbo].[PortalThemeSettings]([ShopDomain] ASC)

    PRINT 'Created index: IX_PortalThemeSettings_ShopDomain'
END
GO

PRINT 'Portal Theme Settings migration completed successfully.'
GO
