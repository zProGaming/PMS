using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.Groups;

namespace Vantage.PMS.Services;

public class GroupManagementService(ApplicationDbContext context)
{
    public async Task<GroupCollectionSummary> GetCollectionSummaryAsync(int groupBookingId)
    {
        var group = await context.GroupBookings
            .AsNoTracking()
            .Include(item => item.RoomBlocks)
            .FirstOrDefaultAsync(item => item.Id == groupBookingId);

        if (group is null)
        {
            return new GroupCollectionSummary();
        }

        var memberReservationIds = await context.GroupMemberReservations
            .AsNoTracking()
            .Where(item => item.GroupBookingId == groupBookingId)
            .Select(item => item.ReservationId)
            .ToListAsync();

        var memberFolioIds = await context.Folios
            .AsNoTracking()
            .Where(item => memberReservationIds.Contains(item.ReservationId))
            .Select(item => item.Id)
            .ToListAsync();

        var groupLinkedFolioIds = await context.GroupFolios
            .AsNoTracking()
            .Where(item => item.GroupBookingId == groupBookingId && item.FolioId != null)
            .Select(item => item.FolioId!.Value)
            .ToListAsync();

        var folioIds = memberFolioIds.Concat(groupLinkedFolioIds).Distinct().ToList();
        var postedCharges = await context.FolioItems
            .AsNoTracking()
            .Where(item => folioIds.Contains(item.FolioId) && !item.IsVoided)
            .SumAsync(item => (decimal?)item.Amount) ?? 0;

        var postedPayments = await context.Payments
            .AsNoTracking()
            .Where(item => folioIds.Contains(item.FolioId) && item.Status == PaymentStatus.Completed)
            .SumAsync(item => (decimal?)item.Amount) ?? 0;

        var deposits = await context.GroupDeposits
            .AsNoTracking()
            .Where(item => item.GroupBookingId == groupBookingId && item.Status != GroupDepositStatus.Cancelled)
            .SumAsync(item => (decimal?)item.Amount) ?? 0;

        var allocated = await context.GroupPaymentAllocations
            .AsNoTracking()
            .Where(item => item.GroupBookingId == groupBookingId)
            .SumAsync(item => (decimal?)item.AllocatedAmount) ?? 0;

        var estimatedCharges = group.RoomBlocks.Sum(block => Math.Max(0, block.RoomsBlocked - block.RoomsReleased) * block.RateAmount);

        return new GroupCollectionSummary(
            estimatedCharges,
            postedCharges,
            deposits,
            postedPayments,
            allocated,
            postedCharges - postedPayments - allocated);
    }

    public async Task<decimal> GetDepositAvailableAmountAsync(int groupDepositId)
    {
        var deposit = await context.GroupDeposits.AsNoTracking().FirstOrDefaultAsync(item => item.Id == groupDepositId);
        if (deposit is null || deposit.Status is GroupDepositStatus.Cancelled or GroupDepositStatus.Refunded or GroupDepositStatus.Forfeited)
        {
            return 0;
        }

        var allocated = await context.GroupPaymentAllocations
            .AsNoTracking()
            .Where(item => item.GroupDepositId == groupDepositId)
            .SumAsync(item => (decimal?)item.AllocatedAmount) ?? 0;

        return Math.Max(0, deposit.Amount - allocated);
    }

    public async Task<ChargeRoutingResult> ResolveChargeRoutingAsync(Folio sourceFolio, FolioItem charge)
    {
        var category = await ResolveChargeCategoryAsync(charge);
        var groupMembership = await context.GroupMemberReservations
            .AsNoTracking()
            .Where(item => item.ReservationId == sourceFolio.ReservationId)
            .OrderByDescending(item => item.IsPrimaryGuest)
            .FirstOrDefaultAsync();
        var groupBookingId = groupMembership?.GroupBookingId;

        var query = context.ChargeRoutingRules
            .AsNoTracking()
            .Include(item => item.TargetGroupFolio)
            .Where(item => item.IsActive && item.SourceChargeCategory == category);

        query = groupBookingId is null
            ? query.Where(item =>
                item.FolioId == sourceFolio.Id ||
                item.ReservationId == sourceFolio.ReservationId)
            : query.Where(item =>
                item.FolioId == sourceFolio.Id ||
                item.ReservationId == sourceFolio.ReservationId ||
                item.GroupBookingId == groupBookingId.Value);

        var rule = await query
            .OrderByDescending(item => item.FolioId == sourceFolio.Id)
            .ThenByDescending(item => item.ReservationId == sourceFolio.ReservationId)
            .FirstOrDefaultAsync();

        if (rule is null)
        {
            return ChargeRoutingResult.NotRouted(category);
        }

        var targetFolioId = rule.TargetFolioId ?? rule.TargetGroupFolio?.FolioId;
        if (targetFolioId is null || targetFolioId == sourceFolio.Id)
        {
            return ChargeRoutingResult.NotRouted(category, rule.Id);
        }

        var targetFolio = await context.Folios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == targetFolioId);

        return targetFolio is null
            ? ChargeRoutingResult.NotRouted(category, rule.Id)
            : ChargeRoutingResult.Routed(category, rule.Id, targetFolio.Id, targetFolio.FolioNumber);
    }

    private async Task<ChargeCategory> ResolveChargeCategoryAsync(FolioItem charge)
    {
        if (charge.ChargeCodeId is not null)
        {
            var chargeCode = await context.ChargeCodes
                .AsNoTracking()
                .Where(item => item.Id == charge.ChargeCodeId)
                .Select(item => (ChargeCategory?)item.ChargeCategory)
                .FirstOrDefaultAsync();

            if (chargeCode is not null)
            {
                return chargeCode.Value;
            }
        }

        var code = (charge.ChargeCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.StartsWith("ROOM") || code.StartsWith("RM"))
        {
            return ChargeCategory.Room;
        }

        if (code.StartsWith("FB") || code.StartsWith("F&B") || code.StartsWith("POS"))
        {
            return ChargeCategory.FoodBeverage;
        }

        if (code.StartsWith("BNQ") || code.StartsWith("BANQ"))
        {
            return ChargeCategory.Banquet;
        }

        if (code.StartsWith("TAX") || code.Contains("VAT"))
        {
            return ChargeCategory.Tax;
        }

        return ChargeCategory.Miscellaneous;
    }
}

public record GroupCollectionSummary(
    decimal TotalEstimatedCharges = 0,
    decimal TotalPostedCharges = 0,
    decimal TotalDeposits = 0,
    decimal TotalPayments = 0,
    decimal TotalAllocated = 0,
    decimal OutstandingBalance = 0);

public record ChargeRoutingResult(
    bool IsRouted,
    ChargeCategory ChargeCategory,
    int? RuleId,
    int? TargetFolioId,
    string? TargetFolioNumber)
{
    public static ChargeRoutingResult NotRouted(ChargeCategory category, int? ruleId = null) => new(false, category, ruleId, null, null);

    public static ChargeRoutingResult Routed(ChargeCategory category, int ruleId, int targetFolioId, string targetFolioNumber) => new(true, category, ruleId, targetFolioId, targetFolioNumber);
}
