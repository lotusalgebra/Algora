-- ============================================
-- Analytics Dashboard SQL Migration Script
-- Adds tables for analytics, ads spend tracking, and CLV
-- ============================================

-- Add CostOfGoodsSold column to Products table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'CostOfGoodsSold')
BEGIN
    ALTER TABLE Products ADD CostOfGoodsSold DECIMAL(18,4) NULL;
    PRINT 'Added CostOfGoodsSold column to Products table';
END
GO

-- Add CostOfGoodsSold column to ProductVariants table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'ProductVariants' AND COLUMN_NAME = 'CostOfGoodsSold')
BEGIN
    ALTER TABLE ProductVariants ADD CostOfGoodsSold DECIMAL(18,4) NULL;
    PRINT 'Added CostOfGoodsSold column to ProductVariants table';
END
GO

-- Create AdsSpends table for tracking advertising spend
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AdsSpends')
BEGIN
    CREATE TABLE AdsSpends (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        Platform NVARCHAR(50) NOT NULL,
        CampaignName NVARCHAR(500) NULL,
        CampaignId NVARCHAR(100) NULL,
        SpendDate DATETIME2 NOT NULL,
        Amount DECIMAL(18,4) NOT NULL,
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        Impressions INT NULL,
        Clicks INT NULL,
        Conversions INT NULL,
        Revenue DECIMAL(18,4) NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    
    CREATE INDEX IX_AdsSpends_ShopDomain_SpendDate ON AdsSpends(ShopDomain, SpendDate);
    CREATE INDEX IX_AdsSpends_ShopDomain_Platform ON AdsSpends(ShopDomain, Platform);
    
    PRINT 'Created AdsSpends table';
END
GO

-- Create AnalyticsSnapshots table for pre-computed metrics
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AnalyticsSnapshots')
BEGIN
    CREATE TABLE AnalyticsSnapshots (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        SnapshotDate DATETIME2 NOT NULL,
        PeriodType NVARCHAR(20) NOT NULL,
        TotalOrders INT NOT NULL DEFAULT 0,
        TotalRevenue DECIMAL(18,4) NOT NULL DEFAULT 0,
        TotalCOGS DECIMAL(18,4) NOT NULL DEFAULT 0,
        TotalAdsSpend DECIMAL(18,4) NOT NULL DEFAULT 0,
        GrossProfit DECIMAL(18,4) NOT NULL DEFAULT 0,
        NetProfit DECIMAL(18,4) NOT NULL DEFAULT 0,
        TotalRefunds DECIMAL(18,4) NOT NULL DEFAULT 0,
        NewCustomers INT NOT NULL DEFAULT 0,
        ReturningCustomers INT NOT NULL DEFAULT 0,
        TotalUnitsSold INT NOT NULL DEFAULT 0,
        AverageOrderValue DECIMAL(18,4) NOT NULL DEFAULT 0,
        ConversionRate DECIMAL(18,6) NULL,
        TopProductsJson NVARCHAR(MAX) NULL,
        TrafficSourcesJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE UNIQUE INDEX IX_AnalyticsSnapshots_ShopDomain_Date_Period 
        ON AnalyticsSnapshots(ShopDomain, SnapshotDate, PeriodType);
    
    PRINT 'Created AnalyticsSnapshots table';
END
GO

-- Create CustomerLifetimeValues table for CLV data
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CustomerLifetimeValues')
BEGIN
    CREATE TABLE CustomerLifetimeValues (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        CustomerId INT NOT NULL,
        TotalOrders INT NOT NULL DEFAULT 0,
        TotalSpent DECIMAL(18,4) NOT NULL DEFAULT 0,
        AverageOrderValue DECIMAL(18,4) NOT NULL DEFAULT 0,
        FirstOrderDate DATETIME2 NOT NULL,
        LastOrderDate DATETIME2 NOT NULL,
        DaysSinceLastOrder INT NOT NULL DEFAULT 0,
        AverageDaysBetweenOrders DECIMAL(18,4) NULL,
        PredictedLifetimeValue DECIMAL(18,4) NOT NULL DEFAULT 0,
        Segment NVARCHAR(20) NOT NULL DEFAULT 'low_value',
        AcquisitionSource NVARCHAR(50) NULL,
        TotalProfit DECIMAL(18,4) NULL,
        CalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_CustomerLifetimeValues_Customer 
            FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
    );
    
    CREATE UNIQUE INDEX IX_CustomerLifetimeValues_ShopDomain_Customer 
        ON CustomerLifetimeValues(ShopDomain, CustomerId);
    CREATE INDEX IX_CustomerLifetimeValues_ShopDomain_Segment 
        ON CustomerLifetimeValues(ShopDomain, Segment);
    
    PRINT 'Created CustomerLifetimeValues table';
END
GO

PRINT 'Analytics migration completed successfully!';
GO
