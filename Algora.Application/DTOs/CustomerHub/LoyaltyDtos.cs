namespace Algora.Application.DTOs.CustomerHub;

// ==================== Loyalty Program DTOs ====================

public record LoyaltyProgramDto(
    int Id,
    string ShopDomain,
    string Name,
    bool IsActive,
    int PointsPerDollar,
    int PointsValueCents,
    int MinimumRedemption,
    int SignupBonus,
    int BirthdayBonus,
    int ReviewBonus,
    int ReferralBonus,
    int? PointsExpireMonths,
    string PointsName,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int TierCount,
    int RewardCount,
    int MemberCount
);

public record SaveLoyaltyProgramDto(
    string ShopDomain,
    string Name,
    int PointsPerDollar = 1,
    int PointsValueCents = 1,
    int MinimumRedemption = 100,
    int SignupBonus = 0,
    int BirthdayBonus = 0,
    int ReviewBonus = 0,
    int ReferralBonus = 0,
    int? PointsExpireMonths = null,
    string PointsName = "Points",
    string Currency = "USD"
);

// ==================== Loyalty Tier DTOs ====================

public record LoyaltyTierDto(
    int Id,
    int LoyaltyProgramId,
    string Name,
    int MinimumPoints,
    decimal PointsMultiplier,
    decimal? PercentageDiscount,
    bool FreeShipping,
    bool ExclusiveAccess,
    string? Color,
    string? Icon,
    int DisplayOrder,
    DateTime CreatedAt,
    int MemberCount
);

public record CreateLoyaltyTierDto(
    int LoyaltyProgramId,
    string Name,
    int MinimumPoints,
    decimal PointsMultiplier = 1.0m,
    decimal? PercentageDiscount = null,
    bool FreeShipping = false,
    bool ExclusiveAccess = false,
    string? Color = null,
    string? Icon = null,
    int DisplayOrder = 0
);

public record UpdateLoyaltyTierDto(
    string? Name = null,
    int? MinimumPoints = null,
    decimal? PointsMultiplier = null,
    decimal? PercentageDiscount = null,
    bool? FreeShipping = null,
    bool? ExclusiveAccess = null,
    string? Color = null,
    string? Icon = null,
    int? DisplayOrder = null
);

// ==================== Loyalty Reward DTOs ====================

public record LoyaltyRewardDto(
    int Id,
    int LoyaltyProgramId,
    string Name,
    string? Description,
    string Type,
    int PointsCost,
    decimal Value,
    decimal? MinimumOrderAmount,
    int? ProductId,
    string? ProductTitle,
    int? MaxRedemptions,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? ImageUrl,
    DateTime CreatedAt,
    int RedemptionCount
);

public record CreateLoyaltyRewardDto(
    int LoyaltyProgramId,
    string Name,
    string? Description,
    string Type,
    int PointsCost,
    decimal Value,
    decimal? MinimumOrderAmount = null,
    int? ProductId = null,
    int? MaxRedemptions = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? ImageUrl = null
);

public record UpdateLoyaltyRewardDto(
    string? Name = null,
    string? Description = null,
    string? Type = null,
    int? PointsCost = null,
    decimal? Value = null,
    decimal? MinimumOrderAmount = null,
    int? ProductId = null,
    int? MaxRedemptions = null,
    bool? IsActive = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? ImageUrl = null
);

// ==================== Customer Loyalty DTOs ====================

public record CustomerLoyaltyDto(
    int Id,
    string ShopDomain,
    int CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    int LoyaltyProgramId,
    int? CurrentTierId,
    string? CurrentTierName,
    int PointsBalance,
    int LifetimePoints,
    int LifetimeRedeemed,
    decimal LifetimeSpent,
    string? ReferralCode,
    int? ReferredById,
    DateTime? Birthday,
    DateTime JoinedAt,
    DateTime? LastActivityAt,
    DateTime? TierUpdatedAt,
    int? PointsToNextTier,
    string? NextTierName
);

public record EnrollMemberDto(
    string ShopDomain,
    int CustomerId,
    string? ReferralCode = null,
    DateTime? Birthday = null
);

public record UpdateMemberDto(
    DateTime? Birthday = null
);

// ==================== Points DTOs ====================

public record LoyaltyPointsDto(
    int Id,
    int CustomerLoyaltyId,
    string Type,
    int Points,
    int BalanceAfter,
    string Source,
    string? SourceId,
    string? Description,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record EarnPointsDto(
    string Source,
    string? SourceId,
    int? Points = null,
    decimal? OrderTotal = null,
    string? Description = null
);

public record RedeemPointsDto(
    int RewardId,
    int? Quantity = 1
);

public record AdjustPointsDto(
    int Points,
    string Reason
);

public record RedemptionResultDto(
    bool Success,
    string? ErrorMessage,
    int PointsDeducted,
    int NewBalance,
    string? DiscountCode,
    decimal? DiscountValue,
    string? RewardType
);

// ==================== Analytics DTOs ====================

public record LoyaltyAnalyticsDto(
    int TotalMembers,
    int ActiveMembers,
    int NewMembersThisPeriod,
    long TotalPointsIssued,
    long TotalPointsRedeemed,
    long TotalPointsExpired,
    decimal TotalRedemptionValue,
    decimal AveragePointsPerMember,
    Dictionary<string, int> MembersByTier,
    Dictionary<string, int> PointsBySource,
    IEnumerable<DailyPointsDto> DailyPoints
);

public record DailyPointsDto(
    DateTime Date,
    int PointsEarned,
    int PointsRedeemed,
    int NewMembers
);
