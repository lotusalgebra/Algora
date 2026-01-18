-- Create Amazon Ads Connection table for storing Amazon Advertising integration settings
-- Run this script to add Amazon Ads support to the database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AmazonAdsConnections')
BEGIN
    CREATE TABLE AmazonAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        AccessToken NVARCHAR(MAX) NULL,      -- Encrypted OAuth access token
        RefreshToken NVARCHAR(MAX) NULL,     -- Encrypted OAuth refresh token
        ProfileId NVARCHAR(100) NULL,        -- Amazon Advertising Profile ID
        ProfileName NVARCHAR(255) NULL,      -- Display name of the profile/account
        MarketplaceId NVARCHAR(50) NULL,     -- Amazon Marketplace ID
        CountryCode NVARCHAR(10) NULL,       -- Country code (US, CA, UK, etc.)
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        IsConnected BIT NOT NULL DEFAULT 0,
        TokenExpiresAt DATETIME2 NULL,
        ConnectedAt DATETIME2 NULL,
        LastSyncedAt DATETIME2 NULL,
        LastSyncError NVARCHAR(MAX) NULL,
        AutoSyncEnabled BIT NOT NULL DEFAULT 1,
        SyncFrequencyHours INT NOT NULL DEFAULT 6,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_AmazonAdsConnections_ShopDomain UNIQUE (ShopDomain)
    );

    -- Index for quick lookups by shop domain
    CREATE INDEX IX_AmazonAdsConnections_ShopDomain
    ON AmazonAdsConnections(ShopDomain);

    -- Index for background sync service to find connections needing sync
    CREATE INDEX IX_AmazonAdsConnections_AutoSync
    ON AmazonAdsConnections(IsConnected, AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1 AND AutoSyncEnabled = 1;

    PRINT 'AmazonAdsConnections table created successfully.';
END
ELSE
BEGIN
    PRINT 'AmazonAdsConnections table already exists.';
END
GO
