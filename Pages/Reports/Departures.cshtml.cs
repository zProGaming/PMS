using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Reports;

public class DeparturesModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public IList<DepartureRow> Departures { get; set; } = new List<DepartureRow>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();
        var nextBusinessDate = BusinessDate.AddDays(1);

        var reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Items)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(reservation =>
                reservation.DepartureDate >= BusinessDate &&
                reservation.DepartureDate < nextBusinessDate)
            .OrderBy(reservation => reservation.DepartureDate)
            .ThenBy(reservation => reservation.Guest!.LastName)
            .ToListAsync();

        Departures = reservations
            .Select(reservation => new DepartureRow(
                BusinessDate,
                FormatGuestName(reservation.Guest?.FirstName, reservation.Guest?.LastName),
                reservation.Room?.RoomNumber ?? "Unassigned",
                reservation.DepartureDate,
                reservation.Folios.Sum(folio => folio.Balance),
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

    public record DepartureRow(
        DateTime BusinessDate,
        string GuestName,
        string RoomNumber,
        DateTime CheckOutDate,
        decimal Balance,
        string Status);
}
