-- Add missing NextStepAt column to EmailAutomationEnrollments table
-- This column tracks when the next automation step should be processed

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'EmailAutomationEnrollments' AND COLUMN_NAME = 'NextStepAt'
)
BEGIN
    ALTER TABLE EmailAutomationEnrollments ADD NextStepAt DATETIME2 NULL;
    PRINT 'Added NextStepAt column to EmailAutomationEnrollments';
END
ELSE
BEGIN
    PRINT 'NextStepAt column already exists';
END
GO

-- Add index for better query performance on automation processing
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmailAutomationEnrollments_StatusNextStep'
    AND object_id = OBJECT_ID('EmailAutomationEnrollments')
)
BEGIN
    CREATE INDEX IX_EmailAutomationEnrollments_StatusNextStep
    ON EmailAutomationEnrollments(Status, NextStepAt);
    PRINT 'Created index IX_EmailAutomationEnrollments_StatusNextStep';
END
ELSE
BEGIN
    PRINT 'Index already exists';
END
GO
