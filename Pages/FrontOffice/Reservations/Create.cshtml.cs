using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Reservation Reservation { get; set; } = new()
    {
        ArrivalDate = DateTime.Today,
        DepartureDate = DateTime.Today.AddDays(1),
        Adults = 1,
        Status = ReservationStatus.Pending
    };

    public SelectList GuestOptions { get; set; } = default!;

    public SelectList RoomOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> ReservationStatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await ApplySelectedRoomAsync();
        ValidateStayDates();

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(Reservation.GuestId, Reservation.RoomId, Reservation.Status);
            return Page();
        }

        Reservation.ConfirmationNumber = CreateConfirmationNumber();
        Reservation.CreatedAtUtc = DateTime.UtcNow;

        _context.Reservations.Add(Reservation);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task ApplySelectedRoomAsync()
    {
        if (Reservation.RoomId is null)
        {
            ModelState.AddModelError("Reservation.RoomId", "Select a room.");
            return;
        }

        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == Reservation.RoomId);

        if (room is null)
        {
            ModelState.AddModelError("Reservation.RoomId", "The selected room was not found.");
            return;
        }

        Reservation.PropertyId = room.PropertyId;
        Reservation.RoomTypeId = room.RoomTypeId;
    }

    private void ValidateStayDates()
    {
        if (Reservation.DepartureDate <= Reservation.ArrivalDate)
        {
            ModelState.AddModelError("Reservation.DepartureDate", "Check-out date must be after check-in date.");
        }
    }

    private async Task LoadSelectListsAsync(object? selectedGuest = null, object? selectedRoom = null, ReservationStatus selectedStatus = ReservationStatus.Pending)
    {
        var guests = await _context.Guests
            .AsNoTracking()
            .OrderBy(guest => guest.LastName)
            .ThenBy(guest => guest.FirstName)
            .Select(guest => new { guest.Id, Name = guest.LastName + ", " + guest.FirstName })
            .ToListAsync();

        var rooms = await _context.Rooms
            .Include(room => room.Property)
            .Include(room => room.RoomType)
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.Property!.Name)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();

        GuestOptions = new SelectList(guests, "Id", "Name", selectedGuest);
        RoomOptions = new SelectList(
            rooms.Select(room => new
            {
                room.Id,
                Name = $"{room.Property?.Name} - {room.RoomNumber} ({room.RoomType?.Name})"
            }),
            "Id",
            "Name",
            selectedRoom);
        ReservationStatusOptions = BuildReservationStatusOptions(selectedStatus);
    }

    private static IEnumerable<SelectListItem> BuildReservationStatusOptions(ReservationStatus selectedStatus)
    {
        return Enum.GetValues<ReservationStatus>()
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status.ToString(),
                Selected = status == selectedStatus
            });
    }

    private static string CreateConfirmationNumber()
    {
        return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}
