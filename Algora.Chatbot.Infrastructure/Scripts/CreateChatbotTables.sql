-- Chatbot Tables Setup Script
-- Creates all tables for the Algora.Chatbot module

USE AlgoraChatbot;
GO

-- Plans table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Plans') AND type = 'U')
BEGIN
    CREATE TABLE Plans (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        MonthlyPrice DECIMAL(10,2) NOT NULL,
        TrialDays INT NOT NULL DEFAULT 14,
        ConversationsPerMonth INT NOT NULL DEFAULT 100,
        MessagesPerConversation INT NOT NULL DEFAULT 50,
        KnowledgeArticles INT NOT NULL DEFAULT 10,
        HasMultipleProviders BIT NOT NULL DEFAULT 0,
        HasAdvancedAnalytics BIT NOT NULL DEFAULT 0,
        HasCustomBranding BIT NOT NULL DEFAULT 0,
        HasPrioritySupport BIT NOT NULL DEFAULT 0,
        HasApiAccess BIT NOT NULL DEFAULT 0,
        HasWebhookIntegrations BIT NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_Plans_Name ON Plans(Name);
    PRINT 'Created Plans table';
END
GO

-- Shops table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Shops') AND type = 'U')
BEGIN
    CREATE TABLE Shops (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Domain NVARCHAR(255) NOT NULL,
        OfflineAccessToken NVARCHAR(500) NULL,
        ShopName NVARCHAR(255) NULL,
        Email NVARCHAR(255) NULL,
        Currency NVARCHAR(10) NULL,
        Timezone NVARCHAR(100) NULL,
        Country NVARCHAR(10) NULL,
        PlanName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        InstalledAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UninstalledAt DATETIME2 NULL,
        LastSyncedAt DATETIME2 NULL
    );
    CREATE UNIQUE INDEX IX_Shops_Domain ON Shops(Domain);
    PRINT 'Created Shops table';
END
GO

-- WidgetConfigurations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'WidgetConfigurations') AND type = 'U')
BEGIN
    CREATE TABLE WidgetConfigurations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        Position NVARCHAR(50) NOT NULL DEFAULT 'bottom-right',
        OffsetX INT NOT NULL DEFAULT 20,
        OffsetY INT NOT NULL DEFAULT 20,
        TriggerStyle NVARCHAR(50) NOT NULL DEFAULT 'bubble',
        PrimaryColor NVARCHAR(20) NOT NULL DEFAULT '#7c3aed',
        SecondaryColor NVARCHAR(20) NOT NULL DEFAULT '#ffffff',
        TextColor NVARCHAR(20) NOT NULL DEFAULT '#333333',
        HeaderBackgroundColor NVARCHAR(20) NOT NULL DEFAULT '#7c3aed',
        HeaderTextColor NVARCHAR(20) NOT NULL DEFAULT '#ffffff',
        LogoUrl NVARCHAR(500) NULL,
        AvatarUrl NVARCHAR(500) NULL,
        HeaderTitle NVARCHAR(100) NOT NULL DEFAULT 'Chat with us',
        TriggerText NVARCHAR(100) NOT NULL DEFAULT 'Need help?',
        AutoOpenOnFirstVisit BIT NOT NULL DEFAULT 0,
        AutoOpenDelaySeconds INT NOT NULL DEFAULT 5,
        ShowTypingIndicator BIT NOT NULL DEFAULT 1,
        ShowTimestamps BIT NOT NULL DEFAULT 1,
        EnableSoundNotifications BIT NOT NULL DEFAULT 1,
        PlaceholderText NVARCHAR(200) NULL DEFAULT 'Type your message...',
        EnableFileUpload BIT NOT NULL DEFAULT 0,
        EnableEmoji BIT NOT NULL DEFAULT 1,
        CustomCss NVARCHAR(MAX) NULL,
        ShowPoweredBy BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE UNIQUE INDEX IX_WidgetConfigurations_ShopDomain ON WidgetConfigurations(ShopDomain);
    PRINT 'Created WidgetConfigurations table';
END
GO

-- ChatbotSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'ChatbotSettings') AND type = 'U')
BEGIN
    CREATE TABLE ChatbotSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        PreferredAiProvider NVARCHAR(50) NOT NULL DEFAULT 'openai',
        FallbackAiProvider NVARCHAR(50) NULL DEFAULT 'anthropic',
        Temperature FLOAT NOT NULL DEFAULT 0.7,
        MaxTokens INT NOT NULL DEFAULT 500,
        BotName NVARCHAR(100) NOT NULL DEFAULT 'Support Assistant',
        WelcomeMessage NVARCHAR(1000) NULL DEFAULT 'Hi! How can I help you today?',
        CustomInstructions NVARCHAR(MAX) NULL,
        Tone NVARCHAR(50) NOT NULL DEFAULT 'professional',
        EnableOrderTracking BIT NOT NULL DEFAULT 1,
        EnableProductRecommendations BIT NOT NULL DEFAULT 1,
        EnableReturns BIT NOT NULL DEFAULT 1,
        EnablePolicyLookup BIT NOT NULL DEFAULT 1,
        EnableHumanEscalation BIT NOT NULL DEFAULT 1,
        EscalateAfterMessages INT NOT NULL DEFAULT 10,
        ConfidenceThreshold DECIMAL(3,2) NOT NULL DEFAULT 0.60,
        EscalationEmail NVARCHAR(255) NULL,
        EscalationWebhookUrl NVARCHAR(500) NULL,
        OperatingHoursJson NVARCHAR(MAX) NULL,
        OutOfHoursMessage NVARCHAR(500) NULL,
        WidgetConfigurationId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_ChatbotSettings_WidgetConfiguration FOREIGN KEY (WidgetConfigurationId) REFERENCES WidgetConfigurations(Id)
    );
    CREATE UNIQUE INDEX IX_ChatbotSettings_ShopDomain ON ChatbotSettings(ShopDomain);
    PRINT 'Created ChatbotSettings table';
END
GO

-- Licenses table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Licenses') AND type = 'U')
BEGIN
    CREATE TABLE Licenses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        PlanId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'trial',
        ShopifyChargeId NVARCHAR(100) NULL,
        StartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ExpiryDate DATETIME2 NULL,
        TrialDaysRemaining INT NOT NULL DEFAULT 14,
        ConversationsThisMonth INT NOT NULL DEFAULT 0,
        MessagesThisMonth INT NOT NULL DEFAULT 0,
        UsagePeriodStart DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_Licenses_Plan FOREIGN KEY (PlanId) REFERENCES Plans(Id)
    );
    CREATE UNIQUE INDEX IX_Licenses_ShopDomain ON Licenses(ShopDomain);
    PRINT 'Created Licenses table';
END
GO

-- Conversations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Conversations') AND type = 'U')
BEGIN
    CREATE TABLE Conversations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        SessionId NVARCHAR(100) NOT NULL,
        VisitorId NVARCHAR(100) NULL,
        ShopifyCustomerId BIGINT NULL,
        CustomerEmail NVARCHAR(255) NULL,
        CustomerName NVARCHAR(255) NULL,
        CurrentPageUrl NVARCHAR(2000) NULL,
        ReferrerUrl NVARCHAR(2000) NULL,
        UserAgent NVARCHAR(500) NULL,
        IpAddress NVARCHAR(50) NULL,
        Status INT NOT NULL DEFAULT 0,
        PrimaryIntent NVARCHAR(100) NULL,
        OverallSentiment DECIMAL(3,2) NULL,
        RelatedOrderId BIGINT NULL,
        RelatedProductId BIGINT NULL,
        ReturnRequestId INT NULL,
        IsEscalated BIT NOT NULL DEFAULT 0,
        EscalationReason NVARCHAR(500) NULL,
        EscalatedAt DATETIME2 NULL,
        AssignedAgentEmail NVARCHAR(255) NULL,
        AssignedAgentName NVARCHAR(256) NULL,
        AssignedAt DATETIME2 NULL,
        Rating INT NULL,
        FeedbackComment NVARCHAR(1000) NULL,
        WasHelpful BIT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastMessageAt DATETIME2 NULL,
        ResolvedAt DATETIME2 NULL
    );
    CREATE INDEX IX_Conversations_ShopDomain ON Conversations(ShopDomain);
    CREATE INDEX IX_Conversations_SessionId ON Conversations(SessionId);
    CREATE INDEX IX_Conversations_Status ON Conversations(Status);
    CREATE INDEX IX_Conversations_CreatedAt ON Conversations(CreatedAt);
    CREATE INDEX IX_Conversations_ShopDomain_SessionId ON Conversations(ShopDomain, SessionId);
    CREATE INDEX IX_Conversations_ShopDomain_IsEscalated ON Conversations(ShopDomain, IsEscalated) INCLUDE (Status, EscalatedAt, CustomerName, CustomerEmail, LastMessageAt) WHERE IsEscalated = 1;
    CREATE INDEX IX_Conversations_ShopDomain_Status ON Conversations(ShopDomain, Status) INCLUDE (CustomerName, CustomerEmail, LastMessageAt, EscalatedAt, IsEscalated);
    PRINT 'Created Conversations table';
END
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Index for agent assignment queries (filtered index requires quoted identifier)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Conversations_AssignedAgentEmail' AND object_id = OBJECT_ID('Conversations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Conversations_AssignedAgentEmail
    ON Conversations (AssignedAgentEmail)
    WHERE AssignedAgentEmail IS NOT NULL;
    PRINT 'Created index IX_Conversations_AssignedAgentEmail';
END
GO

-- Messages table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Messages') AND type = 'U')
BEGIN
    CREATE TABLE Messages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ConversationId INT NOT NULL,
        Role INT NOT NULL DEFAULT 0,
        Content NVARCHAR(MAX) NOT NULL,
        DetectedIntent NVARCHAR(100) NULL,
        IntentConfidence DECIMAL(3,2) NULL,
        Sentiment DECIMAL(3,2) NULL,
        AiProvider NVARCHAR(50) NULL,
        AiModel NVARCHAR(100) NULL,
        TokensUsed INT NULL,
        AiCost DECIMAL(10,6) NULL,
        SuggestedActionsJson NVARCHAR(MAX) NULL,
        AttachmentsJson NVARCHAR(MAX) NULL,
        IsDelivered BIT NOT NULL DEFAULT 1,
        IsRead BIT NOT NULL DEFAULT 0,
        ReadAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
    CREATE INDEX IX_Messages_CreatedAt ON Messages(CreatedAt);
    CREATE INDEX IX_Messages_ConversationId_CreatedAt ON Messages(ConversationId, CreatedAt) INCLUDE (Content, Role);
    PRINT 'Created Messages table';
END
GO

-- KnowledgeArticles table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'KnowledgeArticles') AND type = 'U')
BEGIN
    CREATE TABLE KnowledgeArticles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(100) NULL,
        Tags NVARCHAR(500) NULL,
        KeyPhrases NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Priority INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE INDEX IX_KnowledgeArticles_ShopDomain ON KnowledgeArticles(ShopDomain);
    CREATE INDEX IX_KnowledgeArticles_ShopDomain_Category ON KnowledgeArticles(ShopDomain, Category);
    PRINT 'Created KnowledgeArticles table';
END
GO

-- Policies table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Policies') AND type = 'U')
BEGIN
    CREATE TABLE Policies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        PolicyType NVARCHAR(50) NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Summary NVARCHAR(2000) NULL,
        ReturnWindowDays INT NULL,
        FreeShippingThreshold DECIMAL(10,2) NULL,
        ShippingTimeframe NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE INDEX IX_Policies_ShopDomain ON Policies(ShopDomain);
    CREATE INDEX IX_Policies_ShopDomain_PolicyType ON Policies(ShopDomain, PolicyType);
    PRINT 'Created Policies table';
END
GO

-- ConversationAnalytics table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'ConversationAnalytics') AND type = 'U')
BEGIN
    CREATE TABLE ConversationAnalytics (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopDomain NVARCHAR(255) NOT NULL,
        SnapshotDate DATETIME2 NOT NULL,
        PeriodType NVARCHAR(20) NOT NULL DEFAULT 'daily',
        TotalConversations INT NOT NULL DEFAULT 0,
        ResolvedConversations INT NOT NULL DEFAULT 0,
        EscalatedConversations INT NOT NULL DEFAULT 0,
        AbandonedConversations INT NOT NULL DEFAULT 0,
        AvgResponseTimeSeconds FLOAT NOT NULL DEFAULT 0,
        AvgConversationDurationMinutes FLOAT NOT NULL DEFAULT 0,
        AvgMessagesPerConversation FLOAT NOT NULL DEFAULT 0,
        AvgRating FLOAT NOT NULL DEFAULT 0,
        TotalRatings INT NOT NULL DEFAULT 0,
        HelpfulPercentage FLOAT NOT NULL DEFAULT 0,
        IntentDistributionJson NVARCHAR(MAX) NULL,
        TotalAiCost DECIMAL(10,4) NOT NULL DEFAULT 0,
        TotalTokensUsed INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_ConversationAnalytics_ShopDomain ON ConversationAnalytics(ShopDomain);
    CREATE INDEX IX_ConversationAnalytics_ShopDomain_SnapshotDate_PeriodType ON ConversationAnalytics(ShopDomain, SnapshotDate, PeriodType);
    PRINT 'Created ConversationAnalytics table';
END
GO

-- Insert default plans
IF NOT EXISTS (SELECT * FROM Plans WHERE Name = 'Free')
BEGIN
    INSERT INTO Plans (Name, Description, MonthlyPrice, TrialDays, ConversationsPerMonth, MessagesPerConversation, KnowledgeArticles, HasMultipleProviders, HasAdvancedAnalytics, HasCustomBranding, HasPrioritySupport, HasApiAccess, HasWebhookIntegrations, SortOrder, IsActive)
    VALUES
        ('Free', 'Get started with basic chatbot support', 0, 0, 50, 20, 5, 0, 0, 0, 0, 0, 0, 1, 1),
        ('Basic', 'Essential support for growing stores', 29, 14, 500, 50, 25, 0, 0, 1, 0, 0, 0, 2, 1),
        ('Pro', 'Advanced features for busy stores', 79, 14, 2000, 100, 100, 1, 1, 1, 1, 0, 0, 3, 1),
        ('Enterprise', 'Unlimited support with full customization', 199, 14, 999999, 999999, 999999, 1, 1, 1, 1, 1, 1, 4, 1);
    PRINT 'Inserted default plans';
END
GO

PRINT 'Chatbot tables setup completed successfully';
