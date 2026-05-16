using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class RevenueIntelligenceModel(ApplicationDbContext context, ExecutiveKPIService kpiService) : PageModel
{
    public ExecutiveSummaryMetrics Summary { get; private set; } = new();
    public IList<RevenueDateRow> ForwardDates { get; private set; } = [];
    public int ReservationsOnBooks { get; private set; }
    public decimal RevenueOnBooks { get; private set; }
    public int PromoCodeUses { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var next30 = today.AddDays(30);
        Summary = await kpiService.GetSummaryAsync(today, today);
        var totalRooms = await context.Rooms.AsNoTracking().CountAsync(room => room.IsActive);
        var reservations = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Status != ReservationStatus.Cancelled && reservation.Status != ReservationStatus.NoShow && reservation.ArrivalDate < next30 && reservation.DepartureDate > today)
            .Select(reservation => new { reservation.ArrivalDate, reservation.DepartureDate, reservation.RateAmount })
            .ToListAsync();

        ReservationsOnBooks = reservations.Count;
        RevenueOnBooks = reservations.Sum(reservation => reservation.RateAmount * Math.Max(1, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days));
        PromoCodeUses = await context.BookingRequests.AsNoTracking().CountAsync(request => request.PromotionCodeId != null && request.CreatedAt >= today.AddDays(-30));

        for (var date = today; date < today.AddDays(7); date = date.AddDays(1))
        {
            var sold = reservations.Count(reservation => reservation.ArrivalDate.Date <= date && reservation.DepartureDate.Date > date);
            ForwardDates.Add(new RevenueDateRow(date, totalRooms <= 0 ? 0 : sold * 100m / totalRooms, sold));
        }
    }
}

public record RevenueDateRow(DateTime Date, decimal OccupancyForecast, int ReservationsOnBooks);
