using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Revenue.Calendar;

public class IndexModel(ApplicationDbContext context, RevenueManagementService revenueManagementService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly RevenueManagementService _revenueManagementService = revenueManagementService;
    private static readonly ReservationStatus[] RevenueReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn
    ];

    public DateTime BusinessDate { get; set; }
    public IList<RevenueCalendarDay> CalendarDays { get; set; } = new List<RevenueCalendarDay>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var dates = Enumerable.Range(0, 30).Select(offset => BusinessDate.AddDays(offset).Date).ToList();
        var roomTypes = await _context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.IsActive)
            .OrderBy(roomType => roomType.Code)
            .ToListAsync();

        var baseRates = roomTypes
            .Select(roomType => roomType.BaseRate)
            .Where(rate => rate > 0)
            .ToList();

        foreach (var date in dates)
        {
            var roomTypeAvailability = new List<RevenueManagementService.DailyAvailability>();
            foreach (var roomType in roomTypes)
            {
                roomTypeAvailability.Add(await _revenueManagementService.GetAvailabilityAsync(roomType.Id, date));
            }

            var totalRooms = roomTypeAvailability.Sum(day => day.TotalRooms);
            var roomsSold = await CountSoldRoomsForDateAsync(date);
            var roomsAvailable = roomTypeAvailability.Sum(day => day.RoomsAvailable);
            var stopSell = roomTypeAvailability.Any(day => day.StopSell) ||
                await _context.RateRestrictions.AnyAsync(restriction =>
                    restriction.RestrictionDate.Date == date &&
                    restriction.StopSell);

            var minimumLengthOfStay = await _context.RateRestrictions
                .AsNoTracking()
                .Where(restriction => restriction.RestrictionDate.Date == date)
                .Select(restriction => (int?)restriction.MinimumLengthOfStay)
                .MaxAsync() ?? 0;

            var lowRate = await _revenueManagementService.GetBaseRateRangeLowAsync(date);
            var highRate = await _revenueManagementService.GetBaseRateRangeHighAsync(date);

            if (lowRate is null && baseRates.Count > 0)
            {
                lowRate = baseRates.Min();
                highRate = baseRates.Max();
            }

            CalendarDays.Add(new RevenueCalendarDay(
                date,
                totalRooms,
                roomsSold,
                roomsAvailable,
                totalRooms <= 0 ? 0 : (decimal)roomsSold / totalRooms * 100,
                FormatRateRange(lowRate, highRate),
                stopSell,
                minimumLengthOfStay));
        }
    }

    private async Task<int> CountSoldRoomsForDateAsync(DateTime date)
    {
        return await _context.Reservations.CountAsync(reservation =>
            RevenueReservationStatuses.Contains(reservation.Status) &&
            reservation.ArrivalDate.Date <= date.Date &&
            reservation.DepartureDate.Date > date.Date);
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private static string FormatRateRange(decimal? lowRate, decimal? highRate)
    {
        if (lowRate is null || highRate is null)
        {
            return "-";
        }

        return lowRate == highRate
            ? lowRate.Value.ToString("C")
            : $"{lowRate.Value:C} - {highRate.Value:C}";
    }

    public record RevenueCalendarDay(
        DateTime Date,
        int TotalRooms,
        int RoomsSold,
        int RoomsAvailable,
        decimal OccupancyPercentage,
        string BaseRateRange,
        bool StopSell,
        int MinimumLengthOfStay);
}
