-- Migration: Add missing columns to Orders table
-- These columns exist in the Order entity but are missing from the database

-- Add OrderDate column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'OrderDate')
BEGIN
    ALTER TABLE Orders ADD OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT 'Added OrderDate column to Orders table';
END
ELSE
BEGIN
    PRINT 'OrderDate column already exists';
END
GO

-- Add CustomerEmail column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CustomerEmail')
BEGIN
    ALTER TABLE Orders ADD CustomerEmail NVARCHAR(500) NULL;
    PRINT 'Added CustomerEmail column to Orders table';
END
ELSE
BEGIN
    PRINT 'CustomerEmail column already exists';
END
GO

-- Add Notes column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'Notes')
BEGIN
    ALTER TABLE Orders ADD Notes NVARCHAR(MAX) NULL;
    PRINT 'Added Notes column to Orders table';
END
ELSE
BEGIN
    PRINT 'Notes column already exists';
END
GO

-- Update OrderDate from CreatedAt for existing orders (separate batch so column is visible)
UPDATE Orders SET OrderDate = CreatedAt WHERE OrderDate = GETUTCDATE();
PRINT 'Updated OrderDate from CreatedAt for existing orders';
GO

PRINT 'Migration completed successfully!';
GO
