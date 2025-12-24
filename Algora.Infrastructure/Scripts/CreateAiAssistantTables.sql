-- ==================== AI ASSISTANT TABLES ====================
-- Run this script to create the AI Assistant feature tables

-- Chatbot conversations for customer self-service
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatbotConversations')
BEGIN
    CREATE TABLE ChatbotConversations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        SessionId NVARCHAR(100) NOT NULL,
        CustomerId INT NULL,
        CustomerEmail NVARCHAR(200) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'active', -- active, resolved, escalated
        Topic NVARCHAR(100) NULL, -- order_status, return, product_info, etc.
        RelatedOrderId INT NULL,
        WasHelpful BIT NULL,
        EscalatedToAgentAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        EndedAt DATETIME2 NULL,
        CONSTRAINT FK_ChatbotConversations_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_ChatbotConversations_Order FOREIGN KEY (RelatedOrderId) REFERENCES Orders(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_ChatbotConversations_Shop ON ChatbotConversations(ShopDomain);
    CREATE INDEX IX_ChatbotConversations_Session ON ChatbotConversations(SessionId);

    PRINT 'Created ChatbotConversations table';
END
ELSE
BEGIN
    PRINT 'ChatbotConversations table already exists';
END
GO

-- Individual chatbot messages
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatbotMessages')
BEGIN
    CREATE TABLE ChatbotMessages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ConversationId INT NOT NULL,
        Role NVARCHAR(20) NOT NULL, -- user, assistant, system
        Content NVARCHAR(MAX) NOT NULL,
        Intent NVARCHAR(50) NULL, -- Detected intent (order_status, return_policy, etc.)
        Confidence DECIMAL(5,2) NULL,
        SuggestedActions NVARCHAR(500) NULL, -- JSON array of action buttons
        TokensUsed INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ChatbotMessages_Conversation FOREIGN KEY (ConversationId) REFERENCES ChatbotConversations(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ChatbotMessages_Conversation ON ChatbotMessages(ConversationId);

    PRINT 'Created ChatbotMessages table';
END
ELSE
BEGIN
    PRINT 'ChatbotMessages table already exists';
END
GO

-- Cached SEO data for products
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductSeoData')
BEGIN
    CREATE TABLE ProductSeoData (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        MetaTitle NVARCHAR(70) NULL,
        MetaDescription NVARCHAR(160) NULL,
        Keywords NVARCHAR(500) NULL,
        FocusKeyword NVARCHAR(100) NULL,
        SeoScore INT NULL, -- 0-100
        Provider NVARCHAR(50) NULL,
        GeneratedAt DATETIME2 NULL,
        ApprovedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_ProductSeoData_Product FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_ProductSeoData_Product UNIQUE (ProductId)
    );

    PRINT 'Created ProductSeoData table';
END
ELSE
BEGIN
    PRINT 'ProductSeoData table already exists';
END
GO

-- Pricing suggestions history
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PricingSuggestions')
BEGIN
    CREATE TABLE PricingSuggestions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(200) NOT NULL,
        ProductId INT NOT NULL,
        CurrentPrice DECIMAL(18,4) NOT NULL,
        SuggestedPrice DECIMAL(18,4) NOT NULL,
        MinPrice DECIMAL(18,4) NULL,
        MaxPrice DECIMAL(18,4) NULL,
        PriceChange DECIMAL(18,4) NOT NULL, -- Suggested - Current
        ChangePercent DECIMAL(5,2) NOT NULL,
        Reasoning NVARCHAR(1000) NULL,
        Factors NVARCHAR(MAX) NULL, -- JSON: competitor prices, demand, margin
        Confidence DECIMAL(5,2) NOT NULL,
        WasApplied BIT NOT NULL DEFAULT 0,
        AppliedAt DATETIME2 NULL,
        Provider NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PricingSuggestions_Product FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_PricingSuggestions_Product ON PricingSuggestions(ProductId);

    PRINT 'Created PricingSuggestions table';
END
ELSE
BEGIN
    PRINT 'PricingSuggestions table already exists';
END
GO

PRINT 'AI Assistant tables migration complete';
