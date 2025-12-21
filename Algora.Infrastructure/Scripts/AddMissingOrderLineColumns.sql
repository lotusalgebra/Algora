-- Migration: Add missing columns to OrderLines table
-- These columns exist in the OrderLine entity but are missing from the database

-- Add PlatformProductId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderLines') AND name = 'PlatformProductId')
BEGIN
    ALTER TABLE OrderLines ADD PlatformProductId BIGINT NULL;
    PRINT 'Added PlatformProductId column to OrderLines table';
END
ELSE
BEGIN
    PRINT 'PlatformProductId column already exists';
END
GO

-- Add PlatformVariantId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderLines') AND name = 'PlatformVariantId')
BEGIN
    ALTER TABLE OrderLines ADD PlatformVariantId BIGINT NULL;
    PRINT 'Added PlatformVariantId column to OrderLines table';
END
ELSE
BEGIN
    PRINT 'PlatformVariantId column already exists';
END
GO

PRINT 'OrderLines migration completed successfully!';
GO
