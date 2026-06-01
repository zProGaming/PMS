using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
        var loadResult = await LoadCheckInFormAsync(id);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? id)
    {
        var loadResult = await LoadCheckInFormAsync(id);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return NativePartial();
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
        await ValidateRoomNotAssignedToAnotherInHouseReservationAsync();

        if (!ModelState.IsValid)
        {
            return NativePartialOrPage();
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

    private async Task<IActionResult?> LoadCheckInFormAsync(int? id)
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

        return null;
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

    private async Task ValidateRoomNotAssignedToAnotherInHouseReservationAsync()
    {
        if (Reservation.RoomId is null)
        {
            return;
        }

        var roomHasAnotherInHouseGuest = await _context.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                reservation.Id != Reservation.Id &&
                reservation.RoomId == Reservation.RoomId &&
                reservation.Status == ReservationStatus.CheckedIn);

        if (roomHasAnotherInHouseGuest)
        {
            ModelState.AddModelError(string.Empty, "The assigned room already has an in-house reservation. Select a different room before check-in.");
        }
    }

    private static string CreateFolioNumber(Reservation reservation)
    {
        return $"FOL-{reservation.Id:000000}";
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_CheckInNative",
            ViewData = new ViewDataDictionary<CheckInModel>(ViewData, this)
        };
    }
}
