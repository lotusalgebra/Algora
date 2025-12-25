namespace Algora.Chatbot.Domain.Enums;

public enum ConversationStatus
{
    Active,
    WaitingForCustomer,
    WaitingForAgent,
    Escalated,
    Resolved,
    Abandoned
}
