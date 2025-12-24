-- Migration script to update pricing plans
-- From: Free ($0), Premium ($29), Enterprise ($99)
-- To: Free ($0), Basic ($99), Premium ($199), Enterprise ($299)

-- Update existing Premium plan to new Basic plan
UPDATE Plans
SET Name = 'Basic',
    Description = 'Essential tools for small businesses',
    MonthlyPrice = 99,
    OrderLimit = 500,
    ProductLimit = 250,
    CustomerLimit = 500,
    HasAdvancedReports = 0,
    SortOrder = 2
WHERE Name = 'Premium';

-- Update existing Enterprise plan to new Premium plan
UPDATE Plans
SET Name = 'Premium',
    Description = 'For growing businesses with advanced marketing',
    MonthlyPrice = 199,
    OrderLimit = 2000,
    ProductLimit = 1000,
    CustomerLimit = 2000,
    HasApiAccess = 0,
    SortOrder = 3
WHERE Name = 'Enterprise';

-- Insert new Enterprise plan
IF NOT EXISTS (SELECT 1 FROM Plans WHERE Name = 'Enterprise')
BEGIN
    INSERT INTO Plans (Name, Description, MonthlyPrice, OrderLimit, ProductLimit, CustomerLimit,
                       HasWhatsApp, HasEmailCampaigns, HasSms, HasAdvancedReports, HasApiAccess,
                       SortOrder, IsActive, TrialDays)
    VALUES ('Enterprise', 'Unlimited access for large-scale operations', 299, -1, -1, -1,
            1, 1, 1, 1, 1, 4, 1, 14);
END
