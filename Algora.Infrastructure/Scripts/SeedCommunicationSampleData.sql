-- =============================================
-- Seed Sample Data for Communication Module
-- Shop Domain: demo.myshopify.com
-- =============================================

-- Change this to your shop domain
DECLARE @ShopDomain NVARCHAR(200) = 'devlotusalgebra.myshopify.com';

-- =============================================
-- EMAIL TEMPLATES
-- =============================================
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO EmailTemplates (ShopDomain, Name, Subject, Body, TemplateType, IsActive, IsDefault, CreatedAt)
    VALUES
    (@ShopDomain, 'Welcome Email', 'Welcome to {{shop_name}}!', '<h1>Welcome!</h1><p>Thank you for joining us, {{customer_name}}!</p>', 'welcome', 1, 1, DATEADD(DAY, -30, GETUTCDATE())),
    (@ShopDomain, 'Order Confirmation', 'Your order #{{order_number}} is confirmed', '<h1>Order Confirmed</h1><p>Thank you for your order!</p>', 'order_confirmation', 1, 0, DATEADD(DAY, -28, GETUTCDATE())),
    (@ShopDomain, 'Shipping Notification', 'Your order #{{order_number}} has shipped!', '<h1>Your order is on its way!</h1><p>Track your package: {{tracking_url}}</p>', 'shipping', 1, 0, DATEADD(DAY, -25, GETUTCDATE())),
    (@ShopDomain, 'Abandoned Cart Reminder', 'You left something behind...', '<h1>Complete Your Purchase</h1><p>Your cart is waiting for you!</p>', 'abandoned_cart', 1, 0, DATEADD(DAY, -20, GETUTCDATE())),
    (@ShopDomain, 'Holiday Sale Promo', '🎄 Holiday Sale - Up to 50% Off!', '<h1>Holiday Special!</h1><p>Shop now and save big!</p>', 'promotional', 1, 0, DATEADD(DAY, -10, GETUTCDATE()));
    PRINT 'Inserted Email Templates';
END

-- =============================================
-- EMAIL LISTS
-- =============================================
IF NOT EXISTS (SELECT 1 FROM EmailLists WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO EmailLists (ShopDomain, Name, Description, IsActive, IsDefault, DoubleOptIn, SubscriberCount, CreatedAt)
    VALUES
    (@ShopDomain, 'Newsletter Subscribers', 'Main newsletter list', 1, 1, 0, 1250, DATEADD(DAY, -60, GETUTCDATE())),
    (@ShopDomain, 'VIP Customers', 'High-value customers', 1, 0, 0, 156, DATEADD(DAY, -45, GETUTCDATE())),
    (@ShopDomain, 'Sale Alerts', 'Customers interested in sales', 1, 0, 0, 890, DATEADD(DAY, -30, GETUTCDATE()));
    PRINT 'Inserted Email Lists';
END

-- =============================================
-- EMAIL SUBSCRIBERS
-- =============================================
IF NOT EXISTS (SELECT 1 FROM EmailSubscribers WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO EmailSubscribers (ShopDomain, Email, FirstName, LastName, Status, EmailOptIn, SmsOptIn, Source, CreatedAt)
    VALUES
    (@ShopDomain, 'john.doe@example.com', 'John', 'Doe', 'subscribed', 1, 1, 'checkout', DATEADD(DAY, -45, GETUTCDATE())),
    (@ShopDomain, 'jane.smith@example.com', 'Jane', 'Smith', 'subscribed', 1, 0, 'popup', DATEADD(DAY, -30, GETUTCDATE())),
    (@ShopDomain, 'bob.wilson@example.com', 'Bob', 'Wilson', 'subscribed', 1, 1, 'footer', DATEADD(DAY, -25, GETUTCDATE())),
    (@ShopDomain, 'alice.johnson@example.com', 'Alice', 'Johnson', 'subscribed', 1, 0, 'checkout', DATEADD(DAY, -20, GETUTCDATE())),
    (@ShopDomain, 'charlie.brown@example.com', 'Charlie', 'Brown', 'unsubscribed', 0, 0, 'import', DATEADD(DAY, -15, GETUTCDATE()));
    PRINT 'Inserted Email Subscribers';
END

-- =============================================
-- CUSTOMER SEGMENTS
-- =============================================
IF NOT EXISTS (SELECT 1 FROM CustomerSegments WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO CustomerSegments (ShopDomain, Name, Description, SegmentType, FilterCriteria, IsActive, MemberCount, CreatedAt)
    VALUES
    (@ShopDomain, 'All Subscribers', 'All active email subscribers', 'static', '{}', 1, 1250, DATEADD(DAY, -60, GETUTCDATE())),
    (@ShopDomain, 'High Spenders', 'Customers with LTV > $500', 'dynamic', '{"ltv_min": 500}', 1, 85, DATEADD(DAY, -30, GETUTCDATE())),
    (@ShopDomain, 'Recent Purchasers', 'Purchased in last 30 days', 'dynamic', '{"days_since_purchase": 30}', 1, 234, DATEADD(DAY, -15, GETUTCDATE()));
    PRINT 'Inserted Customer Segments';
END

-- =============================================
-- EMAIL CAMPAIGNS
-- =============================================
DECLARE @TemplateId INT = (SELECT TOP 1 Id FROM EmailTemplates WHERE ShopDomain = @ShopDomain AND TemplateType = 'promotional');
DECLARE @SegmentId INT = (SELECT TOP 1 Id FROM CustomerSegments WHERE ShopDomain = @ShopDomain);

IF NOT EXISTS (SELECT 1 FROM EmailCampaigns WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO EmailCampaigns (ShopDomain, Name, Subject, Body, PreviewText, FromName, FromEmail, ReplyToEmail, Status, CampaignType, EmailTemplateId, SegmentId, TotalRecipients, TotalSent, TotalDelivered, TotalOpened, TotalClicked, TotalBounced, TotalUnsubscribed, TotalComplaints, ScheduledAt, SentAt, CreatedAt)
    VALUES
    (@ShopDomain, 'Holiday Sale 2024', '🎄 Holiday Sale - Up to 50% Off!', '<h1>Holiday Special!</h1>', 'Save big this holiday season', 'Demo Store', 'noreply@demo.myshopify.com', 'support@demo.myshopify.com', 'sent', 'promotional', @TemplateId, @SegmentId, 1250, 1245, 1180, 425, 89, 12, 5, 0, DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE())),
    (@ShopDomain, 'New Year Promo', '🎉 New Year, New Deals!', '<h1>New Year Sale!</h1>', 'Start the year with savings', 'Demo Store', 'noreply@demo.myshopify.com', 'support@demo.myshopify.com', 'scheduled', 'promotional', @TemplateId, @SegmentId, 1250, 0, 0, 0, 0, 0, 0, 0, DATEADD(DAY, 7, GETUTCDATE()), NULL, DATEADD(DAY, -3, GETUTCDATE())),
    (@ShopDomain, 'Flash Sale Alert', '⚡ 24-Hour Flash Sale!', '<h1>Flash Sale!</h1>', 'Limited time only', 'Demo Store', 'noreply@demo.myshopify.com', 'support@demo.myshopify.com', 'draft', 'promotional', @TemplateId, @SegmentId, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, DATEADD(DAY, -1, GETUTCDATE()));
    PRINT 'Inserted Email Campaigns';
END

-- =============================================
-- EMAIL CAMPAIGN RECIPIENTS
-- =============================================
DECLARE @CampaignId INT = (SELECT TOP 1 Id FROM EmailCampaigns WHERE ShopDomain = @ShopDomain AND Status = 'sent');

IF @CampaignId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmailCampaignRecipients WHERE EmailCampaignId = @CampaignId)
BEGIN
    INSERT INTO EmailCampaignRecipients (EmailCampaignId, Email, Status, SentAt, DeliveredAt, OpenedAt, ClickedAt, CreatedAt)
    VALUES
    (@CampaignId, 'john.doe@example.com', 'clicked', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE())),
    (@CampaignId, 'jane.smith@example.com', 'opened', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), NULL, DATEADD(DAY, -7, GETUTCDATE())),
    (@CampaignId, 'bob.wilson@example.com', 'delivered', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), NULL, NULL, DATEADD(DAY, -7, GETUTCDATE())),
    (@CampaignId, 'alice.johnson@example.com', 'bounced', DATEADD(DAY, -7, GETUTCDATE()), NULL, NULL, NULL, DATEADD(DAY, -7, GETUTCDATE())),
    (@CampaignId, 'charlie.brown@example.com', 'unsubscribed', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), NULL, DATEADD(DAY, -7, GETUTCDATE()));
    PRINT 'Inserted Email Campaign Recipients';
END

-- =============================================
-- EMAIL AUTOMATIONS
-- =============================================
IF NOT EXISTS (SELECT 1 FROM EmailAutomations WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO EmailAutomations (ShopDomain, Name, Description, TriggerType, TriggerConditions, IsActive, TotalEnrolled, TotalCompleted, Revenue, CreatedAt)
    VALUES
    (@ShopDomain, 'Welcome Series', 'Welcome new subscribers', 'signup', '{}', 1, 450, 380, 12500.00, DATEADD(DAY, -60, GETUTCDATE())),
    (@ShopDomain, 'Abandoned Cart Recovery', 'Recover abandoned carts', 'abandoned_cart', '{"delay_hours": 1}', 1, 125, 45, 8750.00, DATEADD(DAY, -45, GETUTCDATE())),
    (@ShopDomain, 'Post-Purchase Follow-up', 'Thank customers after purchase', 'order_placed', '{"delay_days": 3}', 1, 890, 850, 0, DATEADD(DAY, -30, GETUTCDATE()));
    PRINT 'Inserted Email Automations';
END

-- =============================================
-- SMS TEMPLATES
-- =============================================
IF NOT EXISTS (SELECT 1 FROM SmsTemplates WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO SmsTemplates (ShopDomain, Name, TemplateType, Body, IsActive, CreatedAt)
    VALUES
    (@ShopDomain, 'Order Confirmation', 'transactional', 'Hi {{name}}, your order #{{order_number}} is confirmed! We''ll notify you when it ships.', 1, DATEADD(DAY, -30, GETUTCDATE())),
    (@ShopDomain, 'Shipping Update', 'transactional', 'Great news! Your order #{{order_number}} has shipped. Track it: {{tracking_url}}', 1, DATEADD(DAY, -28, GETUTCDATE())),
    (@ShopDomain, 'Flash Sale', 'marketing', '🔥 {{discount}}% OFF everything! Use code {{code}}. Shop now: {{shop_url}}', 1, DATEADD(DAY, -15, GETUTCDATE())),
    (@ShopDomain, 'Delivery Reminder', 'transactional', 'Your package is out for delivery today! Order #{{order_number}}', 1, DATEADD(DAY, -10, GETUTCDATE()));
    PRINT 'Inserted SMS Templates';
END

-- =============================================
-- SMS MESSAGES
-- =============================================
IF NOT EXISTS (SELECT 1 FROM SmsMessages WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO SmsMessages (ShopDomain, PhoneNumber, Body, Status, SegmentCount, Cost, SentAt, DeliveredAt, CreatedAt)
    VALUES
    (@ShopDomain, '+1234567890', 'Hi John, your order #1001 is confirmed! We''ll notify you when it ships.', 'delivered', 1, 0.05, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE())),
    (@ShopDomain, '+1234567891', '🔥 50% OFF everything! Use code FLASH50. Shop now: demo.myshopify.com', 'delivered', 1, 0.05, DATEADD(HOUR, -5, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE())),
    (@ShopDomain, '+1234567892', 'Great news! Your order #1002 has shipped. Track it: track.demo.com/abc123', 'delivered', 2, 0.10, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE())),
    (@ShopDomain, '+1234567893', 'Your package is out for delivery today! Order #1003', 'sent', 1, 0.05, DATEADD(MINUTE, -30, GETUTCDATE()), NULL, DATEADD(MINUTE, -30, GETUTCDATE())),
    (@ShopDomain, '+1234567894', 'Hi Alice, your order #1004 is confirmed!', 'failed', 1, NULL, NULL, NULL, DATEADD(DAY, -2, GETUTCDATE()));
    PRINT 'Inserted SMS Messages';
END

-- =============================================
-- WHATSAPP TEMPLATES
-- =============================================
IF NOT EXISTS (SELECT 1 FROM WhatsAppTemplates WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO WhatsAppTemplates (ShopDomain, Name, Language, Category, HeaderType, HeaderContent, Body, Footer, Status, IsActive, CreatedAt, ApprovedAt)
    VALUES
    (@ShopDomain, 'order_confirmation', 'en', 'UTILITY', 'text', 'Order Confirmed!', 'Hi {{1}}, your order #{{2}} has been confirmed. Total: {{3}}. We''ll send tracking info when it ships!', 'Reply STOP to unsubscribe', 'APPROVED', 1, DATEADD(DAY, -30, GETUTCDATE()), DATEADD(DAY, -28, GETUTCDATE())),
    (@ShopDomain, 'shipping_update', 'en', 'UTILITY', 'text', 'Your Order Has Shipped!', 'Great news {{1}}! Your order #{{2}} is on its way. Track your package: {{3}}', 'Delivered by Demo Express', 'APPROVED', 1, DATEADD(DAY, -25, GETUTCDATE()), DATEADD(DAY, -23, GETUTCDATE())),
    (@ShopDomain, 'promotional_offer', 'en', 'MARKETING', 'text', 'Special Offer!', 'Hi {{1}}! We have a special offer just for you: {{2}}% off your next order! Use code: {{3}}', 'Valid for 7 days', 'APPROVED', 1, DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, -13, GETUTCDATE())),
    (@ShopDomain, 'abandoned_cart', 'en', 'MARKETING', NULL, NULL, 'Hi {{1}}, you left items in your cart! Complete your purchase and get free shipping. Shop now: {{2}}', NULL, 'PENDING', 0, DATEADD(DAY, -5, GETUTCDATE()), NULL);
    PRINT 'Inserted WhatsApp Templates';
END

-- =============================================
-- WHATSAPP CAMPAIGNS
-- =============================================
DECLARE @WATemplateId INT = (SELECT TOP 1 Id FROM WhatsAppTemplates WHERE ShopDomain = @ShopDomain AND Status = 'APPROVED' AND Category = 'MARKETING');

IF @WATemplateId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM WhatsAppCampaigns WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO WhatsAppCampaigns (ShopDomain, Name, TemplateId, Status, TotalRecipients, TotalSent, TotalDelivered, TotalRead, TotalFailed, ScheduledAt, SentAt, CreatedAt)
    VALUES
    (@ShopDomain, 'Holiday Promo 2024', @WATemplateId, 'sent', 500, 485, 478, 312, 7, DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE())),
    (@ShopDomain, 'New Year Special', @WATemplateId, 'scheduled', 750, 0, 0, 0, 0, DATEADD(DAY, 5, GETUTCDATE()), NULL, DATEADD(DAY, -2, GETUTCDATE())),
    (@ShopDomain, 'Flash Sale Alert', @WATemplateId, 'draft', 0, 0, 0, 0, 0, NULL, NULL, DATEADD(DAY, -1, GETUTCDATE()));
    PRINT 'Inserted WhatsApp Campaigns';
END

-- =============================================
-- WHATSAPP CONVERSATIONS
-- =============================================
IF NOT EXISTS (SELECT 1 FROM WhatsAppConversations WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO WhatsAppConversations (ShopDomain, PhoneNumber, CustomerName, Status, UnreadCount, LastMessageAt, LastMessagePreview, IsBusinessInitiated, CreatedAt)
    VALUES
    (@ShopDomain, '+1234567890', 'John Doe', 'open', 2, DATEADD(MINUTE, -15, GETUTCDATE()), 'When will my order arrive?', 0, DATEADD(DAY, -5, GETUTCDATE())),
    (@ShopDomain, '+1234567891', 'Jane Smith', 'open', 0, DATEADD(HOUR, -2, GETUTCDATE()), 'Thank you for the quick response!', 0, DATEADD(DAY, -3, GETUTCDATE())),
    (@ShopDomain, '+1234567892', 'Bob Wilson', 'closed', 0, DATEADD(DAY, -1, GETUTCDATE()), 'Issue resolved. Thanks!', 0, DATEADD(DAY, -7, GETUTCDATE())),
    (@ShopDomain, '+1234567893', 'Alice Johnson', 'open', 1, DATEADD(HOUR, -6, GETUTCDATE()), 'Do you have this in blue?', 0, DATEADD(DAY, -2, GETUTCDATE()));
    PRINT 'Inserted WhatsApp Conversations';
END

-- =============================================
-- WHATSAPP MESSAGES
-- =============================================
DECLARE @ConvId1 INT = (SELECT TOP 1 Id FROM WhatsAppConversations WHERE ShopDomain = @ShopDomain AND CustomerName = 'John Doe');
DECLARE @ConvId2 INT = (SELECT TOP 1 Id FROM WhatsAppConversations WHERE ShopDomain = @ShopDomain AND CustomerName = 'Jane Smith');

IF @ConvId1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM WhatsAppMessages WHERE ShopDomain = @ShopDomain)
BEGIN
    INSERT INTO WhatsAppMessages (ShopDomain, ConversationId, PhoneNumber, Direction, MessageType, Content, Status, SentAt, DeliveredAt, ReadAt, CreatedAt)
    VALUES
    -- John Doe conversation
    (@ShopDomain, @ConvId1, '+1234567890', 'inbound', 'text', 'Hi, I placed order #1001 yesterday. When will it ship?', 'read', NULL, NULL, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE())),
    (@ShopDomain, @ConvId1, '+1234567890', 'outbound', 'text', 'Hi John! Your order is being prepared and will ship within 24 hours. You''ll receive tracking info via email.', 'read', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE())),
    (@ShopDomain, @ConvId1, '+1234567890', 'inbound', 'text', 'When will my order arrive?', 'delivered', NULL, NULL, NULL, DATEADD(MINUTE, -15, GETUTCDATE())),
    -- Jane Smith conversation
    (@ShopDomain, @ConvId2, '+1234567891', 'inbound', 'text', 'I need help with my return', 'read', NULL, NULL, DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE())),
    (@ShopDomain, @ConvId2, '+1234567891', 'outbound', 'text', 'Of course! I can help you with that. Can you provide your order number?', 'read', DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE())),
    (@ShopDomain, @ConvId2, '+1234567891', 'inbound', 'text', 'Order #1002. I want to return the blue shirt.', 'read', NULL, NULL, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE())),
    (@ShopDomain, @ConvId2, '+1234567891', 'outbound', 'text', 'I''ve initiated the return for your blue shirt. You''ll receive a return label via email shortly.', 'read', DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE())),
    (@ShopDomain, @ConvId2, '+1234567891', 'inbound', 'text', 'Thank you for the quick response!', 'delivered', NULL, NULL, NULL, DATEADD(HOUR, -2, GETUTCDATE()));
    PRINT 'Inserted WhatsApp Messages';
END

PRINT '';
PRINT '=============================================';
PRINT 'Sample data seeding completed successfully!';
PRINT '=============================================';
GO
