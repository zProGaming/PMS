using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Services;

public class RevenueManagementService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;
    private static readonly ReservationStatus[] BlockingReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Reserved,
        ReservationStatus.CheckedIn
    ];

    public async Task<decimal> GetSuggestedRateAsync(int? ratePlanId, int? roomTypeId, DateTime arrivalDate, DateTime departureDate)
    {
        if (roomTypeId is null)
        {
            return 0;
        }

        var arrival = arrivalDate.Date;
        var departure = departureDate.Date;

        if (ratePlanId is not null)
        {
            var seasonalRate = await _context.SeasonalRates
                .AsNoTracking()
                .Where(rate =>
                    rate.IsActive &&
                    rate.RatePlanId == ratePlanId &&
                    rate.RoomTypeId == roomTypeId &&
                    rate.StartDate.Date <= arrival &&
                    rate.EndDate.Date >= departure)
                .OrderByDescending(rate => rate.StartDate)
                .Select(rate => (decimal?)rate.Rate)
                .FirstOrDefaultAsync();

            if (seasonalRate is not null)
            {
                return seasonalRate.Value;
            }

            var roomTypeRate = await _context.RoomTypeRates
                .AsNoTracking()
                .Where(rate =>
                    rate.IsActive &&
                    rate.RatePlanId == ratePlanId &&
                    rate.RoomTypeId == roomTypeId &&
                    rate.EffectiveFrom.Date <= arrival &&
                    rate.EffectiveTo.Date >= departure)
                .OrderByDescending(rate => rate.EffectiveFrom)
                .Select(rate => (decimal?)rate.BaseRate)
                .FirstOrDefaultAsync();

            if (roomTypeRate is not null)
            {
                return roomTypeRate.Value;
            }
        }

        return await _context.RoomTypes
            .AsNoTracking()
            .Where(roomType => roomType.Id == roomTypeId)
            .Select(roomType => roomType.BaseRate)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<string>> ValidateReservationControlsAsync(
        int? reservationId,
        int? ratePlanId,
        int? roomTypeId,
        DateTime arrivalDate,
        DateTime departureDate)
    {
        var errors = new List<string>();
        if (roomTypeId is null || departureDate.Date <= arrivalDate.Date)
        {
            return errors;
        }

        var stayDates = StayDates(arrivalDate, departureDate).ToList();
        var restrictionDates = RestrictionDates(arrivalDate, departureDate).ToList();
        var stayNights = stayDates.Count;

        var restrictions = await _context.RateRestrictions
            .Include(restriction => restriction.RatePlan)
            .Include(restriction => restriction.RoomType)
            .AsNoTracking()
            .Where(restriction =>
                restrictionDates.Contains(restriction.RestrictionDate.Date) &&
                (restriction.RatePlanId == null || restriction.RatePlanId == ratePlanId) &&
                (restriction.RoomTypeId == null || restriction.RoomTypeId == roomTypeId))
            .ToListAsync();

        if (restrictions.Any(restriction => restriction.StopSell))
        {
            errors.Add("Stop sell is active for the selected room type, rate plan, or stay dates.");
        }

        if (restrictions.Any(restriction => restriction.ClosedToArrival && restriction.RestrictionDate.Date == arrivalDate.Date))
        {
            errors.Add("The selected rate or room type is closed to arrival on the check-in date.");
        }

        if (restrictions.Any(restriction => restriction.ClosedToDeparture && restriction.RestrictionDate.Date == departureDate.Date))
        {
            errors.Add("The selected rate or room type is closed to departure on the check-out date.");
        }

        var minLos = restrictions.Max(restriction => (int?)restriction.MinimumLengthOfStay) ?? 0;
        if (minLos > stayNights)
        {
            errors.Add($"Minimum length of stay is {minLos} night(s).");
        }

        var maxLos = restrictions
            .Where(restriction => restriction.MaximumLengthOfStay is not null)
            .Min(restriction => restriction.MaximumLengthOfStay);
        if (maxLos is not null && stayNights > maxLos.Value)
        {
            errors.Add($"Maximum length of stay is {maxLos.Value} night(s).");
        }

        foreach (var date in stayDates)
        {
            var availability = await GetAvailabilityAsync(roomTypeId.Value, date, reservationId);
            if (availability.StopSell)
            {
                errors.Add($"Inventory stop sell is active for {date:d}.");
            }

            if (availability.RoomsSold >= availability.RoomsToSell + availability.OverbookingLimit)
            {
                errors.Add($"No inventory is available for {date:d}. Rooms sold: {availability.RoomsSold}, sell limit: {availability.RoomsToSell + availability.OverbookingLimit}.");
            }
        }

        return errors;
    }

    public async Task<DailyAvailability> GetAvailabilityAsync(int roomTypeId, DateTime inventoryDate, int? excludeReservationId = null)
    {
        var date = inventoryDate.Date;
        var control = await _context.RoomInventoryControls
            .AsNoTracking()
            .FirstOrDefaultAsync(control =>
                control.RoomTypeId == roomTypeId &&
                control.InventoryDate.Date == date);

        var actualRooms = await _context.Rooms
            .AsNoTracking()
            .CountAsync(room => room.IsActive && room.RoomTypeId == roomTypeId);

        var roomsSold = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                reservation.Id != excludeReservationId &&
                reservation.RoomTypeId == roomTypeId &&
                BlockingReservationStatuses.Contains(reservation.Status) &&
                reservation.ArrivalDate.Date <= date &&
                reservation.DepartureDate.Date > date);

        var groupBlockHolds = await _context.GroupRoomBlocks
            .AsNoTracking()
            .Where(block =>
                block.RoomTypeId == roomTypeId &&
                block.BlockDate.Date == date &&
                block.GroupBooking != null &&
                (block.GroupBooking.BookingStatus == GroupBookingStatus.Tentative ||
                    block.GroupBooking.BookingStatus == GroupBookingStatus.Confirmed ||
                    block.GroupBooking.BookingStatus == GroupBookingStatus.InHouse))
            .SumAsync(block => (int?)((block.RoomsBlocked - block.RoomsPickedUp - block.RoomsReleased) > 0
                ? block.RoomsBlocked - block.RoomsPickedUp - block.RoomsReleased
                : 0)) ?? 0;

        roomsSold += groupBlockHolds;

        var totalRooms = control?.TotalRooms ?? actualRooms;
        var roomsToSell = control?.RoomsToSell ?? actualRooms;
        var overbookingLimit = control?.OverbookingLimit ?? 0;

        return new DailyAvailability(
            date,
            roomTypeId,
            totalRooms,
            roomsSold,
            Math.Max(0, roomsToSell + overbookingLimit - roomsSold),
            roomsToSell,
            overbookingLimit,
            control?.StopSell ?? false);
    }

    public async Task<decimal?> GetBaseRateRangeLowAsync(DateTime date)
    {
        return await _context.RoomTypeRates
            .AsNoTracking()
            .Where(rate => rate.IsActive && rate.EffectiveFrom.Date <= date.Date && rate.EffectiveTo.Date >= date.Date)
            .Select(rate => (decimal?)rate.BaseRate)
            .MinAsync();
    }

    public async Task<decimal?> GetBaseRateRangeHighAsync(DateTime date)
    {
        return await _context.RoomTypeRates
            .AsNoTracking()
            .Where(rate => rate.IsActive && rate.EffectiveFrom.Date <= date.Date && rate.EffectiveTo.Date >= date.Date)
            .Select(rate => (decimal?)rate.BaseRate)
            .MaxAsync();
    }

    private static IEnumerable<DateTime> StayDates(DateTime arrivalDate, DateTime departureDate)
    {
        for (var date = arrivalDate.Date; date < departureDate.Date; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static IEnumerable<DateTime> RestrictionDates(DateTime arrivalDate, DateTime departureDate)
    {
        for (var date = arrivalDate.Date; date <= departureDate.Date; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    public record DailyAvailability(
        DateTime InventoryDate,
        int RoomTypeId,
        int TotalRooms,
        int RoomsSold,
        int RoomsAvailable,
        int RoomsToSell,
        int OverbookingLimit,
        bool StopSell);
}
