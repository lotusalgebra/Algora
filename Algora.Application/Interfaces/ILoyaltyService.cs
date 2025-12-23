using Algora.Application.DTOs.CustomerHub;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for managing loyalty programs, points, and rewards.
/// </summary>
public interface ILoyaltyService
{
    // Program management
    Task<LoyaltyProgramDto?> GetProgramAsync(string shopDomain);
    Task<LoyaltyProgramDto> CreateOrUpdateProgramAsync(SaveLoyaltyProgramDto dto);
    Task<bool> ActivateProgramAsync(string shopDomain);
    Task<bool> DeactivateProgramAsync(string shopDomain);

    // Tiers
    Task<IEnumerable<LoyaltyTierDto>> GetTiersAsync(int programId);
    Task<LoyaltyTierDto> CreateTierAsync(CreateLoyaltyTierDto dto);
    Task<LoyaltyTierDto> UpdateTierAsync(int id, UpdateLoyaltyTierDto dto);
    Task<bool> DeleteTierAsync(int id);

    // Rewards
    Task<IEnumerable<LoyaltyRewardDto>> GetRewardsAsync(int programId);
    Task<IEnumerable<LoyaltyRewardDto>> GetActiveRewardsAsync(string shopDomain);
    Task<LoyaltyRewardDto> CreateRewardAsync(CreateLoyaltyRewardDto dto);
    Task<LoyaltyRewardDto> UpdateRewardAsync(int id, UpdateLoyaltyRewardDto dto);
    Task<bool> DeleteRewardAsync(int id);

    // Member operations
    Task<CustomerLoyaltyDto?> GetMemberAsync(int customerId);
    Task<CustomerLoyaltyDto?> GetMemberByReferralCodeAsync(string referralCode);
    Task<CustomerLoyaltyDto> EnrollMemberAsync(EnrollMemberDto dto);
    Task<CustomerLoyaltyDto> UpdateMemberAsync(int customerId, UpdateMemberDto dto);

    // Points operations
    Task<CustomerLoyaltyDto> EarnPointsAsync(int customerId, EarnPointsDto dto);
    Task<RedemptionResultDto> RedeemPointsAsync(int customerId, RedeemPointsDto dto);
    Task<CustomerLoyaltyDto> AdjustPointsAsync(int customerId, AdjustPointsDto dto);
    Task<IEnumerable<LoyaltyPointsDto>> GetPointsHistoryAsync(int customerId, int? limit = null);

    // Automation
    Task ProcessOrderPointsAsync(int orderId);
    Task ProcessReferralBonusAsync(int referrerId, int referredId);
    Task ProcessBirthdayBonusAsync(string shopDomain);
    Task EvaluateTiersAsync(string shopDomain);
    Task ExpirePointsAsync(string shopDomain);

    // Analytics
    Task<LoyaltyAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<CustomerLoyaltyDto>> GetTopMembersAsync(string shopDomain, int count = 10);
}
