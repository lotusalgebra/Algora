-- Create Snapchat Ads Connection table for storing Snapchat Ads integration settings
-- Run this script to add Snapchat Ads support to the database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SnapchatAdsConnections')
BEGIN
    CREATE TABLE SnapchatAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        AccessToken NVARCHAR(MAX) NULL,      -- Encrypted OAuth access token
        RefreshToken NVARCHAR(MAX) NULL,     -- Encrypted OAuth refresh token
        AdAccountId NVARCHAR(50) NULL,       -- Snapchat Ad Account ID
        AdAccountName NVARCHAR(255) NULL,    -- Display name of the ad account
        OrganizationId NVARCHAR(50) NULL,    -- Organization ID if applicable
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        Timezone NVARCHAR(50) NOT NULL DEFAULT 'America/Los_Angeles',
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

        CONSTRAINT UQ_SnapchatAdsConnections_ShopDomain UNIQUE (ShopDomain)
    );

    -- Index for quick lookups by shop domain
    CREATE INDEX IX_SnapchatAdsConnections_ShopDomain
    ON SnapchatAdsConnections(ShopDomain);

    -- Index for background sync service to find connections needing sync
    CREATE INDEX IX_SnapchatAdsConnections_AutoSync
    ON SnapchatAdsConnections(IsConnected, AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1 AND AutoSyncEnabled = 1;

    PRINT 'SnapchatAdsConnections table created successfully.';
END
ELSE
BEGIN
    PRINT 'SnapchatAdsConnections table already exists.';
END
GO
