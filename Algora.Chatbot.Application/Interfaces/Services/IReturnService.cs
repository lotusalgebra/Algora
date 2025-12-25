using Algora.Chatbot.Application.DTOs;

namespace Algora.Chatbot.Application.Interfaces.Services;

public interface IReturnService
{
    Task<ReturnEligibilityResult> CheckEligibilityAsync(string shopDomain, long orderId, CancellationToken cancellationToken = default);
    Task<ReturnInitiationResult> InitiateReturnAsync(ReturnInitiationRequest request, CancellationToken cancellationToken = default);
    Task<ReturnStatusResult> GetReturnStatusAsync(string shopDomain, string returnNumber, CancellationToken cancellationToken = default);
}
