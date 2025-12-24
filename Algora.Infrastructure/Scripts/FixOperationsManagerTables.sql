-- Fix: Create missing Operations Manager tables with correct FK constraints
-- The ProductVariants FK must use ON DELETE NO ACTION to avoid cascade path conflicts

-- Drop existing tables if they exist (in case partially created)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierProducts')
BEGIN
    DROP TABLE SupplierProducts;
    PRINT 'Dropped existing SupplierProducts table';
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryLevels')
BEGIN
    DROP TABLE InventoryLevels;
    PRINT 'Dropped existing InventoryLevels table';
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductInventoryThresholds')
BEGIN
    DROP TABLE ProductInventoryThresholds;
    PRINT 'Dropped existing ProductInventoryThresholds table';
END
GO

-- SupplierProducts table (fixed FK constraints)
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
        REFERENCES ProductVariants(Id) ON DELETE NO ACTION
);
CREATE INDEX IX_SupplierProducts_SupplierId_ProductId ON SupplierProducts(SupplierId, ProductId, ProductVariantId);
PRINT 'Created SupplierProducts table';
GO

-- InventoryLevels table (fixed FK constraints)
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
        REFERENCES ProductVariants(Id) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IX_InventoryLevels_LocationId_ProductId_VariantId ON InventoryLevels(LocationId, ProductId, ProductVariantId);
CREATE INDEX IX_InventoryLevels_ShopDomain_InventoryItemId ON InventoryLevels(ShopDomain, ShopifyInventoryItemId);
PRINT 'Created InventoryLevels table';
GO

-- ProductInventoryThresholds table (fixed FK constraints)
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
        REFERENCES ProductVariants(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_ProductInventoryThresholds_Suppliers FOREIGN KEY (PreferredSupplierId)
        REFERENCES Suppliers(Id) ON DELETE SET NULL
);
CREATE UNIQUE INDEX IX_ProductInventoryThresholds_ShopDomain_ProductId_VariantId ON ProductInventoryThresholds(ShopDomain, ProductId, ProductVariantId);
PRINT 'Created ProductInventoryThresholds table';
GO

PRINT 'Operations Manager fix migration completed successfully!';
GO
