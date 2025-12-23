-- =============================================
-- Customer Experience Hub Database Migration
-- Creates tables for Unified Inbox, AI Suggestions,
-- Social Media, Exchanges, and Loyalty Program
-- =============================================

-- ==================== UNIFIED INBOX ====================

-- Conversation threads linking messages across channels
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConversationThreads')
BEGIN
    CREATE TABLE ConversationThreads (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        CustomerId INT NULL,
        CustomerEmail NVARCHAR(200) NULL,
        CustomerPhone NVARCHAR(50) NULL,
        CustomerName NVARCHAR(200) NULL,
        Subject NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'open',
        Priority NVARCHAR(20) NOT NULL DEFAULT 'normal',
        AssignedToUserId NVARCHAR(100) NULL,
        Channel NVARCHAR(20) NOT NULL,
        LastMessageAt DATETIME2 NULL,
        LastMessagePreview NVARCHAR(500) NULL,
        UnreadCount INT NOT NULL DEFAULT 0,
        Tags NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        ResolvedAt DATETIME2 NULL,
        CONSTRAINT FK_ConversationThreads_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_ConversationThreads_Shop_Status ON ConversationThreads(ShopDomain, Status);
    CREATE INDEX IX_ConversationThreads_Customer ON ConversationThreads(CustomerId);
    PRINT 'Created table: ConversationThreads';
END
GO

-- Individual messages within a thread
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConversationMessages')
BEGIN
    CREATE TABLE ConversationMessages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ConversationThreadId INT NOT NULL,
        Channel NVARCHAR(20) NOT NULL,
        Direction NVARCHAR(10) NOT NULL,
        ExternalMessageId NVARCHAR(200) NULL,
        SenderType NVARCHAR(20) NOT NULL,
        SenderName NVARCHAR(200) NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ContentType NVARCHAR(20) NOT NULL DEFAULT 'text',
        MediaUrl NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'sent',
        SentAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        DeliveredAt DATETIME2 NULL,
        ReadAt DATETIME2 NULL,
        AiSuggestionUsed BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ConversationMessages_Thread FOREIGN KEY (ConversationThreadId) REFERENCES ConversationThreads(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ConversationMessages_Thread ON ConversationMessages(ConversationThreadId);
    PRINT 'Created table: ConversationMessages';
END
GO

-- AI-generated response suggestions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AiSuggestions')
BEGIN
    CREATE TABLE AiSuggestions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ConversationThreadId INT NOT NULL,
        ConversationMessageId INT NULL,
        SuggestionText NVARCHAR(MAX) NOT NULL,
        Confidence DECIMAL(5,2) NOT NULL,
        Provider NVARCHAR(50) NOT NULL,
        Model NVARCHAR(100) NOT NULL,
        TokensUsed INT NULL,
        EstimatedCost DECIMAL(10,6) NULL,
        WasAccepted BIT NULL,
        WasModified BIT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        AcceptedAt DATETIME2 NULL,
        CONSTRAINT FK_AiSuggestions_Thread FOREIGN KEY (ConversationThreadId) REFERENCES ConversationThreads(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AiSuggestions_Message FOREIGN KEY (ConversationMessageId) REFERENCES ConversationMessages(Id) ON DELETE NO ACTION
    );
    PRINT 'Created table: AiSuggestions';
END
GO

-- Quick reply templates
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuickReplies')
BEGIN
    CREATE TABLE QuickReplies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        Title NVARCHAR(100) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(50) NULL,
        Shortcut NVARCHAR(20) NULL,
        UsageCount INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_QuickReplies_Shop ON QuickReplies(ShopDomain);
    PRINT 'Created table: QuickReplies';
END
GO

-- ==================== SOCIAL MEDIA (Facebook/Instagram) ====================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SocialMediaSettings')
BEGIN
    CREATE TABLE SocialMediaSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        FacebookPageId NVARCHAR(100) NULL,
        FacebookPageAccessToken NVARCHAR(500) NULL,
        InstagramAccountId NVARCHAR(100) NULL,
        MetaAppId NVARCHAR(100) NULL,
        MetaAppSecret NVARCHAR(200) NULL,
        WebhookVerifyToken NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UQ_SocialMediaSettings_Shop UNIQUE (ShopDomain)
    );
    PRINT 'Created table: SocialMediaSettings';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FacebookMessages')
BEGIN
    CREATE TABLE FacebookMessages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        FacebookMessageId NVARCHAR(100) NOT NULL,
        SenderId NVARCHAR(100) NOT NULL,
        SenderName NVARCHAR(200) NULL,
        RecipientId NVARCHAR(100) NOT NULL,
        Direction NVARCHAR(10) NOT NULL,
        MessageType NVARCHAR(20) NOT NULL,
        Content NVARCHAR(MAX) NULL,
        MediaUrl NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'sent',
        SentAt DATETIME2 NOT NULL,
        DeliveredAt DATETIME2 NULL,
        ReadAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_FacebookMessages_Shop ON FacebookMessages(ShopDomain);
    CREATE INDEX IX_FacebookMessages_Sender ON FacebookMessages(SenderId);
    PRINT 'Created table: FacebookMessages';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstagramMessages')
BEGIN
    CREATE TABLE InstagramMessages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        InstagramMessageId NVARCHAR(100) NOT NULL,
        SenderId NVARCHAR(100) NOT NULL,
        SenderUsername NVARCHAR(200) NULL,
        RecipientId NVARCHAR(100) NOT NULL,
        Direction NVARCHAR(10) NOT NULL,
        MessageType NVARCHAR(20) NOT NULL,
        Content NVARCHAR(MAX) NULL,
        MediaUrl NVARCHAR(500) NULL,
        StoryId NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'sent',
        SentAt DATETIME2 NOT NULL,
        DeliveredAt DATETIME2 NULL,
        ReadAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_InstagramMessages_Shop ON InstagramMessages(ShopDomain);
    CREATE INDEX IX_InstagramMessages_Sender ON InstagramMessages(SenderId);
    PRINT 'Created table: InstagramMessages';
END
GO

-- ==================== EXCHANGES ====================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Exchanges')
BEGIN
    CREATE TABLE Exchanges (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        ExchangeNumber NVARCHAR(50) NOT NULL,
        OrderId INT NOT NULL,
        OrderNumber NVARCHAR(50) NOT NULL,
        CustomerId INT NULL,
        CustomerEmail NVARCHAR(200) NOT NULL,
        CustomerName NVARCHAR(200) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'pending',
        ReturnRequestId INT NULL,
        NewOrderId INT NULL,
        PriceDifference DECIMAL(18,4) NOT NULL DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL DEFAULT 'USD',
        Notes NVARCHAR(2000) NULL,
        ApprovedAt DATETIME2 NULL,
        CompletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_Exchanges_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Exchanges_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Exchanges_ReturnRequest FOREIGN KEY (ReturnRequestId) REFERENCES ReturnRequests(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Exchanges_NewOrder FOREIGN KEY (NewOrderId) REFERENCES Orders(Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_Exchanges_Number UNIQUE (ShopDomain, ExchangeNumber)
    );
    CREATE INDEX IX_Exchanges_Shop_Status ON Exchanges(ShopDomain, Status);
    PRINT 'Created table: Exchanges';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExchangeItems')
BEGIN
    CREATE TABLE ExchangeItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ExchangeId INT NOT NULL,
        OriginalOrderLineId INT NOT NULL,
        OriginalProductId INT NOT NULL,
        OriginalProductVariantId INT NULL,
        OriginalProductTitle NVARCHAR(300) NOT NULL,
        OriginalVariantTitle NVARCHAR(200) NULL,
        OriginalSku NVARCHAR(100) NULL,
        OriginalPrice DECIMAL(18,4) NOT NULL,
        Quantity INT NOT NULL,
        NewProductId INT NULL,
        NewProductVariantId INT NULL,
        NewProductTitle NVARCHAR(300) NULL,
        NewVariantTitle NVARCHAR(200) NULL,
        NewSku NVARCHAR(100) NULL,
        NewPrice DECIMAL(18,4) NULL,
        Reason NVARCHAR(500) NULL,
        CustomerNote NVARCHAR(1000) NULL,
        CONSTRAINT FK_ExchangeItems_Exchange FOREIGN KEY (ExchangeId) REFERENCES Exchanges(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ExchangeItems_NewProduct FOREIGN KEY (NewProductId) REFERENCES Products(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ExchangeItems_NewVariant FOREIGN KEY (NewProductVariantId) REFERENCES ProductVariants(Id) ON DELETE NO ACTION
    );
    PRINT 'Created table: ExchangeItems';
END
GO

-- ==================== LOYALTY PROGRAM ====================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyPrograms')
BEGIN
    CREATE TABLE LoyaltyPrograms (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        Name NVARCHAR(100) NOT NULL DEFAULT 'Rewards Program',
        IsActive BIT NOT NULL DEFAULT 0,
        PointsPerDollar INT NOT NULL DEFAULT 1,
        PointsValueCents INT NOT NULL DEFAULT 1,
        MinimumRedemption INT NOT NULL DEFAULT 100,
        SignupBonus INT NOT NULL DEFAULT 0,
        BirthdayBonus INT NOT NULL DEFAULT 0,
        ReviewBonus INT NOT NULL DEFAULT 0,
        ReferralBonus INT NOT NULL DEFAULT 0,
        PointsExpireMonths INT NULL,
        PointsName NVARCHAR(50) NOT NULL DEFAULT 'Points',
        Currency NVARCHAR(3) NOT NULL DEFAULT 'USD',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UQ_LoyaltyPrograms_Shop UNIQUE (ShopDomain)
    );
    PRINT 'Created table: LoyaltyPrograms';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyTiers')
BEGIN
    CREATE TABLE LoyaltyTiers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LoyaltyProgramId INT NOT NULL,
        Name NVARCHAR(50) NOT NULL,
        MinimumPoints INT NOT NULL,
        PointsMultiplier DECIMAL(5,2) NOT NULL DEFAULT 1.0,
        PercentageDiscount DECIMAL(5,2) NULL,
        FreeShipping BIT NOT NULL DEFAULT 0,
        ExclusiveAccess BIT NOT NULL DEFAULT 0,
        Color NVARCHAR(20) NULL,
        Icon NVARCHAR(50) NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_LoyaltyTiers_Program FOREIGN KEY (LoyaltyProgramId) REFERENCES LoyaltyPrograms(Id) ON DELETE CASCADE
    );
    PRINT 'Created table: LoyaltyTiers';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerLoyalties')
BEGIN
    CREATE TABLE CustomerLoyalties (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        CustomerId INT NOT NULL,
        LoyaltyProgramId INT NOT NULL,
        CurrentTierId INT NULL,
        PointsBalance INT NOT NULL DEFAULT 0,
        LifetimePoints INT NOT NULL DEFAULT 0,
        LifetimeRedeemed INT NOT NULL DEFAULT 0,
        LifetimeSpent DECIMAL(18,4) NOT NULL DEFAULT 0,
        ReferralCode NVARCHAR(20) NULL,
        ReferredById INT NULL,
        Birthday DATE NULL,
        JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastActivityAt DATETIME2 NULL,
        TierUpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_CustomerLoyalties_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerLoyalties_Program FOREIGN KEY (LoyaltyProgramId) REFERENCES LoyaltyPrograms(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CustomerLoyalties_Tier FOREIGN KEY (CurrentTierId) REFERENCES LoyaltyTiers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CustomerLoyalties_ReferredBy FOREIGN KEY (ReferredById) REFERENCES CustomerLoyalties(Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_CustomerLoyalties_Customer_Program UNIQUE (CustomerId, LoyaltyProgramId)
    );
    CREATE INDEX IX_CustomerLoyalties_Shop ON CustomerLoyalties(ShopDomain);
    PRINT 'Created table: CustomerLoyalties';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyPoints')
BEGIN
    CREATE TABLE LoyaltyPoints (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerLoyaltyId INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        Points INT NOT NULL,
        BalanceAfter INT NOT NULL,
        Source NVARCHAR(50) NOT NULL,
        SourceId NVARCHAR(50) NULL,
        Description NVARCHAR(500) NULL,
        ExpiresAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_LoyaltyPoints_CustomerLoyalty FOREIGN KEY (CustomerLoyaltyId) REFERENCES CustomerLoyalties(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_LoyaltyPoints_Customer ON LoyaltyPoints(CustomerLoyaltyId);
    PRINT 'Created table: LoyaltyPoints';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyRewards')
BEGIN
    CREATE TABLE LoyaltyRewards (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LoyaltyProgramId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Type NVARCHAR(20) NOT NULL,
        PointsCost INT NOT NULL,
        Value DECIMAL(18,4) NOT NULL,
        MinimumOrderAmount DECIMAL(18,4) NULL,
        ProductId INT NULL,
        MaxRedemptions INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        StartsAt DATETIME2 NULL,
        EndsAt DATETIME2 NULL,
        ImageUrl NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_LoyaltyRewards_Program FOREIGN KEY (LoyaltyProgramId) REFERENCES LoyaltyPrograms(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LoyaltyRewards_Product FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL
    );
    PRINT 'Created table: LoyaltyRewards';
END
GO

PRINT 'Customer Hub tables migration completed successfully!';
