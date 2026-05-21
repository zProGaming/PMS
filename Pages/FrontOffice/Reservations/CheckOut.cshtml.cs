using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CheckOutModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Reservation Reservation { get; set; } = default!;

    [BindProperty]
    public bool ManagerOverrideRequested { get; set; }

    public int? FolioId { get; set; }

    public decimal FolioBalance { get; set; }

    public bool HasOutstandingBalance => FolioBalance > 0;

    public bool CanCheckOut =>
        Reservation.Status == ReservationStatus.CheckedIn &&
        Reservation.RoomId is not null &&
        !HasOutstandingBalance;

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
        LoadFolioState();
        ManagerOverrideRequested = Reservation.ManagerOverrideRequested;
        ValidateCanCheckOut();

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
        LoadFolioState();
        Reservation.ManagerOverrideRequested = ManagerOverrideRequested;
        ValidateCanCheckOut();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Reservation.Status = ReservationStatus.CheckedOut;
        Reservation.ActualCheckOutDate = DateTime.Now;
        Reservation.Room!.Status = RoomStatus.Dirty;
        foreach (var folio in Reservation.Folios.Where(folio => folio.Status == FolioStatus.Open))
        {
            folio.Status = FolioStatus.Closed;
            folio.ClosedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = Reservation.Id });
    }

    private async Task<Reservation?> LoadReservationAsync(int id, bool asTracking)
    {
        var query = _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.Room)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Items)
            .Include(reservation => reservation.Folios)
                .ThenInclude(folio => folio.Payments)
            .Where(reservation => reservation.Id == id);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private void LoadFolioState()
    {
        var folio = Reservation.Folios.FirstOrDefault();
        FolioId = folio?.Id;
        FolioBalance = folio?.Balance ?? 0;
    }

    private void ValidateCanCheckOut()
    {
        if (Reservation.Status != ReservationStatus.CheckedIn)
        {
            ModelState.AddModelError(string.Empty, "Only checked-in reservations can be checked out.");
        }

        if (Reservation.RoomId is null || Reservation.Room is null)
        {
            ModelState.AddModelError(string.Empty, "A room must be assigned before check-out.");
        }

        if (HasOutstandingBalance)
        {
            ModelState.AddModelError(string.Empty, "Guest has outstanding balance. Please settle the folio before check-out.");
        }
    }
}
