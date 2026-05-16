using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Revenue;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private static readonly ReservationStatus[] RevenueReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn
    ];

    public DateTime BusinessDate { get; set; }
    public decimal OccupancyToday { get; set; }
    public decimal OccupancyThisMonth { get; set; }
    public decimal RoomRevenueToday { get; set; }
    public decimal RoomRevenueThisMonth { get; set; }
    public decimal AdrToday { get; set; }
    public decimal AdrThisMonth { get; set; }
    public decimal RevParToday { get; set; }
    public decimal RevParThisMonth { get; set; }
    public int AvailableRoomsToday { get; set; }
    public int OutOfOrderRoomsToday { get; set; }
    public int ReservationsNextSevenDays { get; set; }

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);
        var monthStart = new DateTime(BusinessDate.Year, BusinessDate.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var daysInMonth = (nextMonth - monthStart).Days;

        var totalRooms = await _context.Rooms.CountAsync(room => room.IsActive);
        OutOfOrderRoomsToday = await _context.Rooms.CountAsync(room => room.IsActive && room.Status == RoomStatus.OutOfOrder);

        var roomsSoldToday = await CountSoldRoomsForDateAsync(BusinessDate);
        AvailableRoomsToday = Math.Max(0, totalRooms - OutOfOrderRoomsToday - roomsSoldToday);
        OccupancyToday = Percent(roomsSoldToday, totalRooms);

        var monthlyReservations = await _context.Reservations
            .AsNoTracking()
            .Where(reservation =>
                RevenueReservationStatuses.Contains(reservation.Status) &&
                reservation.ArrivalDate.Date < nextMonth &&
                reservation.DepartureDate.Date > monthStart)
            .Select(reservation => new
            {
                reservation.ArrivalDate,
                reservation.DepartureDate
            })
            .ToListAsync();

        var roomNightsSoldThisMonth = monthlyReservations.Sum(reservation =>
            CountOverlapNights(reservation.ArrivalDate.Date, reservation.DepartureDate.Date, monthStart, nextMonth));
        var availableRoomNightsThisMonth = totalRooms * daysInMonth;
        OccupancyThisMonth = Percent(roomNightsSoldThisMonth, availableRoomNightsThisMonth);

        RoomRevenueToday = await SumRoomRevenueAsync(BusinessDate, nextBusinessDate);
        RoomRevenueThisMonth = await SumRoomRevenueAsync(monthStart, nextMonth);

        AdrToday = Divide(RoomRevenueToday, roomsSoldToday);
        AdrThisMonth = Divide(RoomRevenueThisMonth, roomNightsSoldThisMonth);
        RevParToday = Divide(RoomRevenueToday, totalRooms);
        RevParThisMonth = Divide(RoomRevenueThisMonth, availableRoomNightsThisMonth);

        var sevenDaysOut = BusinessDate.AddDays(7);
        ReservationsNextSevenDays = await _context.Reservations.CountAsync(reservation =>
            RevenueReservationStatuses.Contains(reservation.Status) &&
            reservation.ArrivalDate.Date >= BusinessDate &&
            reservation.ArrivalDate.Date < sevenDaysOut);
    }

    private async Task<int> CountSoldRoomsForDateAsync(DateTime date)
    {
        var businessDate = date.Date;

        return await _context.Reservations.CountAsync(reservation =>
            RevenueReservationStatuses.Contains(reservation.Status) &&
            reservation.ArrivalDate.Date <= businessDate &&
            reservation.DepartureDate.Date > businessDate);
    }

    private async Task<decimal> SumRoomRevenueAsync(DateTime start, DateTime end)
    {
        var charges = await _context.FolioItems
            .AsNoTracking()
            .Where(item =>
                !item.IsVoided &&
                item.PostingDate >= start &&
                item.PostingDate < end)
            .ToListAsync();

        return charges
            .Where(IsRoomCharge)
            .Sum(item => item.Amount);
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private static int CountOverlapNights(DateTime arrival, DateTime departure, DateTime windowStart, DateTime windowEnd)
    {
        var start = arrival > windowStart ? arrival : windowStart;
        var end = departure < windowEnd ? departure : windowEnd;
        return Math.Max(0, (end - start).Days);
    }

    private static decimal Percent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator * 100;
    }

    private static decimal Divide(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator;
    }

    private static bool IsRoomCharge(FolioItem item)
    {
        return item.ChargeCode.StartsWith("ROOM", StringComparison.OrdinalIgnoreCase);
    }
}
