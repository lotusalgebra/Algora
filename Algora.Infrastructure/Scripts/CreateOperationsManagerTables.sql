-- Migration: Create Operations Manager tables
-- Tables: Suppliers, SupplierProducts, PurchaseOrders, PurchaseOrderLines, Locations, InventoryLevels, ProductInventoryThresholds

-- ==================== NEW TABLES ====================

-- Suppliers table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL,
        Phone NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        ContactPerson NVARCHAR(100) NULL,
        Website NVARCHAR(500) NULL,
        DefaultLeadTimeDays INT NOT NULL DEFAULT 7,
        MinimumOrderAmount DECIMAL(18,4) NULL,
        PaymentTerms NVARCHAR(100) NULL,
        Notes NVARCHAR(2000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        TotalOrders INT NOT NULL DEFAULT 0,
        TotalSpent DECIMAL(18,4) NOT NULL DEFAULT 0,
        AverageDeliveryDays DECIMAL(10,2) NULL,
        OnTimeDeliveryRate DECIMAL(5,2) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE INDEX IX_Suppliers_ShopDomain_Name ON Suppliers(ShopDomain, Name);
    CREATE INDEX IX_Suppliers_ShopDomain_IsActive ON Suppliers(ShopDomain, IsActive);
    PRINT 'Created Suppliers table';
END
ELSE
BEGIN
    PRINT 'Suppliers table already exists';
END
GO

-- Locations table (must be created before PurchaseOrders due to FK)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Locations')
BEGIN
    CREATE TABLE Locations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        ShopifyLocationId BIGINT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Address1 NVARCHAR(300) NULL,
        Address2 NVARCHAR(300) NULL,
        City NVARCHAR(100) NULL,
        Province NVARCHAR(100) NULL,
        ProvinceCode NVARCHAR(10) NULL,
        Country NVARCHAR(100) NULL,
        CountryCode NVARCHAR(2) NULL,
        Zip NVARCHAR(20) NULL,
        Phone NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsPrimary BIT NOT NULL DEFAULT 0,
        FulfillsOnlineOrders BIT NOT NULL DEFAULT 1,
        LastSyncedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE UNIQUE INDEX IX_Locations_ShopDomain_ShopifyLocationId ON Locations(ShopDomain, ShopifyLocationId);
    CREATE INDEX IX_Locations_ShopDomain_IsActive ON Locations(ShopDomain, IsActive);
    PRINT 'Created Locations table';
END
ELSE
BEGIN
    PRINT 'Locations table already exists';
END
GO

-- SupplierProducts table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierProducts')
BEGIN
    CREATE TABLE SupplierProducts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SupplierId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductVariantId INT NULL,
        SupplierSku NVARCHAR(100) NULL,
        SupplierProductName NVARCHAR(200) NULL,
        UnitCost DECIMAL(18,4) NOT NULL,
        MinimumOrderQuantity INT NOT NULL DEFAULT 1,
        LeadTimeDays INT NULL,
        IsPreferred BIT NOT NULL DEFAULT 0,
        LastOrderedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SupplierProducts_Suppliers FOREIGN KEY (SupplierId)
            REFERENCES Suppliers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SupplierProducts_Products FOREIGN KEY (ProductId)
            REFERENCES Products(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SupplierProducts_ProductVariants FOREIGN KEY (ProductVariantId)
            REFERENCES ProductVariants(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_SupplierProducts_SupplierId_ProductId ON SupplierProducts(SupplierId, ProductId, ProductVariantId);
    PRINT 'Created SupplierProducts table';
END
ELSE
BEGIN
    PRINT 'SupplierProducts table already exists';
END
GO

-- PurchaseOrders table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE PurchaseOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        SupplierId INT NOT NULL,
        OrderNumber NVARCHAR(50) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'draft',
        LocationId INT NULL,
        Subtotal DECIMAL(18,4) NOT NULL DEFAULT 0,
        Tax DECIMAL(18,4) NOT NULL DEFAULT 0,
        Shipping DECIMAL(18,4) NOT NULL DEFAULT 0,
        Total DECIMAL(18,4) NOT NULL DEFAULT 0,
        Currency NVARCHAR(3) NOT NULL DEFAULT 'USD',
        Notes NVARCHAR(2000) NULL,
        SupplierReference NVARCHAR(100) NULL,
        TrackingNumber NVARCHAR(100) NULL,
        ExpectedDeliveryDate DATETIME2 NULL,
        OrderedAt DATETIME2 NULL,
        ConfirmedAt DATETIME2 NULL,
        ShippedAt DATETIME2 NULL,
        ReceivedAt DATETIME2 NULL,
        CancelledAt DATETIME2 NULL,
        CancellationReason NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_PurchaseOrders_Suppliers FOREIGN KEY (SupplierId)
            REFERENCES Suppliers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_PurchaseOrders_Locations FOREIGN KEY (LocationId)
            REFERENCES Locations(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX IX_PurchaseOrders_ShopDomain_OrderNumber ON PurchaseOrders(ShopDomain, OrderNumber);
    CREATE INDEX IX_PurchaseOrders_ShopDomain_Status ON PurchaseOrders(ShopDomain, Status);
    PRINT 'Created PurchaseOrders table';
END
ELSE
BEGIN
    PRINT 'PurchaseOrders table already exists';
END
GO

-- PurchaseOrderLines table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderLines')
BEGIN
    CREATE TABLE PurchaseOrderLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseOrderId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductVariantId INT NULL,
        Sku NVARCHAR(100) NULL,
        ProductTitle NVARCHAR(300) NOT NULL,
        VariantTitle NVARCHAR(200) NULL,
        QuantityOrdered INT NOT NULL,
        QuantityReceived INT NOT NULL DEFAULT 0,
        UnitCost DECIMAL(18,4) NOT NULL,
        TotalCost DECIMAL(18,4) NOT NULL,
        ReceivedAt DATETIME2 NULL,
        ReceivingNotes NVARCHAR(500) NULL,
        CONSTRAINT FK_PurchaseOrderLines_PurchaseOrders FOREIGN KEY (PurchaseOrderId)
            REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PurchaseOrderLines_Products FOREIGN KEY (ProductId)
            REFERENCES Products(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_PurchaseOrderLines_ProductVariants FOREIGN KEY (ProductVariantId)
            REFERENCES ProductVariants(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_PurchaseOrderLines_PurchaseOrderId ON PurchaseOrderLines(PurchaseOrderId);
    PRINT 'Created PurchaseOrderLines table';
END
ELSE
BEGIN
    PRINT 'PurchaseOrderLines table already exists';
END
GO

-- InventoryLevels table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryLevels')
BEGIN
    CREATE TABLE InventoryLevels (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        LocationId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductVariantId INT NULL,
        ShopifyInventoryItemId BIGINT NOT NULL,
        Available INT NOT NULL DEFAULT 0,
        Incoming INT NOT NULL DEFAULT 0,
        Committed INT NOT NULL DEFAULT 0,
        OnHand INT NULL,
        LastSyncedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_InventoryLevels_Locations FOREIGN KEY (LocationId)
            REFERENCES Locations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_InventoryLevels_Products FOREIGN KEY (ProductId)
            REFERENCES Products(Id) ON DELETE CASCADE,
        CONSTRAINT FK_InventoryLevels_ProductVariants FOREIGN KEY (ProductVariantId)
            REFERENCES ProductVariants(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX IX_InventoryLevels_LocationId_ProductId_VariantId ON InventoryLevels(LocationId, ProductId, ProductVariantId);
    CREATE INDEX IX_InventoryLevels_ShopDomain_InventoryItemId ON InventoryLevels(ShopDomain, ShopifyInventoryItemId);
    PRINT 'Created InventoryLevels table';
END
ELSE
BEGIN
    PRINT 'InventoryLevels table already exists';
END
GO

-- ProductInventoryThresholds table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductInventoryThresholds')
BEGIN
    CREATE TABLE ProductInventoryThresholds (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        ProductId INT NOT NULL,
        ProductVariantId INT NULL,
        LowStockThreshold INT NULL,
        CriticalStockThreshold INT NULL,
        ReorderPoint INT NULL,
        ReorderQuantity INT NULL,
        SafetyStockDays INT NULL,
        LeadTimeDays INT NULL,
        PreferredSupplierId INT NULL,
        AutoReorderEnabled BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_ProductInventoryThresholds_Products FOREIGN KEY (ProductId)
            REFERENCES Products(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ProductInventoryThresholds_ProductVariants FOREIGN KEY (ProductVariantId)
            REFERENCES ProductVariants(Id) ON DELETE SET NULL,
        CONSTRAINT FK_ProductInventoryThresholds_Suppliers FOREIGN KEY (PreferredSupplierId)
            REFERENCES Suppliers(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX IX_ProductInventoryThresholds_ShopDomain_ProductId_VariantId ON ProductInventoryThresholds(ShopDomain, ProductId, ProductVariantId);
    PRINT 'Created ProductInventoryThresholds table';
END
ELSE
BEGIN
    PRINT 'ProductInventoryThresholds table already exists';
END
GO

PRINT 'Operations Manager migration completed successfully!';
GO
