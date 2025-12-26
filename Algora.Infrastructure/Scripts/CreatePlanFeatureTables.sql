-- Create PlanFeatures table
-- Stores feature definitions that can be assigned to plans
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlanFeatures')
BEGIN
    CREATE TABLE [dbo].[PlanFeatures] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [IconClass] NVARCHAR(100) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PlanFeatures] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Unique index on Code
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PlanFeatures_Code]
    ON [dbo].[PlanFeatures] ([Code] ASC);

    -- Index on Category for grouped queries
    CREATE NONCLUSTERED INDEX [IX_PlanFeatures_Category]
    ON [dbo].[PlanFeatures] ([Category] ASC, [SortOrder] ASC);

    PRINT 'Created PlanFeatures table';
END
GO

-- Create PlanFeatureAssignments table
-- Junction table mapping features to plans
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlanFeatureAssignments')
BEGIN
    CREATE TABLE [dbo].[PlanFeatureAssignments] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PlanId] INT NOT NULL,
        [PlanFeatureId] INT NOT NULL,
        [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [AssignedBy] NVARCHAR(256) NULL,
        CONSTRAINT [PK_PlanFeatureAssignments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PlanFeatureAssignments_Plans] FOREIGN KEY ([PlanId])
            REFERENCES [dbo].[Plans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlanFeatureAssignments_PlanFeatures] FOREIGN KEY ([PlanFeatureId])
            REFERENCES [dbo].[PlanFeatures] ([Id]) ON DELETE CASCADE
    );

    -- Unique constraint to prevent duplicate assignments
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PlanFeatureAssignments_PlanId_PlanFeatureId]
    ON [dbo].[PlanFeatureAssignments] ([PlanId] ASC, [PlanFeatureId] ASC);

    -- Index for querying features by plan
    CREATE NONCLUSTERED INDEX [IX_PlanFeatureAssignments_PlanId]
    ON [dbo].[PlanFeatureAssignments] ([PlanId] ASC);

    PRINT 'Created PlanFeatureAssignments table';
END
GO

-- Seed default features
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures])
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES
    -- Communication
    ('whatsapp', 'WhatsApp Integration', 'Send messages via WhatsApp Business API', 'Communication', 'fab fa-whatsapp', 1),
    ('email_campaigns', 'Email Campaigns', 'Create and send marketing email campaigns', 'Communication', 'fas fa-envelope', 2),
    ('sms', 'SMS Messaging', 'Send SMS notifications and marketing messages', 'Communication', 'fas fa-sms', 3),

    -- AI Tools
    ('ai_descriptions', 'AI Product Descriptions', 'Generate product descriptions using AI', 'AI Tools', 'fas fa-robot', 1),
    ('ai_seo', 'AI SEO Optimizer', 'AI-powered SEO meta tag generation', 'AI Tools', 'fas fa-search', 2),
    ('ai_pricing', 'AI Pricing Optimizer', 'AI-powered pricing suggestions', 'AI Tools', 'fas fa-dollar-sign', 3),
    ('ai_chatbot', 'AI Customer Chatbot', 'Automated customer support chatbot', 'AI Tools', 'fas fa-comments', 4),
    ('ai_alt_text', 'AI Alt-Text Generator', 'Generate alt text for product images', 'AI Tools', 'fas fa-image', 5),

    -- Analytics
    ('advanced_reports', 'Advanced Reports', 'Access to detailed analytics and reports', 'Analytics', 'fas fa-chart-bar', 1),
    ('inventory_predictions', 'Inventory Predictions', 'AI-based inventory forecasting', 'Analytics', 'fas fa-chart-line', 2),

    -- Operations
    ('purchase_orders', 'Purchase Orders', 'Create and manage purchase orders', 'Operations', 'fas fa-file-invoice', 1),
    ('supplier_management', 'Supplier Management', 'Manage suppliers and vendor relationships', 'Operations', 'fas fa-truck', 2),
    ('label_designer', 'Label Designer', 'Design and print product labels', 'Operations', 'fas fa-tags', 3),
    ('barcode_generator', 'Barcode Generator', 'Generate barcodes for products', 'Operations', 'fas fa-barcode', 4),

    -- Customer Hub
    ('unified_inbox', 'Unified Inbox', 'Manage all customer messages in one place', 'Customer Hub', 'fas fa-inbox', 1),
    ('loyalty_program', 'Loyalty Program', 'Customer loyalty points and rewards', 'Customer Hub', 'fas fa-gift', 2),
    ('exchanges', 'Exchange Management', 'Handle product exchanges', 'Customer Hub', 'fas fa-exchange-alt', 3),

    -- Integrations
    ('api_access', 'API Access', 'Access to REST API for integrations', 'Integrations', 'fas fa-code', 1),
    ('webhooks', 'Custom Webhooks', 'Configure custom webhook endpoints', 'Integrations', 'fas fa-plug', 2),

    -- Marketing
    ('upsell_offers', 'Upsell Offers', 'Create upsell and cross-sell offers', 'Marketing', 'fas fa-arrow-up', 1),
    ('ab_testing', 'A/B Testing', 'Run experiments on offers', 'Marketing', 'fas fa-flask', 2),
    ('abandoned_cart', 'Abandoned Cart Recovery', 'Recover abandoned carts with automation', 'Marketing', 'fas fa-shopping-cart', 3);

    PRINT 'Seeded default features';
END
GO

-- Assign features to plans based on plan tier
-- Only run if no assignments exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatureAssignments])
BEGIN
    DECLARE @FreePlanId INT, @BasicPlanId INT, @PremiumPlanId INT, @EnterprisePlanId INT;

    SELECT @FreePlanId = Id FROM [dbo].[Plans] WHERE [Name] = 'Free';
    SELECT @BasicPlanId = Id FROM [dbo].[Plans] WHERE [Name] = 'Basic';
    SELECT @PremiumPlanId = Id FROM [dbo].[Plans] WHERE [Name] = 'Premium';
    SELECT @EnterprisePlanId = Id FROM [dbo].[Plans] WHERE [Name] = 'Enterprise';

    -- Free plan: minimal features
    IF @FreePlanId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
        SELECT @FreePlanId, Id, 'system'
        FROM [dbo].[PlanFeatures]
        WHERE [Code] IN ('ai_descriptions');
    END

    -- Basic plan
    IF @BasicPlanId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
        SELECT @BasicPlanId, Id, 'system'
        FROM [dbo].[PlanFeatures]
        WHERE [Code] IN ('email_campaigns', 'ai_descriptions', 'ai_seo', 'upsell_offers', 'abandoned_cart');
    END

    -- Premium plan
    IF @PremiumPlanId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
        SELECT @PremiumPlanId, Id, 'system'
        FROM [dbo].[PlanFeatures]
        WHERE [Code] IN ('whatsapp', 'email_campaigns', 'sms', 'ai_descriptions', 'ai_seo', 'ai_pricing',
                         'ai_alt_text', 'advanced_reports', 'upsell_offers', 'ab_testing', 'abandoned_cart', 'unified_inbox');
    END

    -- Enterprise plan: all features
    IF @EnterprisePlanId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
        SELECT @EnterprisePlanId, Id, 'system'
        FROM [dbo].[PlanFeatures];
    END

    PRINT 'Assigned features to plans';
END
GO

PRINT 'PlanFeature tables migration complete';
GO
