-- Add Commerce and Inventory features to PlanFeatures table
SET QUOTED_IDENTIFIER ON
GO

-- Add Commerce features
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'orders_management')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES
    ('orders_management', 'Orders Management', 'View, manage, and fulfill customer orders', 'Commerce', 'fas fa-shopping-cart', 1),
    ('products_management', 'Products Management', 'Create and manage product catalog', 'Commerce', 'fas fa-box', 2),
    ('product_bundles', 'Product Bundles', 'Create product bundles and kits', 'Commerce', 'fas fa-layer-group', 3),
    ('bulk_import_export', 'Bulk Import/Export', 'Import and export products and orders in bulk', 'Commerce', 'fas fa-file-import', 4);

    PRINT 'Added Commerce features';
END
GO

-- Add Inventory features
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'inventory_tracking')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES
    ('inventory_tracking', 'Inventory Tracking', 'Track stock levels across locations', 'Inventory', 'fas fa-warehouse', 1),
    ('inventory_alerts', 'Inventory Alerts', 'Get notified when stock is low or out of stock', 'Inventory', 'fas fa-bell', 2),
    ('stock_thresholds', 'Stock Thresholds', 'Set custom reorder points and safety stock levels', 'Inventory', 'fas fa-sliders-h', 3),
    ('multi_location', 'Multi-Location Inventory', 'Manage inventory across multiple warehouses', 'Inventory', 'fas fa-map-marker-alt', 4);

    PRINT 'Added Inventory features';
END
GO

-- Assign Commerce features to plans
-- Free plan gets basic orders and products
DECLARE @FreePlanId INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Free');
IF @FreePlanId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatureAssignments] pfa
    INNER JOIN [dbo].[PlanFeatures] pf ON pfa.PlanFeatureId = pf.Id
    WHERE pfa.PlanId = @FreePlanId AND pf.Code = 'orders_management')
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @FreePlanId, Id, 'system'
    FROM [dbo].[PlanFeatures]
    WHERE [Code] IN ('orders_management', 'products_management', 'inventory_tracking');

    PRINT 'Assigned Commerce/Inventory features to Free plan';
END
GO

-- Basic plan gets Commerce + basic inventory
DECLARE @BasicPlanId INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Basic');
IF @BasicPlanId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatureAssignments] pfa
    INNER JOIN [dbo].[PlanFeatures] pf ON pfa.PlanFeatureId = pf.Id
    WHERE pfa.PlanId = @BasicPlanId AND pf.Code = 'orders_management')
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @BasicPlanId, Id, 'system'
    FROM [dbo].[PlanFeatures]
    WHERE [Code] IN ('orders_management', 'products_management', 'product_bundles',
                     'inventory_tracking', 'inventory_alerts');

    PRINT 'Assigned Commerce/Inventory features to Basic plan';
END
GO

-- Premium plan gets all Commerce + most inventory
DECLARE @PremiumPlanId INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Premium');
IF @PremiumPlanId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatureAssignments] pfa
    INNER JOIN [dbo].[PlanFeatures] pf ON pfa.PlanFeatureId = pf.Id
    WHERE pfa.PlanId = @PremiumPlanId AND pf.Code = 'orders_management')
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @PremiumPlanId, Id, 'system'
    FROM [dbo].[PlanFeatures]
    WHERE [Code] IN ('orders_management', 'products_management', 'product_bundles', 'bulk_import_export',
                     'inventory_tracking', 'inventory_alerts', 'stock_thresholds');

    PRINT 'Assigned Commerce/Inventory features to Premium plan';
END
GO

-- Enterprise plan gets all features (handled by existing script, but ensure new ones are added)
DECLARE @EnterprisePlanId INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Enterprise');
IF @EnterprisePlanId IS NOT NULL
BEGIN
    -- Add any missing features to Enterprise
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @EnterprisePlanId, pf.Id, 'system'
    FROM [dbo].[PlanFeatures] pf
    WHERE pf.[Category] IN ('Commerce', 'Inventory')
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanFeatureAssignments] pfa
        WHERE pfa.PlanId = @EnterprisePlanId AND pfa.PlanFeatureId = pf.Id
    );

    PRINT 'Assigned Commerce/Inventory features to Enterprise plan';
END
GO

-- Add Returns feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'returns')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES ('returns', 'Returns Management', 'Process customer returns and refunds', 'Returns', 'fas fa-undo', 1);
    PRINT 'Added Returns feature';
END
GO

-- Add Reviews feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'reviews')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES ('reviews', 'Product Reviews', 'Collect and manage product reviews', 'Reviews', 'fas fa-star', 1);
    PRINT 'Added Reviews feature';
END
GO

-- Add Customer Management feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'customers')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES ('customers', 'Customer Management', 'View and manage customer data', 'Commerce', 'fas fa-users', 5);
    PRINT 'Added Customers feature';
END
GO

-- Add Packing Slips feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'packing_slips')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES ('packing_slips', 'Packing Slips', 'Generate packing slips for orders', 'Operations', 'fas fa-file-alt', 5);
    PRINT 'Added Packing Slips feature';
END
GO

-- Add Live Chat feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'live_chat')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES ('live_chat', 'Live Chat Support', 'Real-time customer support chat', 'Customer Hub', 'fas fa-headset', 4);
    PRINT 'Added Live Chat feature';
END
GO

-- Assign new features to Basic plan
DECLARE @BasicPlanIdNew INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Basic');
IF @BasicPlanIdNew IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @BasicPlanIdNew, Id, 'system'
    FROM [dbo].[PlanFeatures]
    WHERE [Code] IN ('returns', 'reviews', 'customers')
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanFeatureAssignments]
        WHERE PlanId = @BasicPlanIdNew AND PlanFeatureId = [PlanFeatures].Id
    );
    PRINT 'Assigned new features to Basic plan';
END
GO

-- Assign new features to Premium plan
DECLARE @PremiumPlanIdNew INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Premium');
IF @PremiumPlanIdNew IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @PremiumPlanIdNew, Id, 'system'
    FROM [dbo].[PlanFeatures]
    WHERE [Code] IN ('returns', 'reviews', 'customers', 'packing_slips')
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanFeatureAssignments]
        WHERE PlanId = @PremiumPlanIdNew AND PlanFeatureId = [PlanFeatures].Id
    );
    PRINT 'Assigned new features to Premium plan';
END
GO

-- Assign all new features to Enterprise plan
DECLARE @EnterprisePlanIdNew INT = (SELECT Id FROM [dbo].[Plans] WHERE [Name] = 'Enterprise');
IF @EnterprisePlanIdNew IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
    SELECT @EnterprisePlanIdNew, pf.Id, 'system'
    FROM [dbo].[PlanFeatures] pf
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanFeatureAssignments] pfa
        WHERE pfa.PlanId = @EnterprisePlanIdNew AND pfa.PlanFeatureId = pf.Id
    );
    PRINT 'Assigned all features to Enterprise plan';
END
GO

PRINT 'Commerce and Inventory features migration complete';
GO
