-- Migration: Create Marketing Automation tables and columns

-- ==================== NEW TABLES ====================

-- ABTestVariants table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ABTestVariants')
BEGIN
    CREATE TABLE ABTestVariants (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AutomationId INT NOT NULL,
        StepId INT NULL,
        VariantName NVARCHAR(50) NOT NULL,
        Subject NVARCHAR(500) NULL,
        Body NVARCHAR(MAX) NULL,
        Weight INT NOT NULL DEFAULT 50,
        IsControl BIT NOT NULL DEFAULT 0,
        Impressions INT NOT NULL DEFAULT 0,
        Opens INT NOT NULL DEFAULT 0,
        Clicks INT NOT NULL DEFAULT 0,
        Conversions INT NOT NULL DEFAULT 0,
        Revenue DECIMAL(18,4) NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_ABTestVariants_EmailAutomations FOREIGN KEY (AutomationId)
            REFERENCES EmailAutomations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ABTestVariants_EmailAutomationSteps FOREIGN KEY (StepId)
            REFERENCES EmailAutomationSteps(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_ABTestVariants_AutomationId_StepId ON ABTestVariants(AutomationId, StepId);
    PRINT 'Created ABTestVariants table';
END
ELSE
BEGIN
    PRINT 'ABTestVariants table already exists';
END
GO

-- ABTestResults table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ABTestResults')
BEGIN
    CREATE TABLE ABTestResults (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EnrollmentId INT NOT NULL,
        VariantId INT NOT NULL,
        Opened BIT NOT NULL DEFAULT 0,
        Clicked BIT NOT NULL DEFAULT 0,
        Converted BIT NOT NULL DEFAULT 0,
        ConversionValue DECIMAL(18,4) NULL,
        AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        OpenedAt DATETIME2 NULL,
        ClickedAt DATETIME2 NULL,
        ConvertedAt DATETIME2 NULL,
        CONSTRAINT FK_ABTestResults_EmailAutomationEnrollments FOREIGN KEY (EnrollmentId)
            REFERENCES EmailAutomationEnrollments(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ABTestResults_ABTestVariants FOREIGN KEY (VariantId)
            REFERENCES ABTestVariants(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_ABTestResults_EnrollmentId_VariantId ON ABTestResults(EnrollmentId, VariantId);
    PRINT 'Created ABTestResults table';
END
ELSE
BEGIN
    PRINT 'ABTestResults table already exists';
END
GO

-- AutomationStepLogs table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AutomationStepLogs')
BEGIN
    CREATE TABLE AutomationStepLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EnrollmentId INT NOT NULL,
        StepId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'pending',
        Channel NVARCHAR(20) NULL,
        ExternalMessageId NVARCHAR(200) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        ScheduledAt DATETIME2 NULL,
        ExecutedAt DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        OpenedAt DATETIME2 NULL,
        ClickedAt DATETIME2 NULL,
        BouncedAt DATETIME2 NULL,
        UnsubscribedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_AutomationStepLogs_EmailAutomationEnrollments FOREIGN KEY (EnrollmentId)
            REFERENCES EmailAutomationEnrollments(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AutomationStepLogs_EmailAutomationSteps FOREIGN KEY (StepId)
            REFERENCES EmailAutomationSteps(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_AutomationStepLogs_EnrollmentId_StepId ON AutomationStepLogs(EnrollmentId, StepId);
    CREATE INDEX IX_AutomationStepLogs_Status_ScheduledAt ON AutomationStepLogs(Status, ScheduledAt);
    PRINT 'Created AutomationStepLogs table';
END
ELSE
BEGIN
    PRINT 'AutomationStepLogs table already exists';
END
GO

-- WinbackRules table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WinbackRules')
BEGIN
    CREATE TABLE WinbackRules (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        AutomationId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        DaysInactive INT NOT NULL DEFAULT 60,
        MinimumLifetimeValue DECIMAL(18,4) NULL,
        MinimumOrders INT NULL,
        MaximumOrders INT NULL,
        ExcludeRecentSubscribers BIT NOT NULL DEFAULT 0,
        ExcludeSubscribedWithinDays INT NULL,
        CustomerTags NVARCHAR(1000) NULL,
        ExcludeTags NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        LastRunAt DATETIME2 NULL,
        CustomersEnrolledLastRun INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_WinbackRules_EmailAutomations FOREIGN KEY (AutomationId)
            REFERENCES EmailAutomations(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_WinbackRules_ShopDomain_IsActive ON WinbackRules(ShopDomain, IsActive);
    PRINT 'Created WinbackRules table';
END
ELSE
BEGIN
    PRINT 'WinbackRules table already exists';
END
GO

-- ==================== ALTER EXISTING TABLES ====================

-- Add new columns to EmailAutomationEnrollments
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationEnrollments') AND name = 'AbandonedCheckoutId')
BEGIN
    ALTER TABLE EmailAutomationEnrollments ADD AbandonedCheckoutId BIGINT NULL;
    PRINT 'Added AbandonedCheckoutId column to EmailAutomationEnrollments';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationEnrollments') AND name = 'OrderId')
BEGIN
    ALTER TABLE EmailAutomationEnrollments ADD OrderId INT NULL;
    PRINT 'Added OrderId column to EmailAutomationEnrollments';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationEnrollments') AND name = 'ABTestVariantId')
BEGIN
    ALTER TABLE EmailAutomationEnrollments ADD ABTestVariantId INT NULL;
    PRINT 'Added ABTestVariantId column to EmailAutomationEnrollments';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationEnrollments') AND name = 'Metadata')
BEGIN
    ALTER TABLE EmailAutomationEnrollments ADD Metadata NVARCHAR(4000) NULL;
    PRINT 'Added Metadata column to EmailAutomationEnrollments';
END
GO

-- Add foreign key for OrderId if Orders table exists
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Orders')
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmailAutomationEnrollments_Orders')
BEGIN
    ALTER TABLE EmailAutomationEnrollments
    ADD CONSTRAINT FK_EmailAutomationEnrollments_Orders
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE SET NULL;
    PRINT 'Added FK_EmailAutomationEnrollments_Orders';
END
GO

-- Add foreign key for ABTestVariantId
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ABTestVariants')
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmailAutomationEnrollments_ABTestVariants')
BEGIN
    ALTER TABLE EmailAutomationEnrollments
    ADD CONSTRAINT FK_EmailAutomationEnrollments_ABTestVariants
    FOREIGN KEY (ABTestVariantId) REFERENCES ABTestVariants(Id) ON DELETE NO ACTION;
    PRINT 'Added FK_EmailAutomationEnrollments_ABTestVariants';
END
GO

-- Add indexes to EmailAutomationEnrollments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmailAutomationEnrollments_AutomationId_Email' AND object_id = OBJECT_ID('EmailAutomationEnrollments'))
BEGIN
    CREATE INDEX IX_EmailAutomationEnrollments_AutomationId_Email ON EmailAutomationEnrollments(AutomationId, Email);
    PRINT 'Added IX_EmailAutomationEnrollments_AutomationId_Email';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmailAutomationEnrollments_AutomationId_Status' AND object_id = OBJECT_ID('EmailAutomationEnrollments'))
BEGIN
    CREATE INDEX IX_EmailAutomationEnrollments_AutomationId_Status ON EmailAutomationEnrollments(AutomationId, Status);
    PRINT 'Added IX_EmailAutomationEnrollments_AutomationId_Status';
END
GO

-- Add new columns to EmailAutomationSteps
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationSteps') AND name = 'SmsTemplateId')
BEGIN
    ALTER TABLE EmailAutomationSteps ADD SmsTemplateId INT NULL;
    PRINT 'Added SmsTemplateId column to EmailAutomationSteps';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationSteps') AND name = 'SmsBody')
BEGIN
    ALTER TABLE EmailAutomationSteps ADD SmsBody NVARCHAR(500) NULL;
    PRINT 'Added SmsBody column to EmailAutomationSteps';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationSteps') AND name = 'WhatsAppTemplateId')
BEGIN
    ALTER TABLE EmailAutomationSteps ADD WhatsAppTemplateId NVARCHAR(100) NULL;
    PRINT 'Added WhatsAppTemplateId column to EmailAutomationSteps';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationSteps') AND name = 'WhatsAppBody')
BEGIN
    ALTER TABLE EmailAutomationSteps ADD WhatsAppBody NVARCHAR(1000) NULL;
    PRINT 'Added WhatsAppBody column to EmailAutomationSteps';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EmailAutomationSteps') AND name = 'IsABTestEnabled')
BEGIN
    ALTER TABLE EmailAutomationSteps ADD IsABTestEnabled BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsABTestEnabled column to EmailAutomationSteps';
END
GO

PRINT 'Marketing Automation migration completed successfully!';
GO
