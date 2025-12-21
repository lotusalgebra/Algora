-- Migration: Add missing columns to Refunds table

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'Currency')
BEGIN
    ALTER TABLE Refunds ADD Currency NVARCHAR(10) NOT NULL DEFAULT 'USD';
    PRINT 'Added Currency column';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'Reason')
BEGIN
    ALTER TABLE Refunds ADD Reason NVARCHAR(500) NULL;
    PRINT 'Added Reason column';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'Restock')
BEGIN
    ALTER TABLE Refunds ADD Restock BIT NOT NULL DEFAULT 0;
    PRINT 'Added Restock column';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'RefundedAt')
BEGIN
    ALTER TABLE Refunds ADD RefundedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT 'Added RefundedAt column';
END
GO

PRINT 'Refunds migration complete';
GO
