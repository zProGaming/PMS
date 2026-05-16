using Microsoft.AspNetCore.Mvc.RazorPages;
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

namespace Vantage.PMS.Pages;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }
    public decimal OccupancyPercentage { get; set; }
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int OutOfOrderRooms { get; set; }
    public int ArrivalsToday { get; set; }
    public int DeparturesToday { get; set; }
    public int RealizedArrivalsToday { get; set; }
    public int RealizedDeparturesToday { get; set; }
    public int InHouseGuests { get; set; }
    public int EndOfDayForecast { get; set; }
    public int CurrentInHousePax { get; set; }
    public int ExpectedArrivalPax { get; set; }
    public int ExpectedDeparturePax { get; set; }
    public int RealizedArrivalPax { get; set; }
    public int RealizedDeparturePax { get; set; }
    public int EndOfDayForecastPax { get; set; }
    public int CleanStateRooms { get; set; }
    public int CleanRooms { get; set; }
    public int InspectedRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public int ReservedRooms { get; set; }
    public int BlockRooms { get; set; }
    public int DailyUseRooms { get; set; }
    public int CancellationsToday { get; set; }
    public int CancellationRoomNights { get; set; }
    public int NoShowsToday { get; set; }
    public decimal RoomRevenueToday { get; set; }
    public decimal FoodBeverageRevenueToday { get; set; }
    public decimal BanquetRevenueToday { get; set; }
    public decimal TotalRevenueToday { get; set; }
    public decimal TotalPaymentsToday { get; set; }
    public decimal OutstandingGuestBalance { get; set; }
    public decimal CancellationRevenueImpact { get; set; }
    public decimal AdrToday { get; set; }
    public decimal RevParToday { get; set; }
    public int OpenServiceRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int LowStockItems { get; set; }
    public int HighBalanceFolios { get; set; }
    public bool HasOperationalData { get; set; }
    public IList<ManagementInsight> CriticalInsights { get; set; } = new List<ManagementInsight>();
    public IList<ManagementInsight> HighPriorityInsights { get; set; } = new List<ManagementInsight>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);

        TotalRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive);
        AvailableRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Available);
        OccupiedRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Occupied);
        DirtyRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Dirty);
        OutOfOrderRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.OutOfOrder);
        CleanStateRooms = await _context.Rooms.AsNoTracking().CountAsync(room =>
            room.IsActive && (room.Status == RoomStatus.Clean || room.Status == RoomStatus.Inspected || room.Status == RoomStatus.Available));
        CleanRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Clean);
        InspectedRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Inspected);
        MaintenanceRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Maintenance);
        OccupancyPercentage = TotalRooms <= 0 ? 0 : (decimal)OccupiedRooms / TotalRooms * 100;

        ArrivalsToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.ArrivalDate >= BusinessDate &&
            reservation.ArrivalDate < nextBusinessDate &&
            reservation.Status == ReservationStatus.Reserved);
        ExpectedArrivalPax = await _context.Reservations.AsNoTracking()
            .Where(reservation =>
                reservation.ArrivalDate >= BusinessDate &&
                reservation.ArrivalDate < nextBusinessDate &&
                reservation.Status == ReservationStatus.Reserved)
            .SumAsync(reservation => (int?)(reservation.Adults + reservation.Children)) ?? 0;

        DeparturesToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.DepartureDate >= BusinessDate &&
            reservation.DepartureDate < nextBusinessDate &&
            reservation.Status == ReservationStatus.CheckedIn);
        ExpectedDeparturePax = await _context.Reservations.AsNoTracking()
            .Where(reservation =>
                reservation.DepartureDate >= BusinessDate &&
                reservation.DepartureDate < nextBusinessDate &&
                reservation.Status == ReservationStatus.CheckedIn)
            .SumAsync(reservation => (int?)(reservation.Adults + reservation.Children)) ?? 0;

        RealizedArrivalsToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.ActualCheckInDate >= BusinessDate &&
            reservation.ActualCheckInDate < nextBusinessDate);
        RealizedArrivalPax = await _context.Reservations.AsNoTracking()
            .Where(reservation =>
                reservation.ActualCheckInDate >= BusinessDate &&
                reservation.ActualCheckInDate < nextBusinessDate)
            .SumAsync(reservation => (int?)(reservation.Adults + reservation.Children)) ?? 0;

        RealizedDeparturesToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.ActualCheckOutDate >= BusinessDate &&
            reservation.ActualCheckOutDate < nextBusinessDate);
        RealizedDeparturePax = await _context.Reservations.AsNoTracking()
            .Where(reservation =>
                reservation.ActualCheckOutDate >= BusinessDate &&
                reservation.ActualCheckOutDate < nextBusinessDate)
            .SumAsync(reservation => (int?)(reservation.Adults + reservation.Children)) ?? 0;

        CancellationsToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.ArrivalDate >= BusinessDate &&
            reservation.ArrivalDate < nextBusinessDate &&
            reservation.Status == ReservationStatus.Cancelled);
        NoShowsToday = await _context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.ArrivalDate >= BusinessDate &&
            reservation.ArrivalDate < nextBusinessDate &&
            reservation.Status == ReservationStatus.NoShow);
        var cancelledReservations = await _context.Reservations.AsNoTracking()
            .Where(reservation =>
                reservation.ArrivalDate >= BusinessDate &&
                reservation.ArrivalDate < nextBusinessDate &&
                reservation.Status == ReservationStatus.Cancelled)
            .Select(reservation => new
            {
                reservation.ArrivalDate,
                reservation.DepartureDate,
                reservation.RateAmount
            })
            .ToListAsync();
        CancellationRoomNights = cancelledReservations.Sum(reservation => Math.Max(1, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days));
        CancellationRevenueImpact = cancelledReservations.Sum(reservation => Math.Max(1, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days) * reservation.RateAmount);

        InHouseGuests = await _context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn);
        CurrentInHousePax = await _context.Reservations.AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn)
            .SumAsync(reservation => (int?)(reservation.Adults + reservation.Children)) ?? 0;
        EndOfDayForecast = Math.Max(0, InHouseGuests + ArrivalsToday - DeparturesToday);
        EndOfDayForecastPax = Math.Max(0, CurrentInHousePax + ExpectedArrivalPax - ExpectedDeparturePax);
        ReservedRooms = ArrivalsToday;
        RoomRevenueToday = await SumRoomRevenueAsync(BusinessDate, nextBusinessDate);
        FoodBeverageRevenueToday = await _context.POSOrders.AsNoTracking()
            .Where(order =>
                order.OrderDate >= BusinessDate &&
                order.OrderDate < nextBusinessDate &&
                (order.PaymentStatus == POSPaymentStatus.Paid || order.PaymentStatus == POSPaymentStatus.ChargedToRoom))
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        BanquetRevenueToday = await _context.BanquetCharges.AsNoTracking()
            .Where(charge => !charge.IsVoided && charge.ChargeDate >= BusinessDate && charge.ChargeDate < nextBusinessDate)
            .SumAsync(charge => (decimal?)charge.Amount) ?? 0;
        TotalRevenueToday = RoomRevenueToday + FoodBeverageRevenueToday + BanquetRevenueToday;
        AdrToday = OccupiedRooms <= 0 ? 0 : RoomRevenueToday / OccupiedRooms;
        RevParToday = TotalRooms <= 0 ? 0 : RoomRevenueToday / TotalRooms;
        TotalPaymentsToday = await _context.Payments.AsNoTracking()
            .Where(payment => payment.PaymentDate >= BusinessDate && payment.PaymentDate < nextBusinessDate && payment.Status == PaymentStatus.Completed)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;
        OutstandingGuestBalance = await CalculateOutstandingGuestBalanceAsync();
        OpenServiceRequests = await _context.GuestServiceRequests.AsNoTracking().CountAsync(request =>
            request.Status != GuestServiceRequestStatus.Completed &&
            request.Status != GuestServiceRequestStatus.Cancelled);
        PendingApprovals = await CountPendingApprovalsAsync();
        LowStockItems = await _context.InventoryItems.AsNoTracking().CountAsync(item => item.IsActive && item.CurrentStock <= item.ReorderLevel);

        CriticalInsights = await _context.ManagementInsights.AsNoTracking()
            .Where(insight => !insight.IsResolved && insight.Severity == ManagementInsightSeverity.Critical)
            .OrderByDescending(insight => insight.CreatedAt)
            .Take(5)
            .ToListAsync();
        HighPriorityInsights = await _context.ManagementInsights.AsNoTracking()
            .Where(insight => !insight.IsResolved && insight.Severity == ManagementInsightSeverity.High)
            .OrderByDescending(insight => insight.CreatedAt)
            .Take(5)
            .ToListAsync();

        HasOperationalData = TotalRooms > 0 ||
            InHouseGuests > 0 ||
            ArrivalsToday > 0 ||
            DeparturesToday > 0 ||
            RoomRevenueToday > 0 ||
            FoodBeverageRevenueToday > 0 ||
            BanquetRevenueToday > 0 ||
            TotalPaymentsToday > 0;
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings.AsNoTracking().FirstOrDefaultAsync();
        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private async Task<decimal> SumRoomRevenueAsync(DateTime start, DateTime end)
    {
        return await _context.FolioItems.AsNoTracking()
            .Where(item =>
                !item.IsVoided &&
                item.PostingDate >= start &&
                item.PostingDate < end &&
                (item.ChargeCode.StartsWith("ROOM") ||
                    item.ChargeCodeDefinition != null &&
                    item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Room))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
    }

    private async Task<decimal> CalculateOutstandingGuestBalanceAsync()
    {
        var balances = await _context.Folios.AsNoTracking()
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

        var outstandingBalances = balances
            .Select(folio => folio.Charges - folio.Payments)
            .Where(balance => balance > 0)
            .ToList();

        HighBalanceFolios = outstandingBalances.Count(balance => balance >= 50000);

        return outstandingBalances.Sum();
    }

    private async Task<int> CountPendingApprovalsAsync()
    {
        var refundApprovals = await _context.RefundTransactions.AsNoTracking().CountAsync(refund =>
            refund.Status == RefundStatus.Requested || refund.Status == RefundStatus.ForApproval || refund.Status == RefundStatus.Approved);
        var voidApprovals = await _context.VoidRequests.AsNoTracking().CountAsync(request => request.Status == ApprovalStatus.Pending);
        var discountApprovals = await _context.DiscountApprovals.AsNoTracking().CountAsync(discount => discount.Status == ApprovalStatus.Pending);
        var purchaseRequests = await _context.PurchaseRequests.AsNoTracking().CountAsync(request => request.Status == PurchaseRequestStatus.Submitted);
        var purchaseOrders = await _context.PurchaseOrders.AsNoTracking().CountAsync(order => order.Status == PurchaseOrderStatus.ForApproval);
        var stockAdjustments = await _context.StockAdjustments.AsNoTracking().CountAsync(adjustment => adjustment.Status == StockAdjustmentStatus.ForApproval);

        return refundApprovals + voidApprovals + discountApprovals + purchaseRequests + purchaseOrders + stockAdjustments;
    }
}
