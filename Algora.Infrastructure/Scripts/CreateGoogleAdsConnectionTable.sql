-- Create Google Ads Connection table for storing Google Ads integration settings
-- Run this script to add Google Ads support to the database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoogleAdsConnections')
BEGIN
    CREATE TABLE GoogleAdsConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        RefreshToken NVARCHAR(MAX) NULL,  -- Encrypted OAuth refresh token
        CustomerId NVARCHAR(50) NULL,      -- Google Ads Customer ID (without dashes)
        CustomerName NVARCHAR(255) NULL,   -- Display name of the account
        ManagerAccountId NVARCHAR(50) NULL, -- MCC (Manager) Account ID if applicable
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        IsConnected BIT NOT NULL DEFAULT 0,
        ConnectedAt DATETIME2 NULL,
        LastSyncedAt DATETIME2 NULL,
        LastSyncError NVARCHAR(MAX) NULL,
        AutoSyncEnabled BIT NOT NULL DEFAULT 1,
        SyncFrequencyHours INT NOT NULL DEFAULT 6,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_GoogleAdsConnections_ShopDomain UNIQUE (ShopDomain)
    );

    -- Index for quick lookups by shop domain
    CREATE INDEX IX_GoogleAdsConnections_ShopDomain
    ON GoogleAdsConnections(ShopDomain);

    -- Index for background sync service to find connections needing sync
    CREATE INDEX IX_GoogleAdsConnections_AutoSync
    ON GoogleAdsConnections(IsConnected, AutoSyncEnabled, LastSyncedAt)
    WHERE IsConnected = 1 AND AutoSyncEnabled = 1;

    PRINT 'GoogleAdsConnections table created successfully.';
END
ELSE
BEGIN
    PRINT 'GoogleAdsConnections table already exists.';
END
GO
