-- Create Pinterest Ads Connection table for storing Pinterest Ads integration settings
-- Run this script to add Pinterest Ads support to the database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PinterestAdsConnections')
BEGIN
    CREATE TABLE PinterestAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        AccessToken NVARCHAR(MAX) NULL,      -- Encrypted OAuth access token
        RefreshToken NVARCHAR(MAX) NULL,     -- Encrypted OAuth refresh token
        AdAccountId NVARCHAR(50) NULL,       -- Pinterest Ad Account ID
        AdAccountName NVARCHAR(255) NULL,    -- Display name of the ad account
        BusinessId NVARCHAR(50) NULL,        -- Business ID if applicable
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        Country NVARCHAR(10) NOT NULL DEFAULT 'US',
        IsConnected BIT NOT NULL DEFAULT 0,
        TokenExpiresAt DATETIME2 NULL,
        RefreshTokenExpiresAt DATETIME2 NULL,
        ConnectedAt DATETIME2 NULL,
        LastSyncedAt DATETIME2 NULL,
        LastSyncError NVARCHAR(MAX) NULL,
        AutoSyncEnabled BIT NOT NULL DEFAULT 1,
        SyncFrequencyHours INT NOT NULL DEFAULT 6,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_PinterestAdsConnections_ShopDomain UNIQUE (ShopDomain)
    );

    -- Index for quick lookups by shop domain
    CREATE INDEX IX_PinterestAdsConnections_ShopDomain
    ON PinterestAdsConnections(ShopDomain);

    -- Index for background sync service to find connections needing sync
    CREATE INDEX IX_PinterestAdsConnections_AutoSync
    ON PinterestAdsConnections(IsConnected, AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1 AND AutoSyncEnabled = 1;

    PRINT 'PinterestAdsConnections table created successfully.';
END
ELSE
BEGIN
    PRINT 'PinterestAdsConnections table already exists.';
END
GO
