-- Migration: Add missing columns to InventoryPredictions table

-- Add ProductId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryPredictions') AND name = 'ProductId')
BEGIN
    ALTER TABLE InventoryPredictions ADD ProductId INT NULL;
    PRINT 'Added ProductId column to InventoryPredictions table';
END
ELSE
BEGIN
    PRINT 'ProductId column already exists';
END
GO

-- Add ProductVariantId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryPredictions') AND name = 'ProductVariantId')
BEGIN
    ALTER TABLE InventoryPredictions ADD ProductVariantId INT NULL;
    PRINT 'Added ProductVariantId column to InventoryPredictions table';
END
ELSE
BEGIN
    PRINT 'ProductVariantId column already exists';
END
GO

-- Add NinetyDayAverageSales column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryPredictions') AND name = 'NinetyDayAverageSales')
BEGIN
    ALTER TABLE InventoryPredictions ADD NinetyDayAverageSales DECIMAL(18,4) NULL;
    PRINT 'Added NinetyDayAverageSales column to InventoryPredictions table';
END
ELSE
BEGIN
    PRINT 'NinetyDayAverageSales column already exists';
END
GO

-- Add foreign key constraints if Products and ProductVariants tables exist
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Products')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryPredictions_Products')
    BEGIN
        ALTER TABLE InventoryPredictions
        ADD CONSTRAINT FK_InventoryPredictions_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id);
        PRINT 'Added foreign key FK_InventoryPredictions_Products';
    END
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductVariants')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryPredictions_ProductVariants')
    BEGIN
        ALTER TABLE InventoryPredictions
        ADD CONSTRAINT FK_InventoryPredictions_ProductVariants
        FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(Id);
        PRINT 'Added foreign key FK_InventoryPredictions_ProductVariants';
    END
END
GO

PRINT 'InventoryPredictions migration completed successfully!';
GO
