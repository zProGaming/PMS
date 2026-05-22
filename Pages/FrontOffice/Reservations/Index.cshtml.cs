using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Reservation> Reservations { get; set; } = new List<Reservation>();
    public SelectList StatusOptions { get; private set; } = default!;
    public int ArrivalsToday { get; private set; }
    public int DeparturesToday { get; private set; }
    public int InHouseReservations { get; private set; }
    public int UnassignedReservations { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReservationStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StayDate { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ArrivalsToday = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                reservation.ArrivalDate >= today &&
                reservation.ArrivalDate < tomorrow &&
                reservation.Status == ReservationStatus.Reserved);

        DeparturesToday = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                reservation.DepartureDate >= today &&
                reservation.DepartureDate < tomorrow &&
                reservation.Status == ReservationStatus.CheckedIn);

        InHouseReservations = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn);

        UnassignedReservations = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                reservation.RoomId == null &&
                reservation.Status != ReservationStatus.Cancelled &&
                reservation.Status != ReservationStatus.CheckedOut &&
                reservation.Status != ReservationStatus.NoShow);

        var query = _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.RoomType)
            .Include(reservation => reservation.Property)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchTerm = Search.Trim();
            query = query.Where(reservation =>
                reservation.ConfirmationNumber.Contains(searchTerm) ||
                reservation.Guest != null &&
                    (reservation.Guest.FirstName.Contains(searchTerm) ||
                     reservation.Guest.LastName.Contains(searchTerm)) ||
                reservation.Room != null && reservation.Room.RoomNumber.Contains(searchTerm));
        }

        if (Status is not null)
        {
            query = query.Where(reservation => reservation.Status == Status);
        }

        if (StayDate is not null)
        {
            var selectedDate = StayDate.Value.Date;
            query = query.Where(reservation =>
                reservation.ArrivalDate.Date <= selectedDate &&
                reservation.DepartureDate.Date > selectedDate);
        }

        Reservations = await query
            .OrderByDescending(reservation => reservation.ArrivalDate)
            .ThenBy(reservation => reservation.Guest!.LastName)
            .ToListAsync();

        StatusOptions = new SelectList(Enum.GetValues<ReservationStatus>().Select(status => new
        {
            Id = status,
            Name = status.ToString()
        }), "Id", "Name", Status);
    }
}
