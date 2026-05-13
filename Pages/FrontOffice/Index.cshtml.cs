using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int TotalRooms { get; set; }

    public int AvailableRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public int DirtyRooms { get; set; }

    public int ArrivalsToday { get; set; }

    public int DeparturesToday { get; set; }

    public int InHouseGuests { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        TotalRooms = await _context.Rooms.CountAsync(room => room.IsActive);
        AvailableRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive &&
            (room.Status == RoomStatus.VacantClean || room.Status == RoomStatus.Inspected));
        OccupiedRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Occupied);
        DirtyRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.VacantDirty);

        ArrivalsToday = await _context.Reservations.CountAsync(reservation =>
            reservation.ArrivalDate >= today &&
            reservation.ArrivalDate < tomorrow &&
            reservation.Status != ReservationStatus.Cancelled &&
            reservation.Status != ReservationStatus.NoShow);

        DeparturesToday = await _context.Reservations.CountAsync(reservation =>
            reservation.DepartureDate >= today &&
            reservation.DepartureDate < tomorrow &&
            reservation.Status != ReservationStatus.Cancelled &&
            reservation.Status != ReservationStatus.NoShow);

        InHouseGuests = await _context.Reservations.CountAsync(reservation =>
            reservation.Status == ReservationStatus.CheckedIn &&
            reservation.ArrivalDate <= today &&
            reservation.DepartureDate > today);
    }
}
