using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for managing loyalty programs, points, and rewards.
/// </summary>
public class LoyaltyService : ILoyaltyService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LoyaltyService> _logger;

    public LoyaltyService(AppDbContext db, ILogger<LoyaltyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Program Management

    public async Task<LoyaltyProgramDto?> GetProgramAsync(string shopDomain)
    {
        var program = await _db.LoyaltyPrograms
            .Include(p => p.Tiers)
            .Include(p => p.Rewards)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.ShopDomain == shopDomain);

        return program != null ? MapProgramToDto(program) : null;
    }

    public async Task<LoyaltyProgramDto> CreateOrUpdateProgramAsync(SaveLoyaltyProgramDto dto)
    {
        var program = await _db.LoyaltyPrograms.FirstOrDefaultAsync(p => p.ShopDomain == dto.ShopDomain);

        if (program == null)
        {
            program = new LoyaltyProgram
            {
                ShopDomain = dto.ShopDomain,
                CreatedAt = DateTime.UtcNow
            };
            _db.LoyaltyPrograms.Add(program);
        }

        program.Name = dto.Name;
        program.PointsPerDollar = dto.PointsPerDollar;
        program.PointsValueCents = dto.PointsValueCents;
        program.MinimumRedemption = dto.MinimumRedemption;
        program.SignupBonus = dto.SignupBonus;
        program.BirthdayBonus = dto.BirthdayBonus;
        program.ReviewBonus = dto.ReviewBonus;
        program.ReferralBonus = dto.ReferralBonus;
        program.PointsExpireMonths = dto.PointsExpireMonths;
        program.PointsName = dto.PointsName;
        program.Currency = dto.Currency;
        program.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(program).Collection(p => p.Tiers).LoadAsync();
        await _db.Entry(program).Collection(p => p.Rewards).LoadAsync();
        await _db.Entry(program).Collection(p => p.Members).LoadAsync();

        return MapProgramToDto(program);
    }

    public async Task<bool> ActivateProgramAsync(string shopDomain)
    {
        var program = await _db.LoyaltyPrograms.FirstOrDefaultAsync(p => p.ShopDomain == shopDomain);
        if (program == null) return false;

        program.IsActive = true;
        program.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateProgramAsync(string shopDomain)
    {
        var program = await _db.LoyaltyPrograms.FirstOrDefaultAsync(p => p.ShopDomain == shopDomain);
        if (program == null) return false;

        program.IsActive = false;
        program.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Tiers

    public async Task<IEnumerable<LoyaltyTierDto>> GetTiersAsync(int programId)
    {
        var tiers = await _db.LoyaltyTiers
            .Where(t => t.LoyaltyProgramId == programId)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        var memberCounts = await _db.CustomerLoyalties
            .Where(cl => cl.LoyaltyProgramId == programId && cl.CurrentTierId != null)
            .GroupBy(cl => cl.CurrentTierId)
            .Select(g => new { TierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TierId!.Value, x => x.Count);

        return tiers.Select(t => MapTierToDto(t, memberCounts.GetValueOrDefault(t.Id)));
    }

    public async Task<LoyaltyTierDto> CreateTierAsync(CreateLoyaltyTierDto dto)
    {
        var tier = new LoyaltyTier
        {
            LoyaltyProgramId = dto.LoyaltyProgramId,
            Name = dto.Name,
            MinimumPoints = dto.MinimumPoints,
            PointsMultiplier = dto.PointsMultiplier,
            PercentageDiscount = dto.PercentageDiscount,
            FreeShipping = dto.FreeShipping,
            ExclusiveAccess = dto.ExclusiveAccess,
            Color = dto.Color,
            Icon = dto.Icon,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.LoyaltyTiers.Add(tier);
        await _db.SaveChangesAsync();

        return MapTierToDto(tier, 0);
    }

    public async Task<LoyaltyTierDto> UpdateTierAsync(int id, UpdateLoyaltyTierDto dto)
    {
        var tier = await _db.LoyaltyTiers.FindAsync(id)
            ?? throw new InvalidOperationException($"Tier {id} not found");

        if (dto.Name != null) tier.Name = dto.Name;
        if (dto.MinimumPoints.HasValue) tier.MinimumPoints = dto.MinimumPoints.Value;
        if (dto.PointsMultiplier.HasValue) tier.PointsMultiplier = dto.PointsMultiplier.Value;
        if (dto.PercentageDiscount.HasValue) tier.PercentageDiscount = dto.PercentageDiscount;
        if (dto.FreeShipping.HasValue) tier.FreeShipping = dto.FreeShipping.Value;
        if (dto.ExclusiveAccess.HasValue) tier.ExclusiveAccess = dto.ExclusiveAccess.Value;
        if (dto.Color != null) tier.Color = dto.Color;
        if (dto.Icon != null) tier.Icon = dto.Icon;
        if (dto.DisplayOrder.HasValue) tier.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();

        var memberCount = await _db.CustomerLoyalties.CountAsync(cl => cl.CurrentTierId == id);
        return MapTierToDto(tier, memberCount);
    }

    public async Task<bool> DeleteTierAsync(int id)
    {
        var tier = await _db.LoyaltyTiers.FindAsync(id);
        if (tier == null) return false;

        // Clear tier from members before deleting
        var membersInTier = await _db.CustomerLoyalties.Where(cl => cl.CurrentTierId == id).ToListAsync();
        foreach (var m in membersInTier) m.CurrentTierId = null;

        _db.LoyaltyTiers.Remove(tier);
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Rewards

    public async Task<IEnumerable<LoyaltyRewardDto>> GetRewardsAsync(int programId)
    {
        var rewards = await _db.LoyaltyRewards
            .Include(r => r.Product)
            .Where(r => r.LoyaltyProgramId == programId)
            .OrderBy(r => r.PointsCost)
            .ToListAsync();

        return rewards.Select(r => MapRewardToDto(r, 0));
    }

    public async Task<IEnumerable<LoyaltyRewardDto>> GetActiveRewardsAsync(string shopDomain)
    {
        var now = DateTime.UtcNow;
        var rewards = await _db.LoyaltyRewards
            .Include(r => r.LoyaltyProgram)
            .Include(r => r.Product)
            .Where(r => r.LoyaltyProgram.ShopDomain == shopDomain &&
                        r.IsActive &&
                        (r.StartsAt == null || r.StartsAt <= now) &&
                        (r.EndsAt == null || r.EndsAt >= now))
            .OrderBy(r => r.PointsCost)
            .ToListAsync();

        return rewards.Select(r => MapRewardToDto(r, 0));
    }

    public async Task<LoyaltyRewardDto> CreateRewardAsync(CreateLoyaltyRewardDto dto)
    {
        var reward = new LoyaltyReward
        {
            LoyaltyProgramId = dto.LoyaltyProgramId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            PointsCost = dto.PointsCost,
            Value = dto.Value,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            ProductId = dto.ProductId,
            MaxRedemptions = dto.MaxRedemptions,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            ImageUrl = dto.ImageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.LoyaltyRewards.Add(reward);
        await _db.SaveChangesAsync();

        return MapRewardToDto(reward, 0);
    }

    public async Task<LoyaltyRewardDto> UpdateRewardAsync(int id, UpdateLoyaltyRewardDto dto)
    {
        var reward = await _db.LoyaltyRewards.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Reward {id} not found");

        if (dto.Name != null) reward.Name = dto.Name;
        if (dto.Description != null) reward.Description = dto.Description;
        if (dto.Type != null) reward.Type = dto.Type;
        if (dto.PointsCost.HasValue) reward.PointsCost = dto.PointsCost.Value;
        if (dto.Value.HasValue) reward.Value = dto.Value.Value;
        if (dto.MinimumOrderAmount.HasValue) reward.MinimumOrderAmount = dto.MinimumOrderAmount;
        if (dto.ProductId.HasValue) reward.ProductId = dto.ProductId;
        if (dto.MaxRedemptions.HasValue) reward.MaxRedemptions = dto.MaxRedemptions;
        if (dto.IsActive.HasValue) reward.IsActive = dto.IsActive.Value;
        if (dto.StartsAt.HasValue) reward.StartsAt = dto.StartsAt;
        if (dto.EndsAt.HasValue) reward.EndsAt = dto.EndsAt;
        if (dto.ImageUrl != null) reward.ImageUrl = dto.ImageUrl;

        await _db.SaveChangesAsync();
        return MapRewardToDto(reward, 0);
    }

    public async Task<bool> DeleteRewardAsync(int id)
    {
        var reward = await _db.LoyaltyRewards.FindAsync(id);
        if (reward == null) return false;

        _db.LoyaltyRewards.Remove(reward);
        await _db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Member Operations

    public async Task<CustomerLoyaltyDto?> GetMemberAsync(int customerId)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId);

        return member != null ? MapMemberToDto(member) : null;
    }

    public async Task<CustomerLoyaltyDto?> GetMemberByReferralCodeAsync(string referralCode)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.ReferralCode == referralCode);

        return member != null ? MapMemberToDto(member) : null;
    }

    public async Task<CustomerLoyaltyDto> EnrollMemberAsync(EnrollMemberDto dto)
    {
        var program = await _db.LoyaltyPrograms
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.ShopDomain == dto.ShopDomain)
            ?? throw new InvalidOperationException($"Loyalty program not found for {dto.ShopDomain}");

        var customer = await _db.Customers.FindAsync(dto.CustomerId)
            ?? throw new InvalidOperationException($"Customer {dto.CustomerId} not found");

        // Check if already enrolled
        var existing = await _db.CustomerLoyalties.FirstOrDefaultAsync(
            cl => cl.CustomerId == dto.CustomerId && cl.LoyaltyProgramId == program.Id);
        if (existing != null)
            throw new InvalidOperationException("Customer is already enrolled in the loyalty program");

        var member = new CustomerLoyalty
        {
            ShopDomain = dto.ShopDomain,
            CustomerId = dto.CustomerId,
            LoyaltyProgramId = program.Id,
            ReferralCode = GenerateReferralCode(),
            Birthday = dto.Birthday,
            JoinedAt = DateTime.UtcNow
        };

        // Handle referral
        if (!string.IsNullOrEmpty(dto.ReferralCode))
        {
            var referrer = await _db.CustomerLoyalties.FirstOrDefaultAsync(cl => cl.ReferralCode == dto.ReferralCode);
            if (referrer != null)
                member.ReferredById = referrer.Id;
        }

        _db.CustomerLoyalties.Add(member);
        await _db.SaveChangesAsync();

        // Award signup bonus
        if (program.SignupBonus > 0)
        {
            await AddPointsAsync(member, program.SignupBonus, "earn", "signup", null, "Welcome bonus");
        }

        await _db.Entry(member).Reference(m => m.Customer).LoadAsync();
        await _db.Entry(member).Reference(m => m.LoyaltyProgram).LoadAsync();

        _logger.LogInformation("Enrolled customer {CustomerId} in loyalty program for {ShopDomain}", dto.CustomerId, dto.ShopDomain);
        return MapMemberToDto(member);
    }

    public async Task<CustomerLoyaltyDto> UpdateMemberAsync(int customerId, UpdateMemberDto dto)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Member not found for customer {customerId}");

        if (dto.Birthday.HasValue) member.Birthday = dto.Birthday.Value;

        await _db.SaveChangesAsync();
        return MapMemberToDto(member);
    }

    #endregion

    #region Points Operations

    public async Task<CustomerLoyaltyDto> EarnPointsAsync(int customerId, EarnPointsDto dto)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Member not found for customer {customerId}");

        int points = dto.Points ?? 0;
        if (dto.OrderTotal.HasValue)
        {
            var basePoints = (int)(dto.OrderTotal.Value * member.LoyaltyProgram.PointsPerDollar);
            var multiplier = member.CurrentTier?.PointsMultiplier ?? 1.0m;
            points = (int)(basePoints * multiplier);
        }

        if (points > 0)
        {
            await AddPointsAsync(member, points, "earn", dto.Source, dto.SourceId, dto.Description);
            await EvaluateMemberTierAsync(member);
        }

        return MapMemberToDto(member);
    }

    public async Task<RedemptionResultDto> RedeemPointsAsync(int customerId, RedeemPointsDto dto)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.LoyaltyProgram)
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Member not found for customer {customerId}");

        var reward = await _db.LoyaltyRewards.FindAsync(dto.RewardId)
            ?? throw new InvalidOperationException($"Reward {dto.RewardId} not found");

        var quantity = dto.Quantity ?? 1;
        var totalCost = reward.PointsCost * quantity;

        if (member.PointsBalance < totalCost)
        {
            return new RedemptionResultDto(
                false, $"Insufficient points. Required: {totalCost}, Available: {member.PointsBalance}",
                0, member.PointsBalance, null, null, null);
        }

        if (totalCost < member.LoyaltyProgram.MinimumRedemption)
        {
            return new RedemptionResultDto(
                false, $"Minimum redemption is {member.LoyaltyProgram.MinimumRedemption} points",
                0, member.PointsBalance, null, null, null);
        }

        await AddPointsAsync(member, -totalCost, "redeem", "redemption", dto.RewardId.ToString(), $"Redeemed: {reward.Name}");

        // TODO: Generate discount code if applicable
        string? discountCode = reward.Type.Contains("discount") ? $"LOYALTY-{Guid.NewGuid():N}"[..16].ToUpper() : null;

        return new RedemptionResultDto(
            true, null, totalCost, member.PointsBalance, discountCode, reward.Value, reward.Type);
    }

    public async Task<CustomerLoyaltyDto> AdjustPointsAsync(int customerId, AdjustPointsDto dto)
    {
        var member = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Member not found for customer {customerId}");

        await AddPointsAsync(member, dto.Points, "adjust", "manual", null, dto.Reason);

        if (dto.Points > 0)
            await EvaluateMemberTierAsync(member);

        return MapMemberToDto(member);
    }

    public async Task<IEnumerable<LoyaltyPointsDto>> GetPointsHistoryAsync(int customerId, int? limit = null)
    {
        var member = await _db.CustomerLoyalties.FirstOrDefaultAsync(cl => cl.CustomerId == customerId);
        if (member == null) return Enumerable.Empty<LoyaltyPointsDto>();

        var query = _db.LoyaltyPoints
            .Where(lp => lp.CustomerLoyaltyId == member.Id)
            .OrderByDescending(lp => lp.CreatedAt);

        if (limit.HasValue)
            query = (IOrderedQueryable<LoyaltyPoints>)query.Take(limit.Value);

        var points = await query.ToListAsync();
        return points.Select(MapPointsToDto);
    }

    #endregion

    #region Automation

    public async Task ProcessOrderPointsAsync(int orderId)
    {
        var order = await _db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order?.Customer == null) return;

        var member = await _db.CustomerLoyalties
            .Include(cl => cl.LoyaltyProgram)
            .Include(cl => cl.CurrentTier)
            .FirstOrDefaultAsync(cl => cl.CustomerId == order.Customer.Id);

        if (member == null || !member.LoyaltyProgram.IsActive) return;

        var basePoints = (int)(order.GrandTotal * member.LoyaltyProgram.PointsPerDollar);
        var multiplier = member.CurrentTier?.PointsMultiplier ?? 1.0m;
        var points = (int)(basePoints * multiplier);

        await AddPointsAsync(member, points, "earn", "order", orderId.ToString(), $"Order {order.OrderNumber}");
        member.LifetimeSpent += order.GrandTotal;

        await EvaluateMemberTierAsync(member);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Awarded {Points} points for order {OrderId}", points, orderId);
    }

    public async Task ProcessReferralBonusAsync(int referrerId, int referredId)
    {
        var referrer = await _db.CustomerLoyalties
            .Include(cl => cl.LoyaltyProgram)
            .FirstOrDefaultAsync(cl => cl.Id == referrerId);

        if (referrer == null || referrer.LoyaltyProgram.ReferralBonus <= 0) return;

        await AddPointsAsync(referrer, referrer.LoyaltyProgram.ReferralBonus, "bonus", "referral",
            referredId.ToString(), "Referral bonus");

        _logger.LogInformation("Awarded referral bonus to member {ReferrerId}", referrerId);
    }

    public async Task ProcessBirthdayBonusAsync(string shopDomain)
    {
        var today = DateTime.UtcNow;
        var members = await _db.CustomerLoyalties
            .Include(cl => cl.LoyaltyProgram)
            .Where(cl => cl.ShopDomain == shopDomain &&
                         cl.Birthday != null &&
                         cl.Birthday.Value.Month == today.Month &&
                         cl.Birthday.Value.Day == today.Day &&
                         cl.LoyaltyProgram.BirthdayBonus > 0)
            .ToListAsync();

        foreach (var member in members)
        {
            await AddPointsAsync(member, member.LoyaltyProgram.BirthdayBonus, "bonus", "birthday", null, "Birthday bonus");
        }

        _logger.LogInformation("Processed birthday bonuses for {Count} members in {ShopDomain}", members.Count, shopDomain);
    }

    public async Task EvaluateTiersAsync(string shopDomain)
    {
        var members = await _db.CustomerLoyalties
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .Where(cl => cl.ShopDomain == shopDomain)
            .ToListAsync();

        foreach (var member in members)
        {
            await EvaluateMemberTierAsync(member);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Evaluated tiers for {Count} members in {ShopDomain}", members.Count, shopDomain);
    }

    public async Task ExpirePointsAsync(string shopDomain)
    {
        var now = DateTime.UtcNow;

        var expiringPoints = await _db.LoyaltyPoints
            .Include(lp => lp.CustomerLoyalty)
            .Where(lp => lp.CustomerLoyalty.ShopDomain == shopDomain &&
                         lp.ExpiresAt != null &&
                         lp.ExpiresAt <= now &&
                         lp.Type == "earn" &&
                         lp.Points > 0)
            .ToListAsync();

        foreach (var lp in expiringPoints)
        {
            var member = lp.CustomerLoyalty;
            await AddPointsAsync(member, -lp.Points, "expire", "expiration", lp.Id.ToString(), "Points expired");
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Expired {Count} point transactions in {ShopDomain}", expiringPoints.Count, shopDomain);
    }

    #endregion

    #region Analytics

    public async Task<LoyaltyAnalyticsDto> GetAnalyticsAsync(string shopDomain, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var members = await _db.CustomerLoyalties
            .Where(cl => cl.ShopDomain == shopDomain)
            .ToListAsync();

        var pointsTransactions = await _db.LoyaltyPoints
            .Include(lp => lp.CustomerLoyalty)
            .Where(lp => lp.CustomerLoyalty.ShopDomain == shopDomain && lp.CreatedAt >= start && lp.CreatedAt <= end)
            .ToListAsync();

        var membersByTier = members
            .Where(m => m.CurrentTierId != null)
            .GroupBy(m => m.CurrentTierId)
            .ToDictionary(g => g.Key?.ToString() ?? "None", g => g.Count());

        var pointsBySource = pointsTransactions
            .Where(p => p.Points > 0)
            .GroupBy(p => p.Source)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Points));

        var dailyPoints = pointsTransactions
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new DailyPointsDto(
                g.Key,
                g.Where(p => p.Points > 0).Sum(p => p.Points),
                Math.Abs(g.Where(p => p.Points < 0 && p.Type == "redeem").Sum(p => p.Points)),
                members.Count(m => m.JoinedAt.Date == g.Key)
            ))
            .OrderBy(d => d.Date)
            .ToList();

        return new LoyaltyAnalyticsDto(
            TotalMembers: members.Count,
            ActiveMembers: members.Count(m => m.LastActivityAt > DateTime.UtcNow.AddDays(-90)),
            NewMembersThisPeriod: members.Count(m => m.JoinedAt >= start && m.JoinedAt <= end),
            TotalPointsIssued: pointsTransactions.Where(p => p.Points > 0).Sum(p => (long)p.Points),
            TotalPointsRedeemed: Math.Abs(pointsTransactions.Where(p => p.Type == "redeem").Sum(p => (long)p.Points)),
            TotalPointsExpired: Math.Abs(pointsTransactions.Where(p => p.Type == "expire").Sum(p => (long)p.Points)),
            TotalRedemptionValue: 0, // Would need to calculate from rewards
            AveragePointsPerMember: members.Count > 0 ? (decimal)members.Average(m => m.PointsBalance) : 0,
            MembersByTier: membersByTier,
            PointsBySource: pointsBySource,
            DailyPoints: dailyPoints
        );
    }

    public async Task<IEnumerable<CustomerLoyaltyDto>> GetTopMembersAsync(string shopDomain, int count = 10)
    {
        var members = await _db.CustomerLoyalties
            .Include(cl => cl.Customer)
            .Include(cl => cl.LoyaltyProgram).ThenInclude(p => p.Tiers)
            .Include(cl => cl.CurrentTier)
            .Where(cl => cl.ShopDomain == shopDomain)
            .OrderByDescending(cl => cl.LifetimePoints)
            .Take(count)
            .ToListAsync();

        return members.Select(MapMemberToDto);
    }

    #endregion

    #region Private Helpers

    private async Task AddPointsAsync(CustomerLoyalty member, int points, string type, string source, string? sourceId, string? description)
    {
        member.PointsBalance += points;
        if (points > 0)
            member.LifetimePoints += points;
        else if (type == "redeem")
            member.LifetimeRedeemed += Math.Abs(points);

        member.LastActivityAt = DateTime.UtcNow;

        DateTime? expiresAt = null;
        if (points > 0 && member.LoyaltyProgram.PointsExpireMonths.HasValue)
            expiresAt = DateTime.UtcNow.AddMonths(member.LoyaltyProgram.PointsExpireMonths.Value);

        var transaction = new LoyaltyPoints
        {
            CustomerLoyaltyId = member.Id,
            Type = type,
            Points = points,
            BalanceAfter = member.PointsBalance,
            Source = source,
            SourceId = sourceId,
            Description = description,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.LoyaltyPoints.Add(transaction);
        await _db.SaveChangesAsync();
    }

    private async Task EvaluateMemberTierAsync(CustomerLoyalty member)
    {
        var tiers = member.LoyaltyProgram.Tiers.OrderByDescending(t => t.MinimumPoints).ToList();
        var newTier = tiers.FirstOrDefault(t => member.LifetimePoints >= t.MinimumPoints);

        if (newTier?.Id != member.CurrentTierId)
        {
            member.CurrentTierId = newTier?.Id;
            member.TierUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static LoyaltyProgramDto MapProgramToDto(LoyaltyProgram p) => new(
        p.Id, p.ShopDomain, p.Name, p.IsActive, p.PointsPerDollar, p.PointsValueCents,
        p.MinimumRedemption, p.SignupBonus, p.BirthdayBonus, p.ReviewBonus, p.ReferralBonus,
        p.PointsExpireMonths, p.PointsName, p.Currency, p.CreatedAt, p.UpdatedAt,
        p.Tiers.Count, p.Rewards.Count, p.Members.Count
    );

    private static LoyaltyTierDto MapTierToDto(LoyaltyTier t, int memberCount) => new(
        t.Id, t.LoyaltyProgramId, t.Name, t.MinimumPoints, t.PointsMultiplier,
        t.PercentageDiscount, t.FreeShipping, t.ExclusiveAccess, t.Color, t.Icon,
        t.DisplayOrder, t.CreatedAt, memberCount
    );

    private static LoyaltyRewardDto MapRewardToDto(LoyaltyReward r, int redemptionCount) => new(
        r.Id, r.LoyaltyProgramId, r.Name, r.Description, r.Type, r.PointsCost,
        r.Value, r.MinimumOrderAmount, r.ProductId, r.Product?.Title,
        r.MaxRedemptions, r.IsActive, r.StartsAt, r.EndsAt, r.ImageUrl, r.CreatedAt, redemptionCount
    );

    private CustomerLoyaltyDto MapMemberToDto(CustomerLoyalty cl)
    {
        int? pointsToNextTier = null;
        string? nextTierName = null;

        if (cl.LoyaltyProgram?.Tiers != null)
        {
            var nextTier = cl.LoyaltyProgram.Tiers
                .Where(t => t.MinimumPoints > cl.LifetimePoints)
                .OrderBy(t => t.MinimumPoints)
                .FirstOrDefault();

            if (nextTier != null)
            {
                pointsToNextTier = nextTier.MinimumPoints - cl.LifetimePoints;
                nextTierName = nextTier.Name;
            }
        }

        return new CustomerLoyaltyDto(
            cl.Id, cl.ShopDomain, cl.CustomerId,
            cl.Customer != null ? $"{cl.Customer.FirstName} {cl.Customer.LastName}".Trim() : null,
            cl.Customer?.Email, cl.LoyaltyProgramId, cl.CurrentTierId, cl.CurrentTier?.Name,
            cl.PointsBalance, cl.LifetimePoints, cl.LifetimeRedeemed, cl.LifetimeSpent,
            cl.ReferralCode, cl.ReferredById, cl.Birthday, cl.JoinedAt, cl.LastActivityAt,
            cl.TierUpdatedAt, pointsToNextTier, nextTierName
        );
    }

    private static LoyaltyPointsDto MapPointsToDto(LoyaltyPoints lp) => new(
        lp.Id, lp.CustomerLoyaltyId, lp.Type, lp.Points, lp.BalanceAfter,
        lp.Source, lp.SourceId, lp.Description, lp.ExpiresAt, lp.CreatedAt
    );

    #endregion
}
