using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CheckInModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Reservation Reservation { get; set; } = default!;

    public bool CanCheckIn => Reservation.Status == ReservationStatus.Reserved && Reservation.RoomId is not null;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await LoadReservationAsync(id.Value, asTracking: false);
        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        ValidateCanCheckIn();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await LoadReservationAsync(id.Value, asTracking: true);
        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        ValidateCanCheckIn();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Reservation.Status = ReservationStatus.CheckedIn;
        Reservation.ActualCheckInDate = DateTime.Now;
        Reservation.Room!.Status = RoomStatus.Occupied;

        if (!await _context.Folios.AnyAsync(folio => folio.ReservationId == Reservation.Id))
        {
            _context.Folios.Add(new Folio
            {
                PropertyId = Reservation.PropertyId,
                ReservationId = Reservation.Id,
                GuestId = Reservation.GuestId,
                FolioNumber = CreateFolioNumber(Reservation),
                Status = FolioStatus.Open,
                OpenedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = Reservation.Id });
    }

    private async Task<Reservation?> LoadReservationAsync(int id, bool asTracking)
    {
        var query = _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Where(reservation => reservation.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private void ValidateCanCheckIn()
    {
        if (Reservation.Status != ReservationStatus.Reserved)
        {
            ModelState.AddModelError(string.Empty, "Only reserved reservations can be checked in.");
        }

        if (Reservation.RoomId is null || Reservation.Room is null)
        {
            ModelState.AddModelError(string.Empty, "A room must be assigned before check-in.");
        }
        else if (Reservation.Room.Status is RoomStatus.Occupied or RoomStatus.Dirty or RoomStatus.OutOfOrder or RoomStatus.Maintenance)
        {
            ModelState.AddModelError(string.Empty, $"Room {Reservation.Room.RoomNumber} is {Reservation.Room.Status} and cannot be checked in until it is available, clean, or inspected.");
        }
    }

    private static string CreateFolioNumber(Reservation reservation)
    {
        return $"FOL-{reservation.Id:000000}";
    }
}
