-- Insert Sample Data for Analytics Dashboard Testing
-- This script adds sample customers, orders, order lines, and ads spend data

SET QUOTED_IDENTIFIER ON;
GO

DECLARE @shopDomain NVARCHAR(200) = 'devlotusalgebra.myshopify.com';

-- =============================================
-- 1. Insert Sample Customers (without TotalOrders/TotalSpent)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'john.doe@example.com')
BEGIN
    INSERT INTO Customers (ShopDomain, PlatformCustomerId, Email, FirstName, LastName, Phone, CreatedAt)
    VALUES
        (@shopDomain, 1001, 'john.doe@example.com', 'John', 'Doe', '+1234567890', DATEADD(DAY, -90, GETUTCDATE())),
        (@shopDomain, 1002, 'jane.smith@example.com', 'Jane', 'Smith', '+1234567891', DATEADD(DAY, -60, GETUTCDATE())),
        (@shopDomain, 1003, 'bob.wilson@example.com', 'Bob', 'Wilson', '+1234567892', DATEADD(DAY, -120, GETUTCDATE())),
        (@shopDomain, 1004, 'alice.johnson@example.com', 'Alice', 'Johnson', '+1234567893', DATEADD(DAY, -30, GETUTCDATE())),
        (@shopDomain, 1005, 'charlie.brown@example.com', 'Charlie', 'Brown', '+1234567894', DATEADD(DAY, -180, GETUTCDATE())),
        (@shopDomain, 1006, 'diana.prince@example.com', 'Diana', 'Prince', '+1234567895', DATEADD(DAY, -7, GETUTCDATE())),
        (@shopDomain, 1007, 'evan.rogers@example.com', 'Evan', 'Rogers', '+1234567896', DATEADD(DAY, -45, GETUTCDATE())),
        (@shopDomain, 1008, 'fiona.green@example.com', 'Fiona', 'Green', '+1234567897', DATEADD(DAY, -75, GETUTCDATE()));
    PRINT 'Inserted 8 sample customers';
END
ELSE
BEGIN
    PRINT 'Sample customers already exist';
END
GO

-- =============================================
-- 2. Insert Sample Orders (Last 90 days)
-- =============================================
SET QUOTED_IDENTIFIER ON;
DECLARE @shopDomain NVARCHAR(200) = 'devlotusalgebra.myshopify.com';

-- Insert orders for the last 90 days
IF NOT EXISTS (SELECT 1 FROM Orders WHERE ShopDomain = @shopDomain AND PlatformOrderId = 9001)
BEGIN
    INSERT INTO Orders (ShopDomain, PlatformOrderId, OrderNumber, CustomerId, CustomerEmail, Subtotal, TaxTotal, ShippingTotal, DiscountTotal, GrandTotal, Currency, FinancialStatus, FulfillmentStatus, OrderDate, CreatedAt)
    VALUES
        -- Today's orders
        (@shopDomain, 9001, '#1001', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'john.doe@example.com'), 'john.doe@example.com', 89.99, 7.20, 5.00, 0, 102.19, 'USD', 'paid', 'fulfilled', GETUTCDATE(), GETUTCDATE()),
        (@shopDomain, 9002, '#1002', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'jane.smith@example.com'), 'jane.smith@example.com', 149.99, 12.00, 0, 10.00, 151.99, 'USD', 'paid', 'fulfilled', GETUTCDATE(), GETUTCDATE()),

        -- Yesterday's orders
        (@shopDomain, 9003, '#1003', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'bob.wilson@example.com'), 'bob.wilson@example.com', 199.99, 16.00, 5.00, 0, 220.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE())),
        (@shopDomain, 9004, '#1004', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'alice.johnson@example.com'), 'alice.johnson@example.com', 59.99, 4.80, 5.00, 0, 69.79, 'USD', 'paid', 'unfulfilled', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE())),

        -- 2 days ago
        (@shopDomain, 9005, '#1005', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'charlie.brown@example.com'), 'charlie.brown@example.com', 299.99, 24.00, 0, 20.00, 303.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE())),

        -- 3 days ago
        (@shopDomain, 9006, '#1006', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'diana.prince@example.com'), 'diana.prince@example.com', 74.99, 6.00, 5.00, 0, 85.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE())),
        (@shopDomain, 9007, '#1007', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'evan.rogers@example.com'), 'evan.rogers@example.com', 124.99, 10.00, 5.00, 5.00, 134.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE())),

        -- 5 days ago
        (@shopDomain, 9008, '#1008', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'fiona.green@example.com'), 'fiona.green@example.com', 179.99, 14.40, 0, 0, 194.39, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE())),
        (@shopDomain, 9009, '#1009', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'john.doe@example.com'), 'john.doe@example.com', 89.99, 7.20, 5.00, 0, 102.19, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE())),

        -- 7 days ago
        (@shopDomain, 9010, '#1010', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'bob.wilson@example.com'), 'bob.wilson@example.com', 249.99, 20.00, 0, 15.00, 254.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE())),
        (@shopDomain, 9011, '#1011', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'charlie.brown@example.com'), 'charlie.brown@example.com', 99.99, 8.00, 5.00, 0, 112.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE())),

        -- 14 days ago
        (@shopDomain, 9012, '#1012', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'jane.smith@example.com'), 'jane.smith@example.com', 159.99, 12.80, 0, 0, 172.79, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -14, GETUTCDATE()), DATEADD(DAY, -14, GETUTCDATE())),
        (@shopDomain, 9013, '#1013', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'evan.rogers@example.com'), 'evan.rogers@example.com', 79.99, 6.40, 5.00, 0, 91.39, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -14, GETUTCDATE()), DATEADD(DAY, -14, GETUTCDATE())),

        -- 21 days ago
        (@shopDomain, 9014, '#1014', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'fiona.green@example.com'), 'fiona.green@example.com', 199.99, 16.00, 0, 10.00, 205.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -21, GETUTCDATE()), DATEADD(DAY, -21, GETUTCDATE())),
        (@shopDomain, 9015, '#1015', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'alice.johnson@example.com'), 'alice.johnson@example.com', 69.99, 5.60, 5.00, 0, 80.59, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -21, GETUTCDATE()), DATEADD(DAY, -21, GETUTCDATE())),

        -- 30 days ago
        (@shopDomain, 9016, '#1016', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'bob.wilson@example.com'), 'bob.wilson@example.com', 349.99, 28.00, 0, 25.00, 352.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -30, GETUTCDATE()), DATEADD(DAY, -30, GETUTCDATE())),
        (@shopDomain, 9017, '#1017', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'diana.prince@example.com'), 'diana.prince@example.com', 119.99, 9.60, 5.00, 0, 134.59, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -30, GETUTCDATE()), DATEADD(DAY, -30, GETUTCDATE())),

        -- 45 days ago
        (@shopDomain, 9018, '#1018', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'charlie.brown@example.com'), 'charlie.brown@example.com', 189.99, 15.20, 0, 0, 205.19, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -45, GETUTCDATE()), DATEADD(DAY, -45, GETUTCDATE())),
        (@shopDomain, 9019, '#1019', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'john.doe@example.com'), 'john.doe@example.com', 139.99, 11.20, 5.00, 5.00, 151.19, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -45, GETUTCDATE()), DATEADD(DAY, -45, GETUTCDATE())),

        -- 60 days ago
        (@shopDomain, 9020, '#1020', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'evan.rogers@example.com'), 'evan.rogers@example.com', 229.99, 18.40, 0, 10.00, 238.39, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -60, GETUTCDATE()), DATEADD(DAY, -60, GETUTCDATE())),
        (@shopDomain, 9021, '#1021', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'fiona.green@example.com'), 'fiona.green@example.com', 99.99, 8.00, 5.00, 0, 112.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -60, GETUTCDATE()), DATEADD(DAY, -60, GETUTCDATE())),

        -- 75 days ago
        (@shopDomain, 9022, '#1022', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'jane.smith@example.com'), 'jane.smith@example.com', 279.99, 22.40, 0, 15.00, 287.39, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -75, GETUTCDATE()), DATEADD(DAY, -75, GETUTCDATE())),
        (@shopDomain, 9023, '#1023', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'bob.wilson@example.com'), 'bob.wilson@example.com', 159.99, 12.80, 5.00, 0, 177.79, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -75, GETUTCDATE()), DATEADD(DAY, -75, GETUTCDATE())),

        -- 90 days ago
        (@shopDomain, 9024, '#1024', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'alice.johnson@example.com'), 'alice.johnson@example.com', 199.99, 16.00, 0, 0, 215.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -90, GETUTCDATE()), DATEADD(DAY, -90, GETUTCDATE())),
        (@shopDomain, 9025, '#1025', (SELECT TOP 1 Id FROM Customers WHERE ShopDomain = @shopDomain AND Email = 'charlie.brown@example.com'), 'charlie.brown@example.com', 449.99, 36.00, 0, 30.00, 455.99, 'USD', 'paid', 'fulfilled', DATEADD(DAY, -90, GETUTCDATE()), DATEADD(DAY, -90, GETUTCDATE()));

    PRINT 'Inserted 25 sample orders';
END
ELSE
BEGIN
    PRINT 'Sample orders already exist';
END
GO

-- =============================================
-- 3. Insert Order Lines for each order
-- =============================================
SET QUOTED_IDENTIFIER ON;
DECLARE @shopDomain NVARCHAR(200) = 'devlotusalgebra.myshopify.com';

IF NOT EXISTS (SELECT 1 FROM OrderLines ol INNER JOIN Orders o ON ol.OrderId = o.Id WHERE o.PlatformOrderId = 9001)
BEGIN
    INSERT INTO OrderLines (OrderId, PlatformLineItemId, PlatformProductId, PlatformVariantId, ProductTitle, VariantTitle, Sku, Quantity, UnitPrice, DiscountAmount, TaxAmount, LineTotal)
    SELECT
        o.Id,
        o.PlatformOrderId * 10 + 1,
        8000000000001 + (o.PlatformOrderId % 5),
        9000000000001 + (o.PlatformOrderId % 10),
        CASE (o.PlatformOrderId % 5)
            WHEN 0 THEN 'Premium Wireless Headphones'
            WHEN 1 THEN 'Smart Watch Pro'
            WHEN 2 THEN 'Bluetooth Speaker'
            WHEN 3 THEN 'USB-C Hub'
            WHEN 4 THEN 'Laptop Stand'
        END,
        CASE (o.PlatformOrderId % 3)
            WHEN 0 THEN 'Black'
            WHEN 1 THEN 'White'
            WHEN 2 THEN 'Silver'
        END,
        'SKU-' + CAST(o.PlatformOrderId AS NVARCHAR(10)),
        1 + (o.PlatformOrderId % 3),
        o.Subtotal / (1 + (o.PlatformOrderId % 3)),
        o.DiscountTotal,
        o.TaxTotal,
        o.Subtotal
    FROM Orders o
    WHERE o.ShopDomain = @shopDomain AND o.PlatformOrderId BETWEEN 9001 AND 9025;

    PRINT 'Inserted order lines for all orders';
END
ELSE
BEGIN
    PRINT 'Order lines already exist';
END
GO

-- =============================================
-- 4. Update Product COGS for profit calculations
-- =============================================
UPDATE Products SET CostOfGoodsSold = Price * 0.4 WHERE CostOfGoodsSold IS NULL;
UPDATE ProductVariants SET CostOfGoodsSold = Price * 0.4 WHERE CostOfGoodsSold IS NULL;
PRINT 'Updated COGS for products (40% of price)';
GO

-- =============================================
-- 5. Clear old snapshots for fresh calculation
-- =============================================
DECLARE @shopDomain NVARCHAR(200) = 'devlotusalgebra.myshopify.com';
DELETE FROM AnalyticsSnapshots WHERE ShopDomain = @shopDomain;
DELETE FROM CustomerLifetimeValues WHERE ShopDomain = @shopDomain;
PRINT 'Cleared old analytics snapshots for fresh calculation';
GO

PRINT '========================================';
PRINT 'Sample data insertion completed!';
PRINT '- 8 customers';
PRINT '- 25 orders across 90 days';
PRINT '- 25 order lines';
PRINT '- 21 ads spend records (already inserted)';
PRINT '- Product COGS updated';
PRINT '========================================';
PRINT 'Refresh the Analytics Dashboard to see the data!';
GO
