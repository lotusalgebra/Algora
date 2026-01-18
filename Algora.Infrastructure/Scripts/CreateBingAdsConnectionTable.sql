-- Migration script to create BingAdsConnections table for Microsoft Advertising integration
-- Run this script against the Algora database

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create BingAdsConnections table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BingAdsConnections]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BingAdsConnections] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ShopDomain] NVARCHAR(255) NOT NULL,
        [AccessToken] NVARCHAR(MAX) NULL,
        [RefreshToken] NVARCHAR(MAX) NULL,
        [AccountId] NVARCHAR(100) NULL,
        [AccountName] NVARCHAR(255) NULL,
        [CustomerId] NVARCHAR(100) NULL,
        [Currency] NVARCHAR(10) NOT NULL DEFAULT 'USD',
        [IsConnected] BIT NOT NULL DEFAULT 0,
        [TokenExpiresAt] DATETIME2 NULL,
        [ConnectedAt] DATETIME2 NULL,
        [LastSyncedAt] DATETIME2 NULL,
        [LastSyncError] NVARCHAR(MAX) NULL,
        [AutoSyncEnabled] BIT NOT NULL DEFAULT 1,
        [SyncFrequencyHours] INT NOT NULL DEFAULT 6,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_BingAdsConnections] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'Created BingAdsConnections table';
END
ELSE
BEGIN
    PRINT 'BingAdsConnections table already exists';
END
GO

-- Create unique index on ShopDomain (one connection per shop)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BingAdsConnections_ShopDomain' AND object_id = OBJECT_ID('BingAdsConnections'))
BEGIN
    CREATE UNIQUE INDEX [IX_BingAdsConnections_ShopDomain]
    ON [dbo].[BingAdsConnections] ([ShopDomain]);

    PRINT 'Created unique index on ShopDomain';
END
GO

-- Create index for sync queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BingAdsConnections_IsConnected_AutoSyncEnabled' AND object_id = OBJECT_ID('BingAdsConnections'))
BEGIN
    CREATE INDEX [IX_BingAdsConnections_IsConnected_AutoSyncEnabled]
    ON [dbo].[BingAdsConnections] ([IsConnected], [AutoSyncEnabled])
    INCLUDE ([ShopDomain], [LastSyncedAt], [SyncFrequencyHours]);

    PRINT 'Created index for sync queries';
END
GO

PRINT 'BingAdsConnections migration completed successfully';
GO
