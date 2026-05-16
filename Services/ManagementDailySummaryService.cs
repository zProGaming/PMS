using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Services;

public class ManagementDailySummaryService(ApplicationDbContext context, AIPlaceholderService aiPlaceholderService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly AIPlaceholderService _aiPlaceholderService = aiPlaceholderService;

    public async Task<ManagementDailySummary> GenerateOrUpdateTodaySummaryAsync(string generatedBy)
    {
        var businessDate = await GetBusinessDateAsync();
        var nextBusinessDate = businessDate.AddDays(1);

        var summary = await _context.ManagementDailySummaries
            .FirstOrDefaultAsync(item => item.BusinessDate == businessDate);

        if (summary is null)
        {
            summary = new ManagementDailySummary
            {
                BusinessDate = businessDate,
                CreatedAt = DateTime.Now
            };
            _context.ManagementDailySummaries.Add(summary);
        }

        summary.GeneratedBy = generatedBy;
        summary.TotalRooms = await _context.Rooms.CountAsync(room => room.IsActive);
        summary.OccupiedRooms = await _context.Rooms.CountAsync(room => room.IsActive && room.Status == RoomStatus.Occupied);
        summary.AvailableRooms = await _context.Rooms.CountAsync(room => room.IsActive && room.Status == RoomStatus.Available);
        summary.DirtyRooms = await _context.Rooms.CountAsync(room => room.IsActive && room.Status == RoomStatus.Dirty);
        summary.OutOfOrderRooms = await _context.Rooms.CountAsync(room => room.IsActive && room.Status == RoomStatus.OutOfOrder);
        summary.OccupancyPercentage = Percent(summary.OccupiedRooms, summary.TotalRooms);

        summary.ArrivalsToday = await _context.Reservations.CountAsync(reservation =>
            reservation.ArrivalDate >= businessDate &&
            reservation.ArrivalDate < nextBusinessDate &&
            reservation.Status != ReservationStatus.Cancelled);

        summary.DeparturesToday = await _context.Reservations.CountAsync(reservation =>
            reservation.DepartureDate >= businessDate &&
            reservation.DepartureDate < nextBusinessDate &&
            reservation.Status != ReservationStatus.Cancelled);

        summary.InHouseGuests = await _context.Reservations.CountAsync(reservation =>
            reservation.Status == ReservationStatus.CheckedIn);

        summary.RoomRevenue = await SumRoomRevenueAsync(businessDate, nextBusinessDate);
        summary.FBRevenue = await _context.POSOrders
            .Where(order =>
                order.OrderDate >= businessDate &&
                order.OrderDate < nextBusinessDate &&
                (order.PaymentStatus == POSPaymentStatus.Paid || order.PaymentStatus == POSPaymentStatus.ChargedToRoom))
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;

        summary.BanquetRevenue = await _context.BanquetCharges
            .Where(charge => !charge.IsVoided && charge.ChargeDate >= businessDate && charge.ChargeDate < nextBusinessDate)
            .SumAsync(charge => (decimal?)charge.Amount) ?? 0;

        summary.TotalRevenue = summary.RoomRevenue + summary.FBRevenue + summary.BanquetRevenue;

        summary.TotalPayments = await _context.Payments
            .Where(payment =>
                payment.PaymentDate >= businessDate &&
                payment.PaymentDate < nextBusinessDate &&
                payment.Status == PaymentStatus.Completed)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;

        summary.OutstandingGuestBalances = await CalculateOutstandingGuestBalancesAsync();
        summary.ARBalance = await _context.ARInvoices
            .Where(invoice =>
                invoice.Status != ARInvoiceStatus.Paid &&
                invoice.Status != ARInvoiceStatus.Cancelled &&
                invoice.Status != ARInvoiceStatus.WrittenOff)
            .SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;

        summary.OpenServiceRequests = await _context.GuestServiceRequests.CountAsync(request =>
            request.Status != GuestServiceRequestStatus.Completed &&
            request.Status != GuestServiceRequestStatus.Cancelled);

        summary.PendingHousekeepingTasks = await _context.HousekeepingTasks.CountAsync(task =>
            task.TaskStatus != HousekeepingTaskStatus.Completed &&
            task.TaskStatus != HousekeepingTaskStatus.Cancelled);

        summary.PendingMaintenanceTickets = 0;

        summary.LowStockItems = await _context.InventoryItems.CountAsync(item =>
            item.IsActive && item.CurrentStock <= item.ReorderLevel);

        summary.PendingPurchaseRequests = await _context.PurchaseRequests.CountAsync(request =>
            request.Status == PurchaseRequestStatus.Submitted);

        summary.PendingApprovals = await CountPendingApprovalsAsync();
        summary.SummaryText = await _aiPlaceholderService.GenerateDailySummaryTextAsync(summary);

        _context.AIActionLogs.Add(new AIActionLog
        {
            ActionDate = DateTime.Now,
            ActionType = AIActionType.SummaryGenerated,
            Module = "Management AI",
            Description = $"Generated management summary for {businessDate:yyyy-MM-dd}.",
            PerformedBy = generatedBy
        });

        await _context.SaveChangesAsync();
        return summary;
    }

    public async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private async Task<decimal> SumRoomRevenueAsync(DateTime start, DateTime end)
    {
        return await _context.FolioItems
            .Where(item =>
                !item.IsVoided &&
                item.PostingDate >= start &&
                item.PostingDate < end &&
                (item.ChargeCode.StartsWith("ROOM") ||
                 item.ChargeCodeDefinition != null && item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Room))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
    }

    private async Task<decimal> CalculateOutstandingGuestBalancesAsync()
    {
        var folioBalances = await _context.Folios
            .Select(folio => new
            {
                Charges = _context.FolioItems
                    .Where(item => item.FolioId == folio.Id && !item.IsVoided)
                    .Sum(item => (decimal?)item.Amount) ?? 0,
                Payments = _context.Payments
                    .Where(payment =>
                        payment.FolioId == folio.Id &&
                        payment.Status != PaymentStatus.Voided &&
                        payment.Status != PaymentStatus.Failed)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0
            })
            .ToListAsync();

        return folioBalances
            .Select(folio => folio.Charges - folio.Payments)
            .Where(balance => balance > 0)
            .Sum();
    }

    private async Task<int> CountPendingApprovalsAsync()
    {
        var refundApprovals = await _context.RefundTransactions.CountAsync(refund =>
            refund.Status == RefundStatus.Requested ||
            refund.Status == RefundStatus.ForApproval ||
            refund.Status == RefundStatus.Approved);

        var voidApprovals = await _context.VoidRequests.CountAsync(request =>
            request.Status == ApprovalStatus.Pending);

        var discountApprovals = await _context.DiscountApprovals.CountAsync(discount =>
            discount.Status == ApprovalStatus.Pending);

        var purchaseRequestApprovals = await _context.PurchaseRequests.CountAsync(request =>
            request.Status == PurchaseRequestStatus.Submitted);

        var purchaseOrderApprovals = await _context.PurchaseOrders.CountAsync(order =>
            order.Status == PurchaseOrderStatus.ForApproval);

        var stockAdjustmentApprovals = await _context.StockAdjustments.CountAsync(adjustment =>
            adjustment.Status == StockAdjustmentStatus.ForApproval);

        return refundApprovals + voidApprovals + discountApprovals + purchaseRequestApprovals + purchaseOrderApprovals + stockAdjustmentApprovals;
    }

    private static decimal Percent(decimal value, decimal total)
    {
        return total <= 0 ? 0 : value / total * 100;
    }
}
