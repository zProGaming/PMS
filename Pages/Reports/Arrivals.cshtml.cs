using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Reports;

public class ArrivalsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public IList<ArrivalRow> Arrivals { get; set; } = new List<ArrivalRow>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);

        var reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .AsNoTracking()
            .Where(reservation =>
                reservation.ArrivalDate >= BusinessDate &&
                reservation.ArrivalDate < nextBusinessDate)
            .OrderBy(reservation => reservation.ArrivalDate)
            .ThenBy(reservation => reservation.Guest!.LastName)
            .ToListAsync();

        Arrivals = reservations
            .Select(reservation => new ArrivalRow(
                BusinessDate,
                FormatGuestName(reservation.Guest?.FirstName, reservation.Guest?.LastName),
                reservation.Room?.RoomNumber ?? "Unassigned",
                reservation.ArrivalDate,
                reservation.Status.ToString()))
            .ToList();
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private static string FormatGuestName(string? firstName, string? lastName)
    {
        return string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    public record ArrivalRow(
        DateTime BusinessDate,
        string GuestName,
        string RoomNumber,
        DateTime CheckInDate,
        string Status);
}
