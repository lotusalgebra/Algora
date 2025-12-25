-- Update Plan Prices
-- Free: $0, Basic: $99, Premium: $199, Enterprise: $299

UPDATE Plans SET MonthlyPrice = 0 WHERE Name = 'Free';
UPDATE Plans SET MonthlyPrice = 99 WHERE Name = 'Basic';
UPDATE Plans SET MonthlyPrice = 199 WHERE Name = 'Premium';
UPDATE Plans SET MonthlyPrice = 299 WHERE Name = 'Enterprise';

-- Verify the updates
SELECT Name, MonthlyPrice, OrderLimit, ProductLimit FROM Plans ORDER BY SortOrder;
