using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Groups;

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
    public int TentativeGroups { get; set; }
    public int ConfirmedGroupsArrivingThisWeek { get; set; }
    public int GroupRoomsBlocked { get; set; }
    public int GroupRoomsPickedUp { get; set; }
    public int PseudoRoomOpenFolios { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        TotalRooms = await _context.Rooms.CountAsync(room => room.IsActive);
        AvailableRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Available);
        OccupiedRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Occupied);
        DirtyRooms = await _context.Rooms.CountAsync(room =>
            room.IsActive && room.Status == RoomStatus.Dirty);

        ArrivalsToday = await _context.Reservations.CountAsync(reservation =>
            reservation.ArrivalDate >= today &&
            reservation.ArrivalDate < tomorrow &&
            reservation.Status == ReservationStatus.Reserved);

        DeparturesToday = await _context.Reservations.CountAsync(reservation =>
            reservation.DepartureDate >= today &&
            reservation.DepartureDate < tomorrow &&
            reservation.Status == ReservationStatus.CheckedIn);

        InHouseGuests = await _context.Reservations.CountAsync(reservation =>
            reservation.Status == ReservationStatus.CheckedIn);

        var weekEnd = today.AddDays(7);
        TentativeGroups = await _context.GroupBookings.CountAsync(group => group.BookingStatus == GroupBookingStatus.Tentative);
        ConfirmedGroupsArrivingThisWeek = await _context.GroupBookings.CountAsync(group =>
            group.BookingStatus == GroupBookingStatus.Confirmed &&
            group.ArrivalDate >= today &&
            group.ArrivalDate < weekEnd);
        GroupRoomsBlocked = await _context.GroupRoomBlocks
            .Where(block => block.BlockDate >= today && block.BlockDate < weekEnd && block.GroupBooking != null && block.GroupBooking.BookingStatus != GroupBookingStatus.Cancelled)
            .SumAsync(block => (int?)block.RoomsBlocked) ?? 0;
        GroupRoomsPickedUp = await _context.GroupRoomBlocks
            .Where(block => block.BlockDate >= today && block.BlockDate < weekEnd && block.GroupBooking != null && block.GroupBooking.BookingStatus != GroupBookingStatus.Cancelled)
            .SumAsync(block => (int?)block.RoomsPickedUp) ?? 0;
        PseudoRoomOpenFolios = await _context.GroupFolios.CountAsync(folio => folio.Status == GroupFolioStatus.Open && folio.PseudoRoomId != null);
    }
}
