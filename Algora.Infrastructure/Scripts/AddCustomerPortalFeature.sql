-- Add Customer Portal feature (Enterprise Plan only)
-- This feature provides a branded customer self-service portal with:
-- - Login/Registration with custom fields
-- - Order history and tracking
-- - Return request management
-- - Profile management
-- - Configurable theme and branding

SET QUOTED_IDENTIFIER ON
GO

-- Add Customer Portal feature if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[PlanFeatures] WHERE [Code] = 'customer_portal')
BEGIN
    INSERT INTO [dbo].[PlanFeatures] ([Code], [Name], [Description], [Category], [IconClass], [SortOrder])
    VALUES (
        'customer_portal',
        'Customer Portal',
        'Branded customer self-service portal with login, orders, returns, and profile management. Includes configurable theme and custom fields.',
        'Customer Hub',
        'fas fa-user-circle',
        4
    );

    PRINT 'Added Customer Portal feature';
END
GO

-- Assign Customer Portal to Enterprise plan only
DECLARE @EnterprisePlanId INT;
DECLARE @CustomerPortalFeatureId INT;

SELECT @EnterprisePlanId = Id FROM [dbo].[Plans] WHERE [Name] = 'Enterprise';
SELECT @CustomerPortalFeatureId = Id FROM [dbo].[PlanFeatures] WHERE [Code] = 'customer_portal';

IF @EnterprisePlanId IS NOT NULL AND @CustomerPortalFeatureId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanFeatureAssignments]
        WHERE [PlanId] = @EnterprisePlanId AND [PlanFeatureId] = @CustomerPortalFeatureId
    )
    BEGIN
        INSERT INTO [dbo].[PlanFeatureAssignments] ([PlanId], [PlanFeatureId], [AssignedBy])
        VALUES (@EnterprisePlanId, @CustomerPortalFeatureId, 'system');

        PRINT 'Assigned Customer Portal to Enterprise plan';
    END
END
GO

PRINT 'Customer Portal feature migration complete';
GO
