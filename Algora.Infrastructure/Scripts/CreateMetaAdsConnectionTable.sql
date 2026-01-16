-- Create MetaAdsConnections table for storing Meta (Facebook/Instagram) Ads integration settings
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MetaAdsConnections' AND xtype='U')
BEGIN
    CREATE TABLE MetaAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        AccessToken NVARCHAR(MAX) NULL,
        AdAccountId NVARCHAR(100) NULL,
        AdAccountName NVARCHAR(255) NULL,
        BusinessName NVARCHAR(255) NULL,
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        IsConnected BIT NOT NULL DEFAULT 0,
        ConnectedAt DATETIME2 NULL,
        LastSyncedAt DATETIME2 NULL,
        LastSyncError NVARCHAR(MAX) NULL,
        TokenExpiresAt DATETIME2 NULL,
        AutoSyncEnabled BIT NOT NULL DEFAULT 1,
        SyncFrequencyHours INT NOT NULL DEFAULT 6,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    -- Create unique index on ShopDomain (one connection per shop)
    CREATE UNIQUE INDEX IX_MetaAdsConnections_ShopDomain
    ON MetaAdsConnections(ShopDomain);

    -- Create index for sync queries
    CREATE INDEX IX_MetaAdsConnections_AutoSync
    ON MetaAdsConnections(AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1;

    PRINT 'MetaAdsConnections table created successfully';
END
ELSE
BEGIN
    PRINT 'MetaAdsConnections table already exists';
END

-- Add Source column to AdsSpends if it doesn't exist (to track manual vs API entries)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AdsSpends') AND name = 'Source')
BEGIN
    ALTER TABLE AdsSpends ADD Source NVARCHAR(50) NULL;
    PRINT 'Added Source column to AdsSpends table';
END

-- Create index on AdsSpends for Meta Ads queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdsSpends_Platform_ShopDomain')
BEGIN
    CREATE INDEX IX_AdsSpends_Platform_ShopDomain
    ON AdsSpends(Platform, ShopDomain, SpendDate);
    PRINT 'Created index IX_AdsSpends_Platform_ShopDomain';
END
GO
