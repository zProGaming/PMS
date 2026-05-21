using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CreateModel(ApplicationDbContext context, RevenueManagementService revenueManagement) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly RevenueManagementService _revenueManagement = revenueManagement;

    [BindProperty]
    public Reservation Reservation { get; set; } = new()
    {
        ArrivalDate = DateTime.Today,
        DepartureDate = DateTime.Today.AddDays(1),
        Adults = 1,
        Status = ReservationStatus.Reserved
    };

    public SelectList GuestOptions { get; set; } = default!;

    public SelectList RoomOptions { get; set; } = default!;

    public SelectList RatePlanOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> ReservationStatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public decimal? SuggestedRate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await ApplySelectedRoomAsync();
        ValidateStayDates();
        await ApplySuggestedRateAsync();
        await ValidateRevenueControlsAsync();

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(Reservation.GuestId, Reservation.RoomId, Reservation.RatePlanId, Reservation.Status);
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

        if (!room.IsActive || !IsAssignableRoomStatus(room.Status))
        {
            ModelState.AddModelError("Reservation.RoomId", $"Room {room.RoomNumber} is {room.Status} and cannot be assigned to a new reservation.");
            return;
        }

        Reservation.PropertyId = room.PropertyId;
        Reservation.RoomTypeId = room.RoomTypeId;
        await ValidateRoomAvailabilityAsync(room.Id);
    }

    private void ValidateStayDates()
    {
        if (Reservation.DepartureDate <= Reservation.ArrivalDate)
        {
            ModelState.AddModelError("Reservation.DepartureDate", "Check-out date must be after check-in date.");
        }

        if (Reservation.RateAmount < 0)
        {
            ModelState.AddModelError("Reservation.RateAmount", "Reservation rate cannot be negative.");
        }
    }

    private async Task ApplySuggestedRateAsync()
    {
        SuggestedRate = await _revenueManagement.GetSuggestedRateAsync(
            Reservation.RatePlanId,
            Reservation.RoomTypeId,
            Reservation.ArrivalDate,
            Reservation.DepartureDate);

        if (Reservation.RateAmount <= 0 && SuggestedRate > 0)
        {
            Reservation.RateAmount = SuggestedRate.Value;
        }
    }

    private async Task ValidateRevenueControlsAsync()
    {
        var errors = await _revenueManagement.ValidateReservationControlsAsync(
            null,
            Reservation.RatePlanId,
            Reservation.RoomTypeId,
            Reservation.ArrivalDate,
            Reservation.DepartureDate);

        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private async Task ValidateRoomAvailabilityAsync(int roomId)
    {
        var hasConflict = await _context.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                reservation.RoomId == roomId &&
                reservation.Status != ReservationStatus.Cancelled &&
                reservation.Status != ReservationStatus.CheckedOut &&
                reservation.Status != ReservationStatus.NoShow &&
                reservation.ArrivalDate.Date < Reservation.DepartureDate.Date &&
                reservation.DepartureDate.Date > Reservation.ArrivalDate.Date);

        if (hasConflict)
        {
            ModelState.AddModelError("Reservation.RoomId", "The selected room already has an active reservation during these stay dates.");
        }
    }

    private async Task LoadSelectListsAsync(object? selectedGuest = null, object? selectedRoom = null, object? selectedRatePlan = null, ReservationStatus selectedStatus = ReservationStatus.Reserved)
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
            .Where(room => room.IsActive &&
                (room.Status == RoomStatus.Available ||
                    room.Status == RoomStatus.Clean ||
                    room.Status == RoomStatus.Inspected))
            .OrderBy(room => room.Property!.Name)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();

        var ratePlans = await _context.RatePlans
            .AsNoTracking()
            .Where(ratePlan => ratePlan.IsActive)
            .OrderBy(ratePlan => ratePlan.Code)
            .Select(ratePlan => new { ratePlan.Id, Name = ratePlan.Code + " - " + ratePlan.Name })
            .ToListAsync();

        GuestOptions = new SelectList(guests, "Id", "Name", selectedGuest);
        RoomOptions = new SelectList(
            rooms.Select(room => new
            {
                room.Id,
                Name = $"{room.Property?.Name} - {room.RoomNumber} ({room.RoomType?.Name}, {room.Status})"
            }),
            "Id",
            "Name",
            selectedRoom);
        RatePlanOptions = new SelectList(ratePlans, "Id", "Name", selectedRatePlan);
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

    private static bool IsAssignableRoomStatus(RoomStatus status)
    {
        return status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected;
    }
}
