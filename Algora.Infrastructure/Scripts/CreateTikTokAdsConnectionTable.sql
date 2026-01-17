-- Create TikTok Ads Connection table for storing TikTok Ads integration settings
-- Run this script to add TikTok Ads support to the database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TikTokAdsConnections')
BEGIN
    CREATE TABLE TikTokAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        AccessToken NVARCHAR(MAX) NULL,      -- Encrypted OAuth access token
        RefreshToken NVARCHAR(MAX) NULL,     -- Encrypted OAuth refresh token
        AdvertiserId NVARCHAR(50) NULL,      -- TikTok Advertiser ID
        AdvertiserName NVARCHAR(255) NULL,   -- Display name of the advertiser
        BusinessCenterId NVARCHAR(50) NULL,  -- Business Center ID if applicable
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        Timezone NVARCHAR(50) NOT NULL DEFAULT 'UTC',
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

        CONSTRAINT UQ_TikTokAdsConnections_ShopDomain UNIQUE (ShopDomain)
    );

    -- Index for quick lookups by shop domain
    CREATE INDEX IX_TikTokAdsConnections_ShopDomain
    ON TikTokAdsConnections(ShopDomain);

    -- Index for background sync service to find connections needing sync
    CREATE INDEX IX_TikTokAdsConnections_AutoSync
    ON TikTokAdsConnections(IsConnected, AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1 AND AutoSyncEnabled = 1;

    PRINT 'TikTokAdsConnections table created successfully.';
END
ELSE
BEGIN
    PRINT 'TikTokAdsConnections table already exists.';
END
GO
