using Algora.Application.DTOs.Communication;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for personalizing email/SMS content with dynamic tokens.
/// </summary>
public interface IPersonalizationService
{
    /// <summary>
    /// Get all available personalization tokens.
    /// </summary>
    List<PersonalizationTokenDto> GetAvailableTokens();

    /// <summary>
    /// Replace all tokens in content with actual values.
    /// </summary>
    Task<string> PersonalizeContentAsync(string content, PersonalizationContextDto context);

    /// <summary>
    /// Build a personalization context for an enrollment.
    /// </summary>
    Task<PersonalizationContextDto> BuildContextForEnrollmentAsync(int enrollmentId);

    /// <summary>
    /// Build a personalization context for an abandoned cart.
    /// </summary>
    Task<PersonalizationContextDto> BuildContextForAbandonedCartAsync(
        string shopDomain,
        AbandonedCartTriggerDto cartData);

    /// <summary>
    /// Build a personalization context for a post-purchase email.
    /// </summary>
    Task<PersonalizationContextDto> BuildContextForOrderAsync(
        string shopDomain,
        int orderId);

    /// <summary>
    /// Build a personalization context for a customer.
    /// </summary>
    Task<PersonalizationContextDto> BuildContextForCustomerAsync(
        string shopDomain,
        int customerId);

    /// <summary>
    /// Validate content for missing or invalid tokens.
    /// Returns list of invalid tokens found.
    /// </summary>
    List<string> ValidateTokens(string content);

    /// <summary>
    /// Preview personalized content with sample data.
    /// </summary>
    string PreviewWithSampleData(string content);
}
