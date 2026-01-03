-- Chatbot Bridge Setup Script
-- Creates indexes and optimizations for chatbot-inbox integration

-- Ensure the Conversations table has necessary indexes for escalation queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Conversations_ShopDomain_IsEscalated' AND object_id = OBJECT_ID('Conversations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Conversations_ShopDomain_IsEscalated
    ON Conversations (ShopDomain, IsEscalated)
    INCLUDE (Status, EscalatedAt, CustomerName, CustomerEmail, LastMessageAt)
    WHERE IsEscalated = 1;
    PRINT 'Created index IX_Conversations_ShopDomain_IsEscalated';
END
GO

-- Index for filtering by status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Conversations_ShopDomain_Status' AND object_id = OBJECT_ID('Conversations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Conversations_ShopDomain_Status
    ON Conversations (ShopDomain, Status)
    INCLUDE (CustomerName, CustomerEmail, LastMessageAt, EscalatedAt, IsEscalated);
    PRINT 'Created index IX_Conversations_ShopDomain_Status';
END
GO

-- Index for agent assignment queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Conversations_AssignedAgentEmail' AND object_id = OBJECT_ID('Conversations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Conversations_AssignedAgentEmail
    ON Conversations (AssignedAgentEmail)
    WHERE AssignedAgentEmail IS NOT NULL;
    PRINT 'Created index IX_Conversations_AssignedAgentEmail';
END
GO

-- Index for message polling (get messages after a certain time)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_ConversationId_CreatedAt' AND object_id = OBJECT_ID('Messages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Messages_ConversationId_CreatedAt
    ON Messages (ConversationId, CreatedAt)
    INCLUDE (Content, Role);
    PRINT 'Created index IX_Messages_ConversationId_CreatedAt';
END
GO

-- Add EscalationReason column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Conversations') AND name = 'EscalationReason')
BEGIN
    ALTER TABLE Conversations ADD EscalationReason NVARCHAR(500) NULL;
    PRINT 'Added EscalationReason column to Conversations';
END
GO

-- Add AssignedAgentName column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Conversations') AND name = 'AssignedAgentName')
BEGIN
    ALTER TABLE Conversations ADD AssignedAgentName NVARCHAR(256) NULL;
    PRINT 'Added AssignedAgentName column to Conversations';
END
GO

-- Add AssignedAt column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Conversations') AND name = 'AssignedAt')
BEGIN
    ALTER TABLE Conversations ADD AssignedAt DATETIME2 NULL;
    PRINT 'Added AssignedAt column to Conversations';
END
GO

-- Add ResolvedAt column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Conversations') AND name = 'ResolvedAt')
BEGIN
    ALTER TABLE Conversations ADD ResolvedAt DATETIME2 NULL;
    PRINT 'Added ResolvedAt column to Conversations';
END
GO

-- Add LastMessageAt column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Conversations') AND name = 'LastMessageAt')
BEGIN
    ALTER TABLE Conversations ADD LastMessageAt DATETIME2 NULL;
    PRINT 'Added LastMessageAt column to Conversations';
END
GO

-- Update LastMessageAt for existing conversations
UPDATE c
SET LastMessageAt = (
    SELECT MAX(m.CreatedAt)
    FROM Messages m
    WHERE m.ConversationId = c.Id
)
WHERE c.LastMessageAt IS NULL;
GO

PRINT 'Chatbot Bridge Setup completed successfully';
