using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Reports;

public class InHouseModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public DateTime BusinessDate { get; set; }

    public IList<InHouseGuestRow> InHouseGuests { get; set; } = new List<InHouseGuestRow>();

    public async Task OnGetAsync()
    {
        BusinessDate = await GetBusinessDateAsync();

        var reservations = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Items)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Payments)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(reservation => reservation.Status == ReservationStatus.CheckedIn)
            .OrderBy(reservation => reservation.Room!.RoomNumber)
            .ThenBy(reservation => reservation.Guest!.LastName)
            .ToListAsync();

        InHouseGuests = reservations
            .Select(reservation => new InHouseGuestRow(
                FormatGuestName(reservation.Guest?.FirstName, reservation.Guest?.LastName),
                reservation.Room?.RoomNumber ?? "Unassigned",
                reservation.ArrivalDate,
                reservation.DepartureDate,
                reservation.Folios.Sum(folio => folio.Balance)))
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

    public record InHouseGuestRow(
        string GuestName,
        string RoomNumber,
        DateTime ArrivalDate,
        DateTime DepartureDate,
        decimal Balance);
}
